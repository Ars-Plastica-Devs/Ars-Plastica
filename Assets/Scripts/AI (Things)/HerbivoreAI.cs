using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HerbivoreAI : Animal
{
    private enum BehaviourState
    {
        Reproducing,
        Running,
        Flocking,
        SeekingFood,
        Death
    }

    private readonly HashSet<GameObject> m_NodulesInRange = new HashSet<GameObject>();
    private readonly HashSet<GameObject> m_CarnivoresInRange = new HashSet<GameObject>();
    private readonly HashSet<GameObject> m_HerbivoresInRange = new HashSet<GameObject>();

    private GameObject m_TargetNodule;

    private int m_DaysOfStarvingDamageTaken;
    private int m_NodulesEatenWhileFullLife;
    private int m_RefreshClosestNoduleCounter;
    private int m_RefreshClosestNoduleRate = 60;

    private readonly FSM<BehaviourState> m_BehaviourBrain = new FSM<BehaviourState>();

    [SerializeField] [SyncVar]
    private BehaviourState m_CurrentBehaviourState;

    public float DaysBeforeReproducing = 25f;
    public float DaysBetweenReproductions = 3f;
    public float StarvingDamageAmount = 30f;
    public float StructureCollisionDamageAmount = 10f;

    public WanderParameters WanderParameters = new WanderParameters(1f, 10f, 40f);
    public float MaxFlockDispersion = 30f;
    public float MinFlockDispersion = 15f;
    private float m_MaxFlockDispersionSquared;
    private float m_MinFlockDispersionSquared;

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        m_MaxFlockDispersionSquared = MaxFlockDispersion * MaxFlockDispersion;
        m_MinFlockDispersionSquared = MinFlockDispersion * MinFlockDispersion;


        var collidersInRangeNow = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius);
        foreach (var coll in collidersInRangeNow)
        {
            if (coll.tag == "Nodule")
            {
                m_NodulesInRange.Add(coll.gameObject);
            }
            else if (coll.tag == "Carnivore")
            {
                m_CarnivoresInRange.Add(coll.gameObject);
            }
            else if (coll.tag == "Herbivore")
            {
                m_HerbivoresInRange.Add(coll.gameObject);
            }
        }

        //This forces the herbivore to start in the seeking food state
        TimeSinceEating = Clock.DaysToSeconds(.4f);

        m_BehaviourBrain.In(BehaviourState.SeekingFood)
            .If(() => Health <= 0)
                .GoTo(BehaviourState.Death)
            .If(() => TimeSinceEating < Clock.DaysToSeconds(.4f))
                .GoTo(BehaviourState.Flocking)
            .If(() => ReproductionAllowed && DaysOld > DaysBeforeReproducing && Health >= 100f && m_NodulesEatenWhileFullLife > 0 && (DaysOld - LastDayOfReproduction) > DaysBetweenReproductions)
                .GoTo(BehaviourState.Reproducing)
            .ExecuteWhileIn(SeekFood, StarvationCheck);

        m_BehaviourBrain.In(BehaviourState.Flocking)
            .If(() => TimeSinceEating > Clock.DaysToSeconds(.4f))
                .GoTo(BehaviourState.SeekingFood)
            .If(() => ReproductionAllowed && DaysOld > DaysBeforeReproducing && Health >= 100f && m_NodulesEatenWhileFullLife > 0 && (DaysOld - LastDayOfReproduction) > DaysBetweenReproductions)
                .GoTo(BehaviourState.Reproducing)
            .ExecuteWhileIn(Flocking);

        m_BehaviourBrain.In(BehaviourState.Reproducing)
            .DoOnce(Reproduce)
                .If(() => true)
                    .GoTo(BehaviourState.SeekingFood);

        m_BehaviourBrain.In(BehaviourState.Death)
            .DoOnce(Die);

        m_BehaviourBrain.Initialize(BehaviourState.SeekingFood);
    }

    private void Update()
    {
        CurrentGrowthState = GrowthBrain.CurrentState;
        m_CurrentBehaviourState = m_BehaviourBrain.CurrentState;

        if (isClient && !isServer)
        {
            transform.localScale = Scale;
            Rigidbody.velocity = Velocity;
        }

        if (isServer)
        {
            m_HerbivoresInRange.RemoveWhere(go => go == null);
            m_CarnivoresInRange.RemoveWhere(go => go == null);
            m_NodulesInRange.RemoveWhere(go => go == null);

            GrowthBrain.Update(Time.deltaTime);

            Velocity = Rigidbody.velocity;
        }
    }

    public override void Damage(float amount)
    {
        base.Damage(amount);

        if (Health < 100f)
        {
            m_NodulesEatenWhileFullLife = 0;
        }
    }

    protected override void BehaviourUpdate()
    {
        m_BehaviourBrain.Update(Time.deltaTime);
    }

    private void SeekFood()
    {
        //TODO: Make sure the nodule isnt fleeing faster than we can catch them
        TimeSinceEating += Time.deltaTime;

        m_RefreshClosestNoduleCounter++;
        if (m_RefreshClosestNoduleCounter > m_RefreshClosestNoduleRate)
        {
            m_RefreshClosestNoduleCounter = 0;
            m_TargetNodule = GetClosestGameObject(m_NodulesInRange);
        }

        if (m_TargetNodule == null)
        {
            m_TargetNodule = GetClosestGameObject(m_NodulesInRange);

            if (m_TargetNodule == null)
            {
                Flocking();
            }
            return;
        }

        var toTarget = (m_TargetNodule.transform.position - transform.position);
        var desiredVel = toTarget.normalized * BaseSpeed;

        transform.rotation = Quaternion.LookRotation(desiredVel);
        Rigidbody.velocity = desiredVel;
    }

    private void StarvationCheck()
    {
        if (Clock.SecondsToDays(TimeSinceEating) - m_DaysOfStarvingDamageTaken > 1f)
        {
            m_DaysOfStarvingDamageTaken++;
            Damage(StarvingDamageAmount);
        }
    }

    private void Flocking()
    {
        TimeSinceEating += Time.deltaTime;

        var vel = Vector3.zero;
        if (m_HerbivoresInRange.Count > 0)
        {
            Vector3 cohesionTarget;
            var cohesionVel = Steering.Cohesion(gameObject, m_HerbivoresInRange, out cohesionTarget);

            var sqrDistance = (cohesionTarget - transform.position).sqrMagnitude;

            if (sqrDistance > m_MaxFlockDispersionSquared)
            {
                if (sqrDistance > m_MaxFlockDispersionSquared * 2)
                    vel += cohesionVel * 1.5f;
                else
                    vel += cohesionVel;
            }
            else if (sqrDistance < m_MinFlockDispersionSquared)
            {
                if (sqrDistance < m_MinFlockDispersionSquared / 2)
                    vel += -cohesionVel * .75f;
                else
                    vel += -cohesionVel * .25f;
            }
        }

        vel += Steering.Wander(gameObject, ref WanderParameters);
        //vel += Steering.Cohesion(gameObject, m_HerbivoresInRange) * .5f;

        //Use velocity so that physics continues to work
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel), 5f);
        Rigidbody.velocity = vel.normalized * BaseSpeed;
    }

    private void Reproduce()
    {
        m_NodulesEatenWhileFullLife = 0;
        LastDayOfReproduction = DaysOld;

        if (Ecosystem.CanAddHerbivore())
            Ecosystem.SpawnHerbivore(transform.position + transform.forward * 5f);
    }

    protected override void Die()
    {
        Ecosystem.KillHerbivore(gameObject);
    }

    private GameObject GetClosestGameObject(IEnumerable<GameObject> objs)
    {
        GameObject closest = null;
        var closestDist = float.PositiveInfinity;
        foreach (var obj in objs)
        {
            if (obj == null) continue;
            var dist = (transform.position - obj.transform.position).sqrMagnitude;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = obj;
            }
        }

        return closest;
    }

    private void Eat(GameObject obj)
    {
        m_NodulesInRange.Remove(obj);

        if (obj == null) return;

        Ecosystem.RemoveNodule(obj);
        NetworkServer.Destroy(obj);

        if (Health >= 100f)
        {
            m_NodulesEatenWhileFullLife++;
        }

        Health = 100f;
        TimeSinceEating = 0f;
        m_DaysOfStarvingDamageTaken = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer || !enabled)
            return;

        if (other.tag == "Nodule")
        {
            m_NodulesInRange.Add(other.gameObject);
        }
        else if (other.tag == "Carnivore")
        {
            m_CarnivoresInRange.Add(other.gameObject);
        }
        else if (other.tag == "Herbivore")
        {
            m_HerbivoresInRange.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer || !enabled)
            return;

        if (other.tag == "Nodule")
        {
            m_NodulesInRange.Remove(other.gameObject);
        }
        else if (other.tag == "Carnivore")
        {
            m_CarnivoresInRange.Remove(other.gameObject);
        }
        else if (other.tag == "Herbivore")
        {
            m_HerbivoresInRange.Remove(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (!isServer || !enabled)
            return;

        if (coll.gameObject.tag == "Nodule")
        {
            Eat(coll.gameObject);
        }
        else if (coll.gameObject.tag == "Structure")
        {
            GetComponent<Rigidbody>().velocity = -(coll.contacts[0].point - transform.position).normalized * BaseSpeed;
            Damage(StructureCollisionDamageAmount);
        }
    }
}

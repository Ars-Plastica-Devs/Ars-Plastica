using System.Collections.Generic;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class HerbivoreBase : Creature
{
    private struct BehaviourStateComparer : IEqualityComparer<BehaviourState>
    {
        public bool Equals(BehaviourState x, BehaviourState y)
        {
            return x == y;
        }

        public int GetHashCode(BehaviourState obj)
        {
            return (int)obj;
        }
    }

    private static readonly BehaviourStateComparer BStateComparer = new BehaviourStateComparer();

    public enum BehaviourState
    {
        Reproducing,
        Running,
        Flocking,
        SeekingFood,
        Eating,
        Dying,
        Death
    }

    protected IGrower Grower;

    protected readonly FSM<BehaviourState> BehaviourBrain = new FSM<BehaviourState>(BStateComparer);

    [SerializeField]
    private BehaviourState m_BehaviourState;
    public BehaviourState CurrentBehaviourState {
        get { return m_BehaviourState; }
        private set { m_BehaviourState = value; }
    }

    protected OctreeSensor<Nodule> NoduleSensor;

    protected Rigidbody Rigidbody;

    public abstract HerbivoreType Type { get; }
    public AgeDataComponent AgeData;
    public FlockingOptions FlockingOptions = new FlockingOptions(.25f, .75f, 1f, 30f, 15f);
    public WanderParameters WanderParameters = new WanderParameters(1f, 10f, 40f);

    public float BaseSpeed = 18f;
    public float SensingRadius = 100f;

    public float DaysBeforeReproducing = 25f;
    public float DaysBetweenReproductions = 3f;
    public float StarvingDamageAmount = 30f;
    public float StructureCollisionDamageAmount = 10f;

    public abstract void StartDeathThrows();
    protected abstract void BehaviourUpdate();

    private void Awake()
    {
        if (AgeData == null)
            AgeData = GetComponent<AgeDataComponent>();

        if (AgeData == null)
            Debug.LogError("AgeDataComponent does not exist on " + gameObject.name, gameObject);
    }

    protected override void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();

        base.Start();
    }

    public override void OnStartServer()
    {
        NoduleSensor = new OctreeSensor<Nodule>(transform, SensingRadius, OctreeManager.Get(OctreeType.Nodule));
        var noduleRate = NoduleSensor.RefreshRate;
        NoduleSensor.RefreshRate += Random.Range(-.05f * noduleRate, .05f * noduleRate);

        base.OnStartServer();
    }

    protected virtual void Update()
    {
        CurrentBehaviourState = BehaviourBrain.CurrentState;

        if (!isServer) return;

        Grower.GrowthUpdate(AgeData.DaysOld / AgeData.DaysToGrown);

        BehaviourUpdate();
    }

    protected virtual void FixedUpdate()
    {
        if (!isServer)
            return;

        if (BehaviourBrain.CurrentState == BehaviourState.SeekingFood)
        {
            NoduleSensor.SensorUpdate();
        }

        if (NoduleSensor != null && NoduleSensor.Closest != null)
        {
            var myBounds = gameObject.GetComponent<BoxCollider>().bounds;
            var targetBounds = NoduleSensor.Closest.GetComponentsInChildren<Renderer>();

            for (var i = 0; i < targetBounds.Length; i++)
            {
                if (!myBounds.Intersects(targetBounds[i].bounds)) continue;

                Eat(NoduleSensor.Closest);
                NoduleSensor.ForceUpdate();
                break;
            }
        }
    }

    protected void Flock<T>(HashSet<T> flockMates)
        where T : Component
    {
        var vel = Vector3.zero;
        //flockmates contains this object aswell
        if (flockMates.Count > 1)
        {
            Vector3 cohesionTarget;
            var cohesionVel = Steering.Cohesion(gameObject, flockMates, out cohesionTarget);
            var alignmentVel = Steering.Alignment(gameObject, flockMates) * FlockingOptions.AlignmentWeight;

            vel += alignmentVel;

            var sqrDistance = (cohesionTarget - transform.position).sqrMagnitude;

            if (sqrDistance > FlockingOptions.MaxDispersionSquared)
            {
                if (sqrDistance > FlockingOptions.MaxDispersionSquared * 2)
                    vel += cohesionVel * 1.5f;
                else
                    vel += cohesionVel;
            }
            else if (sqrDistance < FlockingOptions.MinDispersionSquared)
            {
                if (sqrDistance < FlockingOptions.MinDispersionSquared / 2)
                    vel += -cohesionVel * .75f;
                else
                    vel += -cohesionVel * .25f;
            }
        }

        vel += Steering.Wander(gameObject, ref WanderParameters) * FlockingOptions.WanderWeight;
        //vel += Steering.Cohesion(gameObject, m_FlockMates) * .5f;

        if (vel == Vector3.zero)
            vel = transform.forward;

        //Use velocity so that physics continues to work
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel, transform.up), 5f);
        Rigidbody.velocity = vel.normalized * BaseSpeed;
    }

    protected virtual void Eat(Nodule nod)
    {
        nod.GetEaten(this);
    }

    protected virtual void Die()
    {
        Ecosystem.Singleton.KillHerbivore(this);
    }
}

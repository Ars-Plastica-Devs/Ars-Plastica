﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CarnivoreAI : Animal
{
    private enum BehaviourState
    {
        Reproducing,
        Hunting,
        Death
    }

    private readonly HashSet<GameObject> m_HerbivoresInRange = new HashSet<GameObject>();

    private GameObject m_ClosestHerbivore;

    private int m_DaysOfStarvingDamageTaken;
    private int m_HerbivoresEatenWhileFullLife;

    private readonly FSM<BehaviourState> m_BehaviourBrain = new FSM<BehaviourState>();

    [SerializeField]
    [SyncVar]
    private BehaviourState m_CurrentBehaviourState;

    public float DaysBetweenReproductions = 3f;
    public float StarvingDamageAmount = 34f;
    public float StructureCollisionDamageAmount = 10f;

    public int MaximumHerdSizeToAttack = 10;
    public float HerdApproachDistance = 40f;

    public WanderParameters WanderParameters = new WanderParameters(1f, 10f, 40f);

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        var collidersInRangeNow = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius);
        foreach (var coll in collidersInRangeNow)
        {
            if (coll.tag == "Herbivore")
            {
                m_HerbivoresInRange.Add(coll.gameObject);
            }
        }

        //Starts the carnivore slightly hungry
        TimeSinceEating = Clock.DaysToSeconds(.4f);

        m_BehaviourBrain.In(BehaviourState.Hunting)
            .If(() => Health <= 0)
                .GoTo(BehaviourState.Death)
            .If(() => ReproductionAllowed && CurrentGrowthState == GrowthState.Adult && Health >= 100f && m_HerbivoresEatenWhileFullLife >= 2 && (DaysOld - LastDayOfReproduction) > DaysBetweenReproductions)
                .GoTo(BehaviourState.Reproducing)
            .ExecuteWhileIn(Hunt, StarvationCheck);

        m_BehaviourBrain.In(BehaviourState.Reproducing)
            .DoOnce(Reproduce)
                .If(() => true)
                    .GoTo(BehaviourState.Hunting);

        m_BehaviourBrain.In(BehaviourState.Death)
            .DoOnce(Die);

        m_BehaviourBrain.Initialize(BehaviourState.Hunting);
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
            m_HerbivoresInRange.RemoveWhere(go => go.name == "null");

            GrowthBrain.Update(Time.deltaTime);

            Velocity = Rigidbody.velocity;
        }
    }

    public override void Damage(float amount)
    {
        base.Damage(amount);

        if (Health < 100f)
        {
            m_HerbivoresEatenWhileFullLife = 0;
        }
    }

    protected override void BehaviourUpdate()
    {
        m_BehaviourBrain.Update(Time.deltaTime);
    }

    private void Hunt()
    {
        TimeSinceEating += Time.deltaTime;

        var vel = Vector3.zero;
        m_ClosestHerbivore = GetClosestGameObject(m_HerbivoresInRange);

        if (m_HerbivoresInRange.Count > MaximumHerdSizeToAttack)
        {
            var dist = (m_ClosestHerbivore.transform.position - transform.position).magnitude;
            if (dist < HerdApproachDistance)
            {
                vel += Steering.Wander(gameObject, ref WanderParameters);
            }
            else
            {
                vel += Steering.Wander(gameObject, ref WanderParameters);

                var evadeFactor = dist < HerdApproachDistance / 2f ? 2f : .5f;
                vel += Steering.Evade(gameObject, m_ClosestHerbivore, Speed) * evadeFactor;
            }
        }
        else if (m_ClosestHerbivore == null || !PredationAllowed)
        {
            vel += Steering.Wander(gameObject, ref WanderParameters);
        }
        else
        {
            vel += Steering.Pursuit(gameObject, m_ClosestHerbivore, Speed);
        }

        //Use velocity so that physics continues to work
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel), 5f);
        Rigidbody.velocity = vel.normalized * Speed;
    }

    private void StarvationCheck()
    {
        if (Clock.secondsToDays(TimeSinceEating) - m_DaysOfStarvingDamageTaken > 1f)
        {
            m_DaysOfStarvingDamageTaken++;
            Damage(StarvingDamageAmount);
        }
    }

    private void Reproduce()
    {
        m_HerbivoresEatenWhileFullLife = 0;
        LastDayOfReproduction = DaysOld;

        if (Ecosystem.CanAddCarnivore())
            Ecosystem.SpawnCarnivore(transform.position + transform.forward * 5f);
    }

    protected override void Die()
    {
        Ecosystem.KillCarnivore(gameObject);
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
        m_HerbivoresInRange.Remove(obj);

        if (obj == null) return;

        Ecosystem.KillHerbivore(obj);

        if (Health >= 100f)
        {
            m_HerbivoresEatenWhileFullLife++;
        }

        Health = 100f;
        TimeSinceEating = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer || !enabled)
            return;

        if (other.tag == "Herbivore")
        {
            m_HerbivoresInRange.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer || !enabled)
            return;

        if (other.tag == "Herbivore")
        {
            m_HerbivoresInRange.Remove(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (!isServer || !enabled)
            return;

        if (coll.gameObject.tag == "Herbivore")
        {
            Eat(coll.gameObject);
        }
        else if (coll.gameObject.tag == "Structure")
        {
            GetComponent<Rigidbody>().velocity = -(coll.contacts[0].point - transform.position).normalized * Speed;
            Damage(StructureCollisionDamageAmount);
        }
    }
}

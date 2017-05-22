using System.Collections.Generic;
using Assets.Octree;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class CarnivoreBase : Creature
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
        Hunting,
        Attacking,
        Eating,
        SkirtingHerd,
        Death
    }

    protected readonly FSM<BehaviourState> BehaviourBrain = new FSM<BehaviourState>(BStateComparer);

    protected IProximitySensor<HerbivoreBase> HerbivoreSensor;

    protected bool ApplyVelocity = false;

    protected Rigidbody Rigidbody;

    protected IGrower Grower;

    //protected GameObject ClosestHerbivore;
    protected GameObject ClosestSnatcher;
    protected GameObject GrabbedTarget;

    protected int DaysOfStarvingDamageTaken;
    protected float TimeSinceEating;
    protected int FoodEatenWhileFullLife;
    protected float LastDayOfReproduction;

    [SerializeField]
    private BehaviourState m_BehaviourState;
    public BehaviourState CurrentBehaviourState
    {
        get { return m_BehaviourState; }
        private set { m_BehaviourState = value; }
    }

    public float SensingRadius = 50f;

    public abstract CarnivoreType Type { get; }

    public AgeData AgeData = new AgeData(200f, 5f, 0f);
    public WanderParameters WanderParameters = new WanderParameters(1f, 10f, 40f);

    public float BaseSpeed = 20f;
    public float HuntingPeriodSpeed;
    public float HuntingPeriodStart = 12f;
    public float HuntingPeriodEnd = 14.5f;

    public int MaximumHerdSizeToAttack = 10;
    public float HerdApproachDistance = 40f;

    public float DaysBetweenReproductions = 3f;

    public float StarvingDamageAmount = 34f;
    public float StructureCollisionDamageAmount = 10f;

    protected override void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();

        base.Start();

        if (!isServer && isClient)
        {
            var anim = GetComponent<Animator>();
            if (anim != null)
                anim.applyRootMotion = false;
            return;
        }

        if (!isServer)
            return;

        HerbivoreSensor = new OctreeSensor<HerbivoreBase>(transform, SensingRadius, MaximumHerdSizeToAttack + 1, OctreeManager.Get(OctreeType.Herbivore));

        //Starts the carnivore slightly hungry
        TimeSinceEating = DayClock.Singleton.DaysToSeconds(1.01f);
    }

    public override void Damage(float amount)
    {
        base.Damage(amount);

        if (Health < 100f)
        {
            FoodEatenWhileFullLife = 0;
        }
    }

    protected virtual void Update()
    {
        CurrentBehaviourState = BehaviourBrain.CurrentState;

        if (!isServer) return;

        HerbivoreSensor.SensorUpdate();

        HerbivoreSensor.KClosest.RemoveWhere(go => go == null);

        AgeData.DaysOld = DayClock.Singleton.SecondsToDays(Time.time - BirthTime);
        Grower.GrowthUpdate(AgeData.DaysOld / AgeData.DaysToGrown);

        BehaviourUpdate();
    }

    protected virtual void BehaviourUpdate()
    {
        TimeSinceEating += Time.deltaTime;
        
        BehaviourBrain.Update(Time.deltaTime);
        StarvationCheck();
    }

    protected virtual void Hunt()
    {
        var vel = Vector3.zero;

        if ((HerbivoreSensor.Closest == null)
            || !PredationAllowed || DayClock.Singleton.SecondsToDays(TimeSinceEating) < .4f)
        {
            vel += Steering.Wander(gameObject, ref WanderParameters);
        }
        else
        {
            vel += transform.forward;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel, transform.up), 1f);

        if (ApplyVelocity)
        {
            var speed = GetSpeed();
            Rigidbody.velocity = vel.normalized * speed;
        }
    }

    protected virtual void SkirtHerd()
    {
        var vel = default(Vector3);
        var speed = GetSpeed();

        var dist = (HerbivoreSensor.Closest.transform.position - transform.position).magnitude;
        if (dist < HerdApproachDistance)
        {
            vel += Steering.Wander(gameObject, ref WanderParameters);
        }
        else
        {
            vel += Steering.Wander(gameObject, ref WanderParameters);

            var evadeFactor = dist < HerdApproachDistance / 2f ? 2f : .5f;
            vel += Steering.Evade(gameObject, HerbivoreSensor.Closest.gameObject, speed) * evadeFactor;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel, transform.up), 1f);

        if (ApplyVelocity)
        {
            Rigidbody.velocity = vel.normalized * speed;
        }
    }

    protected virtual void Reproduce()
    {
        FoodEatenWhileFullLife = 0;
        LastDayOfReproduction = AgeData.DaysOld;

        if (Ecosystem.Singleton.CanAddCarnivore())
            Ecosystem.Singleton.SpawnCarnivore(transform.position + transform.forward * 5f, Quaternion.identity, Type);
    }

    protected virtual void Die()
    {
        Ecosystem.Singleton.KillCarnivore(this);
    }

    protected virtual void GrabTarget(GameObject obj)
    {
        GrabbedTarget = obj;
    }

    protected virtual void EatGrabbedTarget()
    {
        if (HerbivoreSensor.Closest != null && HerbivoreSensor.Closest.gameObject == GrabbedTarget)
        {
            Ecosystem.Singleton.KillHerbivore(GrabbedTarget.GetComponent<HerbivoreBase>(), true);
        }

        GrabbedTarget = null;

        if (Health >= 100f)
        {
            FoodEatenWhileFullLife++;
        }

        Health = 100f;
        TimeSinceEating = 0f;
        DaysOfStarvingDamageTaken = 0;
    }

    protected virtual float GetSpeed()
    {
        return (DayClock.Singleton.Hour > HuntingPeriodStart && DayClock.Singleton.Hour < HuntingPeriodEnd)
            ? HuntingPeriodSpeed
            : BaseSpeed;
    }

    private void StarvationCheck()
    {
        if (DayClock.Singleton.SecondsToDays(TimeSinceEating) - DaysOfStarvingDamageTaken > 1f)
        {
            DaysOfStarvingDamageTaken++;
            Damage(StarvingDamageAmount);
        }
    }
}

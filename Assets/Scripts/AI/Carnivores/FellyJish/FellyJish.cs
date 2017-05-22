using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(FellyJishAnimAudioController))]
[SpawnableCreature("fellyjish", CarnivoreType.FellyJish)]
public class FellyJish : CarnivoreBase
{
    //Animation flags
    private bool m_Attacking;

    private FellyJishAnimAudioController m_AnimAudioController;

    public Transform FoodPosition;

    [SyncVar]
    public float Scale = 1f;

    public override CarnivoreType Type
    {
        get { return CarnivoreType.FellyJish; }
    }

    protected override void Start()
    {
        if (Scale > 5f)
            Scale = 1f;
        else if (Scale < 1f)
            Scale = 1f;

        transform.localScale = new Vector3(Scale, Scale, Scale);

        if (FoodPosition == null)
            Debug.LogError("FoodPosition is set to null in FellyJish", this);

        //FellyJish does not use root motion movement
        ApplyVelocity = true;

        if (!isServer && isClient)
        {
            base.Start();
            return;
        }

        m_AnimAudioController = GetComponent<FellyJishAnimAudioController>();
        m_AnimAudioController.OnAttackFinished += OnAttackFinished;
        m_AnimAudioController.OnEatingFinished += OnEatingFinished;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.FellyJishInitialScale),
            DataStore.GetFloat(Data.FellyJishFinalScaleMin),
            DataStore.GetFloat(Data.FellyJishFinalScaleMax));

        Grower.StartGrowing();
        Scale = Grower.Scale;

        AgeData.DaysToGrown = DataStore.GetFloat(Data.FellyJishDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.FellyJishLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.FellyJishBaseSpeed);

        HuntingPeriodSpeed = DataStore.GetFloat(Data.FellyJishHuntingPeriodSpeed);
        DaysBetweenReproductions = DataStore.GetFloat(Data.FellyJishDaysBetweenReproductions);
        StarvingDamageAmount = DataStore.GetFloat(Data.FellyJishStarvingDamageAmount);
        StructureCollisionDamageAmount = DataStore.GetFloat(Data.FellyJishStructureCollisionDamageAmount);
        MaximumHerdSizeToAttack = DataStore.GetInt(Data.FellyJishMaximumHerdSizeToAttack);
        HerdApproachDistance = DataStore.GetFloat(Data.FellyJishHerdApproachDistance);
        SensingRadius = DataStore.GetFloat(Data.FellyJishSensingRadius);

        base.Start();

        BehaviourBrain.In(BehaviourState.Hunting)
            .If(() => Health <= 0 || AgeData.DaysOld > AgeData.LifeSpan)
                .GoTo(BehaviourState.Death)
            .If(() => ReproductionAllowed && Grower.State == GrowthState.Grown && Health >= 100f && FoodEatenWhileFullLife >= 2 && (AgeData.DaysOld - LastDayOfReproduction) > DaysBetweenReproductions)
                .GoTo(BehaviourState.Reproducing)
            .If(() => TimeSinceEating > DayClock.Singleton.DaysToSeconds(1f) && (HerbivoreSensor.Closest != null || ClosestSnatcher != null))
                .GoTo(BehaviourState.Attacking)
            .ExecuteWhileIn(Hunt);

        BehaviourBrain.In(BehaviourState.Attacking)
            .If(() => (HerbivoreSensor.Closest == null && ClosestSnatcher == null) || TimeSinceEating < DayClock.Singleton.DaysToSeconds(1f))
                .GoTo(BehaviourState.Hunting)
            .If(() => HerbivoreSensor.KClosest.Count > MaximumHerdSizeToAttack && HerbivoreSensor.Closest != null)
                .GoTo(BehaviourState.SkirtingHerd)
            .If(() => GrabbedTarget != null)
                .GoTo(BehaviourState.Eating)
            .ExecuteOnEntry(StartAttack)
            .ExecuteWhileIn(Attack)
            .ExecuteOnExit(EndAttack);

        BehaviourBrain.In(BehaviourState.Eating)
            .If(() => GrabbedTarget == null)
                .GoTo(BehaviourState.Hunting)
            .ExecuteOnEntry(StartEating)
            .ExecuteWhileIn(EatTarget)
            .ExecuteOnExit(EndEating);

        BehaviourBrain.In(BehaviourState.SkirtingHerd)
            .If(() => HerbivoreSensor.KClosest.Count < MaximumHerdSizeToAttack || HerbivoreSensor.Closest == null)
                .GoTo(BehaviourState.Hunting)
            .ExecuteWhileIn(SkirtHerd);

        BehaviourBrain.In(BehaviourState.Reproducing)
            .DoOnce(Reproduce)
                .If(() => true)
                    .GoTo(BehaviourState.Hunting);

        BehaviourBrain.In(BehaviourState.Death)
            .DoOnce(Die);

        BehaviourBrain.Initialize(BehaviourState.Hunting);
    }

    protected override void Update()
    {
        if (isClient && !isServer)
        {
            if (Scale != 0f && !float.IsNaN(Scale))
            {
                transform.localScale = new Vector3(Scale, Scale, Scale);
            }
            if (GrabbedTarget != null)
            {
                GrabbedTarget.transform.position = FoodPosition.position;
            }
        }

        base.Update();

        if (isServer && Grower.Scale.PercentDifference(Scale) > .03f)
            Scale = Grower.Scale;
    }

    private void StartAttack()
    {

    }

    private void Attack()
    {
        var target = HerbivoreSensor.Closest.gameObject ?? ClosestSnatcher;
        if (target == null)
            return;

        //Carnivores move faster at certain parts of the day
        var speed = GetSpeed();

        var vel = Steering.Pursuit(gameObject, target, speed);

        if (vel.sqrMagnitude < .2f)
            vel = transform.forward;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel, transform.up), 1f);

        if (ApplyVelocity)
        {
            Rigidbody.velocity = vel.normalized * speed;
        }

        if (!m_Attacking && (FoodPosition.position - target.transform.position).sqrMagnitude < 10f)
        {
            m_Attacking = true;
            m_AnimAudioController.DoAttack();
        }
    }

    private void EndAttack()
    {
        m_Attacking = false;
    }

    private void OnAttackFinished()
    {
        if (HerbivoreSensor.Closest == null)
            return;

        var target = HerbivoreSensor.Closest.gameObject ?? ClosestSnatcher;
        if (target == null)
            return;

        GrabTarget(target);
    }

    private void StartEating()
    {
        GrabbedTarget.transform.position = FoodPosition.position;
        m_AnimAudioController.DoEating();

        var hb = GrabbedTarget.GetComponent<HerbivoreBase>();
        if (hb != null)
            hb.StartDeathThrows();
    }

    private void EatTarget()
    {
        GrabbedTarget.transform.position = FoodPosition.position;
        Rigidbody.velocity = Vector3.zero;
    }

    private void EndEating()
    {
        GrabbedTarget = null;
    }

    private void OnEatingFinished()
    {
        EatGrabbedTarget();
    }

    protected override void GrabTarget(GameObject obj)
    {
        base.GrabTarget(obj);

        RpcSetGrabbedTarget(obj);
    }

    [ClientRpc]
    private void RpcSetGrabbedTarget(GameObject obj)
    {
        GrabbedTarget = obj;
    }

    public List<string> GetData()
    {
        var ageString = Grower.State == GrowthState.Growing
                                        ? "Young" : "Adult";

        switch (CurrentBehaviourState)
        {
            case BehaviourState.Death:
                return new List<string> { "This " + ageString + " FellyJish is Dead!" };
            case BehaviourState.Hunting:
                return new List<string> { "This " + ageString + " FellyJish is currently hunting" };
            case BehaviourState.Reproducing:
                return new List<string> { "This " + ageString + " FellyJish is reproducing!" };
        }

        return new List<string>();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.FellyJishDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.FellyJishLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.FellyJishBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.FellyJishHuntingPeriodSpeed, HuntingPeriodSpeed);
        DataStore.SetIfDifferent(Data.FellyJishDaysBetweenReproductions, DaysBetweenReproductions);
        DataStore.SetIfDifferent(Data.FellyJishStarvingDamageAmount, StarvingDamageAmount);
        DataStore.SetIfDifferent(Data.FellyJishStructureCollisionDamageAmount, StructureCollisionDamageAmount);
        DataStore.SetIfDifferent(Data.FellyJishMaximumHerdSizeToAttack, MaximumHerdSizeToAttack);
        DataStore.SetIfDifferent(Data.FellyJishHerdApproachDistance, HerdApproachDistance);
        DataStore.SetIfDifferent(Data.FellyJishSensingRadius, SensingRadius);
    }

    public static void ChangeFellyJishData(Data key, string value, IEnumerable<FellyJish> fellyjishEnum)
    {
        var fellyjish = fellyjishEnum.ToList();
        switch (key)
        {
            case Data.FellyJishInitialScale:
                var initScale = float.Parse(value);
                fellyjish.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.FellyJishFinalScaleMin:
                var scaleMin = float.Parse(value);
                fellyjish.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.FellyJishFinalScaleMax:
                var scaleMax = float.Parse(value);
                fellyjish.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.FellyJishHuntingPeriodSpeed:
                var huntingSpeed = float.Parse(value);
                fellyjish.ForEach(c => c.HuntingPeriodSpeed = huntingSpeed);
                break;
            case Data.FellyJishDaysBetweenReproductions:
                var betweenRepro = float.Parse(value);
                fellyjish.ForEach(c => c.DaysBetweenReproductions = betweenRepro);
                break;
            case Data.FellyJishStarvingDamageAmount:
                var starvingDamage = float.Parse(value);
                fellyjish.ForEach(c => c.StarvingDamageAmount = starvingDamage);
                break;
            case Data.FellyJishStructureCollisionDamageAmount:
                var collisionDamage = float.Parse(value);
                fellyjish.ForEach(c => c.StructureCollisionDamageAmount = collisionDamage);
                break;
            case Data.FellyJishMaximumHerdSizeToAttack:
                var herdSize = int.Parse(value);
                fellyjish.ForEach(c => c.MaximumHerdSizeToAttack = herdSize);
                break;
            case Data.FellyJishHerdApproachDistance:
                var distance = float.Parse(value);
                fellyjish.ForEach(c => c.HerdApproachDistance = distance);
                break;
            case Data.FellyJishSensingRadius:
                var senseRadius = float.Parse(value);
                fellyjish.ForEach(c =>
                {
                    c.SensingRadius = senseRadius;
                    c.HerbivoreSensor.Range = senseRadius;
                });
                break;
            case Data.FellyJishLifeSpan:
                var lifeSpan = float.Parse(value);
                fellyjish.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.FellyJishBaseSpeed:
                var baseSpeed = float.Parse(value);
                fellyjish.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.FellyJishDaysToGrown:
                var dtg = float.Parse(value);
                fellyjish.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}

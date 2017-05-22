using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(GnomehatzAnimAudioController))]
[SpawnableCreature("gnomehatz", CarnivoreType.Gnomehatz)]
public class Gnomehatz : CarnivoreBase
{
    //Animation flags
    private bool m_Attacking;

    private GnomehatzAnimAudioController m_AnimAudioController;

    public Transform FoodPosition;

    [SyncVar]
    public float Scale = 1f;

    public override CarnivoreType Type
    {
        get { return CarnivoreType.Gnomehatz; }
    }

    protected override void Start()
    {
        if (Scale > 5f)
            Scale = 1f;
        else if (Scale < 1f)
            Scale = 1f;

        transform.localScale = new Vector3(Scale, Scale, Scale);

        if (FoodPosition == null)
            Debug.LogError("FoodPosition is set to null in Gnomehatz", this);

        if (!isServer && isClient)
        {
            base.Start();
            return;
        }

        m_AnimAudioController = GetComponent<GnomehatzAnimAudioController>();
        m_AnimAudioController.OnEatingFinished += OnEatingFinished;
        m_AnimAudioController.OnAttackFinished += OnAttackFinished;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.GnomehatzInitialScale),
            DataStore.GetFloat(Data.GnomehatzFinalScaleMin),
            DataStore.GetFloat(Data.GnomehatzFinalScaleMax));

        Grower.StartGrowing();
        Scale = Grower.Scale;

        AgeData.DaysToGrown = DataStore.GetFloat(Data.GnomehatzDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.GnomehatzLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.GnomehatzBaseSpeed);

        HuntingPeriodSpeed = DataStore.GetFloat(Data.GnomehatzHuntingPeriodSpeed);
        DaysBetweenReproductions = DataStore.GetFloat(Data.GnomehatzDaysBetweenReproductions);
        StarvingDamageAmount = DataStore.GetFloat(Data.GnomehatzStarvingDamageAmount);
        StructureCollisionDamageAmount = DataStore.GetFloat(Data.GnomehatzStructureCollisionDamageAmount);
        MaximumHerdSizeToAttack = DataStore.GetInt(Data.GnomehatzMaximumHerdSizeToAttack);
        HerdApproachDistance = DataStore.GetFloat(Data.GnomehatzHerdApproachDistance);
        SensingRadius = DataStore.GetFloat(Data.GnomehatzSensingRadius);

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
        Rigidbody.velocity = Vector3.zero;
        m_AnimAudioController.StartMoveFast();
    }

    private void Attack()
    {
        var target = HerbivoreSensor.Closest.gameObject ?? ClosestSnatcher;
        if (target == null)
            return;

        //Carnivores move faster at certain parts of the day
        var speed = (DayClock.Singleton.Hour > HuntingPeriodStart && DayClock.Singleton.Hour < HuntingPeriodEnd)
            ? HuntingPeriodSpeed
            : BaseSpeed;

        var vel = Steering.Pursuit(gameObject, target, speed);

        if (vel.sqrMagnitude < .2f)
            vel = transform.forward;

        //Don't do any smoothing of the rotation, just set it directly. Avoids constant missing
        transform.rotation = Quaternion.LookRotation(vel, transform.up);

        if (!m_Attacking && (FoodPosition.position - target.transform.position).sqrMagnitude < 5f)
        {
            m_Attacking = true;
            m_AnimAudioController.DoAttack();
        }
    }

    private void EndAttack()
    {
        m_Attacking = false;
        m_AnimAudioController.StopMoveFast();
    }

    private void OnAttackFinished()
    {
        var target = HerbivoreSensor.Closest.gameObject ?? ClosestSnatcher;
        if (target == null)
            return;

        GrabTarget(target);
    }

    private void StartEating()
    {
        GrabbedTarget.transform.position = FoodPosition.position;

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
                return new List<string> { "This " + ageString + " Gnomehatz is Dead!" };
            case BehaviourState.Hunting:
                return new List<string> { "This " + ageString + " Gnomehatz is currently hunting" };
            case BehaviourState.Reproducing:
                return new List<string> { "This " + ageString + " Gnomehatz is reproducing!" };
        }

        return new List<string>();
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (!isServer || !enabled)
            return;

        if (coll.gameObject.tag == "Herbivore" || coll.gameObject.tag == "Snatcher")
        {
            //if (CurrentBehaviourState == BehaviourState.Attacking)
                //GrabTarget(coll.gameObject);
        }
        else if (coll.gameObject.tag == "Structure")
        {
            TurnAwayFromCollision();

            Damage(StructureCollisionDamageAmount);
        }
    }

    private void TurnAwayFromCollision()
    {
        var objectInFront = Physics.Raycast(transform.position, transform.forward, 10f);

        //If there isn't an object in front of us, we don't need to do any turning
        //Since in the next update we will be moving generally forward
        if (!objectInFront)
            return;

        var r = Random.value;
        if (r < .33f)
            m_AnimAudioController.DoTurnLeft();
        else if (r < .66f)
            m_AnimAudioController.DoTurnRight();
        else
            m_AnimAudioController.DoTurnDown();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.GnomehatzDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.GnomehatzLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.GnomehatzBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.GnomehatzHuntingPeriodSpeed, HuntingPeriodSpeed);
        DataStore.SetIfDifferent(Data.GnomehatzDaysBetweenReproductions, DaysBetweenReproductions);
        DataStore.SetIfDifferent(Data.GnomehatzStarvingDamageAmount, StarvingDamageAmount);
        DataStore.SetIfDifferent(Data.GnomehatzStructureCollisionDamageAmount, StructureCollisionDamageAmount);
        DataStore.SetIfDifferent(Data.GnomehatzMaximumHerdSizeToAttack, MaximumHerdSizeToAttack);
        DataStore.SetIfDifferent(Data.GnomehatzHerdApproachDistance, HerdApproachDistance);
        DataStore.SetIfDifferent(Data.GnomehatzSensingRadius, SensingRadius);
    }

    public static void ChangeGnomehatzData(Data key, string value, IEnumerable<Gnomehatz> gnomehatzEnum)
    {
        var gnomehatz = gnomehatzEnum.ToList();
        switch (key)
        {
            case Data.GnomehatzInitialScale:
                var initScale = float.Parse(value);
                gnomehatz.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.GnomehatzFinalScaleMin:
                var scaleMin = float.Parse(value);
                gnomehatz.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.GnomehatzFinalScaleMax:
                var scaleMax = float.Parse(value);
                gnomehatz.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.GnomehatzHuntingPeriodSpeed:
                var huntingSpeed = float.Parse(value);
                gnomehatz.ForEach(c => c.HuntingPeriodSpeed = huntingSpeed);
                break;
            case Data.GnomehatzDaysBetweenReproductions:
                var betweenRepro = float.Parse(value);
                gnomehatz.ForEach(c => c.DaysBetweenReproductions = betweenRepro);
                break;
            case Data.GnomehatzStarvingDamageAmount:
                var starvingDamage = float.Parse(value);
                gnomehatz.ForEach(c => c.StarvingDamageAmount = starvingDamage);
                break;
            case Data.GnomehatzStructureCollisionDamageAmount:
                var collisionDamage = float.Parse(value);
                gnomehatz.ForEach(c => c.StructureCollisionDamageAmount = collisionDamage);
                break;
            case Data.GnomehatzMaximumHerdSizeToAttack:
                var herdSize = int.Parse(value);
                gnomehatz.ForEach(c => c.MaximumHerdSizeToAttack = herdSize);
                break;
            case Data.GnomehatzHerdApproachDistance:
                var distance = float.Parse(value);
                gnomehatz.ForEach(c => c.HerdApproachDistance = distance);
                break;
            case Data.GnomehatzSensingRadius:
                var senseRadius = float.Parse(value);
                gnomehatz.ForEach(c =>
                {
                    c.SensingRadius = senseRadius;
                    c.HerbivoreSensor.Range = senseRadius;
                });
                break;
            case Data.GnomehatzLifeSpan:
                var lifeSpan = float.Parse(value);
                gnomehatz.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.GnomehatzBaseSpeed:
                var baseSpeed = float.Parse(value);
                gnomehatz.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.GnomehatzDaysToGrown:
                var dtg = float.Parse(value);
                gnomehatz.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}
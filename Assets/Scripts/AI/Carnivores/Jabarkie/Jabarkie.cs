using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(JabarkieAnimAudioController))]
[SpawnableCreature("jabarkie", CarnivoreType.Jabarkie)]
public class Jabarkie : CarnivoreBase
{
    //Animation flags
    private bool m_Roaring;
    private bool m_Attacking;
    private bool m_DiveBombing;

    private JabarkieAnimAudioController m_AnimAudioController;

    public Transform FoodPosition;

    [SyncVar]
    public float Scale = 1f;

    public override CarnivoreType Type
    {
        get { return CarnivoreType.Jabarkie; }
    }

    protected override void Start()
    {
        if (Scale > 5f)
            Scale = 1f;
        else if (Scale < 1f)
            Scale = 1f;

        transform.localScale = new Vector3(Scale, Scale, Scale);

        if (FoodPosition == null)
            Debug.LogError("FoodPosition is set to null in Jabarkie", this);

        if (!isServer && isClient)
        {
            base.Start();
            return;
        }

        m_AnimAudioController = GetComponent<JabarkieAnimAudioController>();

        m_AnimAudioController.OnRoarFinished += OnRoarFinished;
        m_AnimAudioController.OnEatingFinished += OnEatingFinished;
        m_AnimAudioController.OnDiveBombFinished += OnDiveBombFinished;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.JabarkieInitialScale),
            DataStore.GetFloat(Data.JabarkieFinalScaleMin),
            DataStore.GetFloat(Data.JabarkieFinalScaleMax));

        Grower.StartGrowing();
        Scale = Grower.Scale;

        AgeData.DaysToGrown = DataStore.GetFloat(Data.JabarkieDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.JabarkieLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.JabarkieBaseSpeed);

        HuntingPeriodSpeed = DataStore.GetFloat(Data.JabarkieHuntingPeriodSpeed);
        DaysBetweenReproductions = DataStore.GetFloat(Data.JabarkieDaysBetweenReproductions);
        StarvingDamageAmount = DataStore.GetFloat(Data.JabarkieStarvingDamageAmount);
        StructureCollisionDamageAmount = DataStore.GetFloat(Data.JabarkieStructureCollisionDamageAmount);
        MaximumHerdSizeToAttack = DataStore.GetInt(Data.JabarkieMaximumHerdSizeToAttack);
        HerdApproachDistance = DataStore.GetFloat(Data.JabarkieHerdApproachDistance);
        SensingRadius = DataStore.GetFloat(Data.JabarkieSensingRadius);

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
            if (Scale != 0f && !float.IsNaN(Scale) && !float.IsInfinity(Scale))
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

    private void LateUpdate()
    {
        if (isClient && GrabbedTarget != null)
        {
            GrabbedTarget.transform.position = FoodPosition.position;
        }
    }

    private void StartAttack()
    {
        m_Roaring = true;
        m_Attacking = true;
        Rigidbody.velocity = Vector3.zero;
        m_AnimAudioController.DoQuickStopAndRoar();
    }

    private void Attack()
    {
        var target = HerbivoreSensor.Closest.gameObject ?? ClosestSnatcher;
        if (target == null)
            return;

        if (m_Roaring)
        {
            Rigidbody.velocity = Vector3.zero;
            transform.LookAt(target.transform);
            return;
        }

        //Carnivores move faster at certain parts of the day
        var speed = (DayClock.Singleton.Hour > HuntingPeriodStart && DayClock.Singleton.Hour < HuntingPeriodEnd)
            ? HuntingPeriodSpeed
            : BaseSpeed;

        if (m_DiveBombing)
            speed *= 2f;

        var vel = Steering.Pursuit(gameObject, target, speed);

        if (vel.sqrMagnitude < .2f)
            vel = transform.forward;

        //Use velocity so that physics continues to work
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel, transform.up), 5f);
    }

    private void EndAttack()
    {
        m_Attacking = false;
        m_Roaring = false;
    }

    private void StartEating()
    {
        GrabbedTarget.transform.position = FoodPosition.position;
        m_AnimAudioController.DoStrike();

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

    private void OnRoarFinished()
    {
        m_Roaring = false;
        if (m_Attacking)
        {
            m_AnimAudioController.DoDiveBomb();
            m_DiveBombing = true;
        }
    }

    private void OnDiveBombFinished()
    {
        m_DiveBombing = false;
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
                return new List<string> { "This " + ageString + " Jabarkie is Dead!" };
            case BehaviourState.Hunting:
                return new List<string> { "This " + ageString + " Jabarkie is currently hunting" };
            case BehaviourState.Reproducing:
                return new List<string> { "This " + ageString + " Jabarkie is reproducing!" };
        }

        return new List<string>();
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (!isServer || !enabled)
            return;

        if (coll.gameObject.tag == "Herbivore" || coll.gameObject.tag == "Snatcher")
        {
            if (CurrentBehaviourState == BehaviourState.Attacking)
                GrabTarget(coll.gameObject);
        }
        else if (coll.gameObject.tag == "Structure")
        {
            if (Random.value < .5f)
                m_AnimAudioController.DoTurnLeft();
            else
                m_AnimAudioController.DoTurnRight();

            Damage(StructureCollisionDamageAmount);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.JabarkieDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.JabarkieLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.JabarkieBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.JabarkieHuntingPeriodSpeed, HuntingPeriodSpeed);
        DataStore.SetIfDifferent(Data.JabarkieDaysBetweenReproductions, DaysBetweenReproductions);
        DataStore.SetIfDifferent(Data.JabarkieStarvingDamageAmount, StarvingDamageAmount);
        DataStore.SetIfDifferent(Data.JabarkieStructureCollisionDamageAmount, StructureCollisionDamageAmount);
        DataStore.SetIfDifferent(Data.JabarkieMaximumHerdSizeToAttack, MaximumHerdSizeToAttack);
        DataStore.SetIfDifferent(Data.JabarkieHerdApproachDistance, HerdApproachDistance);
        DataStore.SetIfDifferent(Data.JabarkieSensingRadius, SensingRadius);
    }

    public static void ChangeJabarkieData(Data key, string value, IEnumerable<Jabarkie> jabarkiesEnum)
    {
        var jabarkies = jabarkiesEnum.ToList();
        switch (key)
        {
            case Data.JabarkieInitialScale:
                var initScale = float.Parse(value);
                jabarkies.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.JabarkieFinalScaleMin:
                var scaleMin = float.Parse(value);
                jabarkies.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.JabarkieFinalScaleMax:
                var scaleMax = float.Parse(value);
                jabarkies.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.JabarkieHuntingPeriodSpeed:
                var huntingSpeed = float.Parse(value);
                jabarkies.ForEach(c => c.HuntingPeriodSpeed = huntingSpeed);
                break;
            case Data.JabarkieDaysBetweenReproductions:
                var betweenRepro = float.Parse(value);
                jabarkies.ForEach(c => c.DaysBetweenReproductions = betweenRepro);
                break;
            case Data.JabarkieStarvingDamageAmount:
                var starvingDamage = float.Parse(value);
                jabarkies.ForEach(c => c.StarvingDamageAmount = starvingDamage);
                break;
            case Data.JabarkieStructureCollisionDamageAmount:
                var collisionDamage = float.Parse(value);
                jabarkies.ForEach(c => c.StructureCollisionDamageAmount = collisionDamage);
                break;
            case Data.JabarkieMaximumHerdSizeToAttack:
                var herdSize = int.Parse(value);
                jabarkies.ForEach(c => c.MaximumHerdSizeToAttack = herdSize);
                break;
            case Data.JabarkieHerdApproachDistance:
                var distance = float.Parse(value);
                jabarkies.ForEach(c => c.HerdApproachDistance = distance);
                break;
            case Data.JabarkieSensingRadius:
                var senseRadius = float.Parse(value);
                jabarkies.ForEach(c =>
                {
                    c.SensingRadius = senseRadius;
                    c.HerbivoreSensor.Range = senseRadius;
                });
                break;
            case Data.JabarkieLifeSpan:
                var lifeSpan = float.Parse(value);
                jabarkies.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.JabarkieBaseSpeed:
                var baseSpeed = float.Parse(value);
                jabarkies.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.JabarkieDaysToGrown:
                var dtg = float.Parse(value);
                jabarkies.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}

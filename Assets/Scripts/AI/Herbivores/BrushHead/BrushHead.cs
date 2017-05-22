using System.Collections.Generic;
using System.Linq;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(BrushHeadAnimAudioController))]
[SpawnableCreature("brush-head", HerbivoreType.BrushHead)]
public class BrushHead : HerbivoreBase
{
    private BrushHeadAnimAudioController m_AnimAudioController;

    private bool m_EatingNodule;
    private bool m_InDeathThrows;

    private int m_DaysOfStarvingDamageTaken;
    private int m_NodulesEatenWhileFullLife;
    private int m_FlockingUpdateRate = 2;
    private int m_FlockingUpdateCounter;

    private float m_TimeSinceEating;
    private float m_LastDayOfReproduction;

    private IProximitySensor<BrushHead> m_FlockmateSensor;

    [SyncVar]
    public float Scale = 1f;

    public override HerbivoreType Type {
        get { return HerbivoreType.BrushHead; }
    }

    public override void OnStartServer()
    {
        m_AnimAudioController = GetComponent<BrushHeadAnimAudioController>();
        m_AnimAudioController.OnEatingFinished += OnEatingFinished;
        m_AnimAudioController.OnDyingFinished += Die;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.BrushHeadInitialScale),
            DataStore.GetFloat(Data.BrushHeadFinalScaleMin),
            DataStore.GetFloat(Data.BrushHeadFinalScaleMax));

        Grower.StartGrowing();
        Scale = Grower.Scale;

        AgeData.DaysToGrown = DataStore.GetFloat(Data.BrushHeadDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.BrushHeadLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.BrushHeadBaseSpeed);

        DaysBeforeReproducing = DataStore.GetFloat(Data.BrushHeadDaysBeforeReproducing);
        DaysBetweenReproductions = DataStore.GetFloat(Data.BrushHeadDaysBetweenReproductions);
        StarvingDamageAmount = DataStore.GetFloat(Data.BrushHeadStarvingDamageAmount);
        StructureCollisionDamageAmount = DataStore.GetFloat(Data.BrushHeadStructureCollisionDamageAmount);
        SensingRadius = DataStore.GetFloat(Data.BrushHeadSensingRadius);

        WanderParameters.Radius = DataStore.GetFloat(Data.BrushHeadWanderRadius);
        WanderParameters.Distance = DataStore.GetFloat(Data.BrushHeadWanderDistance);
        WanderParameters.Jitter = DataStore.GetFloat(Data.BrushHeadWanderJitter);
        FlockingOptions.WanderWeight = DataStore.GetFloat(Data.BrushHeadWanderWeight);
        FlockingOptions.AlignmentWeight = DataStore.GetFloat(Data.BrushHeadAlignWeight);
        FlockingOptions.MinDispersion = DataStore.GetFloat(Data.BrushHeadMinFlockDispersion);
        FlockingOptions.MaxDispersion = DataStore.GetFloat(Data.BrushHeadMaxFlockDispersion);
        FlockingOptions.MinDispersionSquared = FlockingOptions.MinDispersion * FlockingOptions.MinDispersion;
        FlockingOptions.MaxDispersionSquared = FlockingOptions.MaxDispersion * FlockingOptions.MaxDispersion;

        m_FlockmateSensor = new OctreeSensor<BrushHead>(transform, SensingRadius, 20, OctreeManager.Get(OctreeType.Herbivore));
        m_FlockmateSensor.RefreshRate = m_FlockmateSensor.RefreshRate.Randomize(.05f);

        base.OnStartServer();
        Scale = Grower.Scale;

        BehaviourBrain.In(BehaviourState.SeekingFood)
            .If(() => Health <= 0)
                .GoTo(BehaviourState.Death)
            .If(() => m_InDeathThrows)
                .GoTo(BehaviourState.Dying)
            .If(() => m_TimeSinceEating < DayClock.Singleton.DaysToSeconds(.4f))
                .GoTo(BehaviourState.Flocking)
            .If(CanReproduce)
                .GoTo(BehaviourState.Reproducing)
            .If(() => m_EatingNodule)
                .GoTo(BehaviourState.Eating)
            .ExecuteWhileIn(SeekFood, StarvationCheck);

        BehaviourBrain.In(BehaviourState.Flocking)
            .If(() => Health <= 0)
                .GoTo(BehaviourState.Death)
            .If(() => m_InDeathThrows)
                .GoTo(BehaviourState.Dying)
            .If(() => m_TimeSinceEating > DayClock.Singleton.DaysToSeconds(.4f))
                .GoTo(BehaviourState.SeekingFood)
            .If(CanReproduce)
                .GoTo(BehaviourState.Reproducing)
            .ExecuteWhileIn(Flocking);

        BehaviourBrain.In(BehaviourState.Eating)
            .If(() => m_InDeathThrows)
                .GoTo(BehaviourState.Dying)
            .If(() => !m_EatingNodule)
                .GoTo(BehaviourState.Flocking)
            .ExecuteOnEntry(StartEating)
            .ExecuteWhileIn(EatNodule)
            .ExecuteOnExit(EndEating);

        BehaviourBrain.In(BehaviourState.Reproducing)
            .DoOnce(Reproduce)
                .If(() => true)
                    .GoTo(BehaviourState.SeekingFood);

        //Empty state. All action is handled through anim callbacks or outside function calls
        //Once we enter this state, we do not leave it.
        BehaviourBrain.In(BehaviourState.Dying);

        BehaviourBrain.In(BehaviourState.Death)
            .DoOnce(Die);

        BehaviourBrain.Initialize(BehaviourState.SeekingFood);

        //This forces the herbivore to start in the seeking food state
        m_TimeSinceEating = DayClock.Singleton.DaysToSeconds(.4f);
    }

    protected override void Start()
    {
        base.Start();

        if (Scale > 5f)
            Scale = 1f;
        else if (Scale < 1f)
            Scale = 1f;

        transform.localScale = new Vector3(Scale, Scale, Scale);
    }

    protected override void Update()
    {
        if (isClient && !isServer)
        {
            if (Scale > 5f)
                Scale = 1f;

            if (Scale != 0f && !float.IsNaN(Scale))
            {
                transform.localScale = new Vector3(Scale, Scale, Scale);
            }
            else
                transform.localScale = new Vector3(1f, 1f, 1f);

        }

        if (isServer)
            m_FlockmateSensor.KClosest.RemoveWhere(go => go == null);

        base.Update();

        if (isServer && Grower.Scale.PercentDifference(Scale) > .03f)
        {
            Scale = Grower.Scale;
            Debug.Assert(Scale > 0f);
        }
    }

    protected override void FixedUpdate()
    {
        if (isServer)
            m_FlockmateSensor.SensorUpdate();

        base.FixedUpdate();
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
        m_TimeSinceEating += Time.deltaTime;

        BehaviourBrain.Update(Time.deltaTime);
    }

    private void SeekFood()
    {
        if (NoduleSensor.Closest == null)
        {
            Flocking();
            return;
        }

        var toTarget = (NoduleSensor.Closest.transform.position - transform.position);
        var desiredVel = toTarget.normalized * BaseSpeed;

        transform.rotation = Quaternion.LookRotation(desiredVel);
        Rigidbody.velocity = desiredVel;
    }

    private void StarvationCheck()
    {
        if (DayClock.Singleton.SecondsToDays(m_TimeSinceEating) - m_DaysOfStarvingDamageTaken > 1f)
        {
            m_DaysOfStarvingDamageTaken++;
            Damage(StarvingDamageAmount);
        }
    }

    private void Flocking()
    {
        m_FlockingUpdateCounter++;
        if (m_FlockingUpdateCounter < m_FlockingUpdateRate)
        {
            return;
        }
        m_FlockingUpdateCounter = 0;

        Flock(m_FlockmateSensor.KClosest);
    }

    private void Reproduce()
    {
        m_NodulesEatenWhileFullLife = 0;
        m_LastDayOfReproduction = AgeData.DaysOld;

        if (Ecosystem.Singleton.CanAddHerbivore())
            Ecosystem.Singleton.SpawnHerbivore(transform.position + transform.forward * 5f, Quaternion.identity, Type);
    }

    private bool CanReproduce()
    {
        return ReproductionAllowed && AgeData.DaysOld > DaysBeforeReproducing && Health >= 100f &&
               m_NodulesEatenWhileFullLife > 0 && (AgeData.DaysOld - m_LastDayOfReproduction) > DaysBetweenReproductions;
    }

    private void StartEating()
    {
        m_EatingNodule = true;
        m_AnimAudioController.DoEat();
    }

    private void EatNodule()
    {
        Rigidbody.velocity = Vector3.zero;
    }

    private void EndEating()
    {
        if (Health >= 100f)
        {
            m_NodulesEatenWhileFullLife++;
        }

        Health = 100f;
        m_TimeSinceEating = 0f;
        m_DaysOfStarvingDamageTaken = 0;
    }

    private void OnEatingFinished()
    {
        m_EatingNodule = false;
    }

    public override void StartDeathThrows()
    {
        m_InDeathThrows = true;
        m_AnimAudioController.DoDie();
    }

    protected override void Eat(Nodule nod)
    {
        if (nod == null)
            return;

        base.Eat(nod);
        m_EatingNodule = true;
    }

    private void OnCollisionEnter(Collision coll)
    {
        if (!isServer || !enabled)
            return;

        if (coll.gameObject.tag == "Structure")
        {
            GetComponent<Rigidbody>().velocity = -(coll.contacts[0].point - transform.position).normalized * BaseSpeed;
            Damage(StructureCollisionDamageAmount);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        if (AgeData == null)
            AgeData = GetComponent<AgeDataComponent>();

        DataStore.SetIfDifferent(Data.BrushHeadDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.BrushHeadLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.BrushHeadBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.BrushHeadDaysBeforeReproducing, DaysBeforeReproducing);
        DataStore.SetIfDifferent(Data.BrushHeadDaysBetweenReproductions, DaysBetweenReproductions);
        DataStore.SetIfDifferent(Data.BrushHeadStarvingDamageAmount, StarvingDamageAmount);
        DataStore.SetIfDifferent(Data.BrushHeadStructureCollisionDamageAmount, StructureCollisionDamageAmount);
        DataStore.SetIfDifferent(Data.BrushHeadMinFlockDispersion, FlockingOptions.MinDispersion);
        DataStore.SetIfDifferent(Data.BrushHeadMaxFlockDispersion, FlockingOptions.MaxDispersion);
        DataStore.SetIfDifferent(Data.BrushHeadWanderRadius, WanderParameters.Radius);
        DataStore.SetIfDifferent(Data.BrushHeadWanderDistance, WanderParameters.Distance);
        DataStore.SetIfDifferent(Data.BrushHeadWanderJitter, WanderParameters.Jitter);
        DataStore.SetIfDifferent(Data.BrushHeadWanderWeight, FlockingOptions.WanderWeight);
        DataStore.SetIfDifferent(Data.BrushHeadAlignWeight, FlockingOptions.AlignmentWeight);
        if (DataStore.SetIfDifferent(Data.BrushHeadSensingRadius, SensingRadius))
        {
            if (m_FlockmateSensor != null)
                m_FlockmateSensor.Range = SensingRadius;
            NoduleSensor.Range = SensingRadius;
        }
    }

    public static void ChangeBrushHeadData(Data key, string value, IEnumerable<BrushHead> brushHeadEnum)
    {
        var brushHeads = brushHeadEnum.ToList();
        switch (key)
        {
            case Data.BrushHeadInitialScale:
                var initScale = float.Parse(value);
                brushHeads.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.BrushHeadFinalScaleMin:
                var scaleMin = float.Parse(value);
                brushHeads.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.BrushHeadFinalScaleMax:
                var scaleMax = float.Parse(value);
                brushHeads.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.BrushHeadDaysBeforeReproducing:
                var beforeReproducing = float.Parse(value);
                brushHeads.ForEach(h => h.DaysBeforeReproducing = beforeReproducing);
                break;
            case Data.BrushHeadDaysBetweenReproductions:
                var betweenRepro = float.Parse(value);
                brushHeads.ForEach(h => h.DaysBetweenReproductions = betweenRepro);
                break;
            case Data.BrushHeadStarvingDamageAmount:
                var starvingDamage = float.Parse(value);
                brushHeads.ForEach(h => h.StarvingDamageAmount = starvingDamage);
                break;
            case Data.BrushHeadStructureCollisionDamageAmount:
                var collisionDamage = float.Parse(value);
                brushHeads.ForEach(h => h.StructureCollisionDamageAmount = collisionDamage);
                break;
            case Data.BrushHeadMinFlockDispersion:
                var minFlockDispersion = float.Parse(value);
                brushHeads.ForEach(h =>
                {
                    h.FlockingOptions.MinDispersion = minFlockDispersion;
                    h.FlockingOptions.MinDispersionSquared = minFlockDispersion * minFlockDispersion;
                });
                break;
            case Data.BrushHeadMaxFlockDispersion:
                var maxFlockDispersion = float.Parse(value);
                brushHeads.ForEach(h =>
                {
                    h.FlockingOptions.MaxDispersion = maxFlockDispersion;
                    h.FlockingOptions.MaxDispersionSquared = maxFlockDispersion * maxFlockDispersion;
                });
                break;
            case Data.BrushHeadSensingRadius:
                var senseRadius = float.Parse(value);
                brushHeads.ForEach(h =>
                {
                    h.SensingRadius = senseRadius;
                    h.NoduleSensor.Range = senseRadius;
                });
                break;
            case Data.BrushHeadWanderRadius:
                var wandRadius = float.Parse(value);
                brushHeads.ForEach(h => h.WanderParameters.Radius = wandRadius);
                break;
            case Data.BrushHeadWanderDistance:
                var wandDist = float.Parse(value);
                brushHeads.ForEach(h => h.WanderParameters.Distance = wandDist);
                break;
            case Data.BrushHeadWanderJitter:
                var wandJitter = float.Parse(value);
                brushHeads.ForEach(h => h.WanderParameters.Jitter = wandJitter);
                break;
            case Data.BrushHeadWanderWeight:
                var wandWeight = float.Parse(value);
                brushHeads.ForEach(h => h.FlockingOptions.WanderWeight = wandWeight);
                break;
            case Data.BrushHeadAlignWeight:
                var alignWeight = float.Parse(value);
                brushHeads.ForEach(h => h.FlockingOptions.AlignmentWeight = alignWeight);
                break;
            case Data.BrushHeadLifeSpan:
                var lifeSpan = float.Parse(value);
                brushHeads.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.BrushHeadBaseSpeed:
                var baseSpeed = float.Parse(value);
                brushHeads.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.BrushHeadDaysToGrown:
                var dtg = float.Parse(value);
                brushHeads.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}

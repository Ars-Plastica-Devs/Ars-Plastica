using System.Collections.Generic;
using System.Linq;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(TriHorseAnimAudioController))]
[SpawnableCreature("tri-horse", HerbivoreType.TriHorse)]
public class TriHorse : HerbivoreBase
{
    private TriHorseAnimAudioController m_AnimAudioController;

    private bool m_EatingNodule;
    private bool m_InDeathThrows;

    private int m_DaysOfStarvingDamageTaken;
    private int m_NodulesEatenWhileFullLife;
    private int m_FlockingUpdateRate = 2;
    private int m_FlockingUpdateCounter;

    private float m_TimeSinceEating;
    private float m_LastDayOfReproduction;

    private IProximitySensor<TriHorse> m_FlockmateSensor;

    public float Scale = 1f;

    public override HerbivoreType Type {
        get { return HerbivoreType.TriHorse; }
    }

    public override void OnStartServer()
    {
        m_AnimAudioController = GetComponent<TriHorseAnimAudioController>();
        m_AnimAudioController.OnEatingFinished += OnEatingFinished;
        m_AnimAudioController.OnDyingFinished += Die;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.TriHorseInitialScale),
            DataStore.GetFloat(Data.TriHorseFinalScaleMin),
            DataStore.GetFloat(Data.TriHorseFinalScaleMax));

        Grower.StartGrowing();
        Scale = Grower.Scale;

        AgeData.DaysToGrown = DataStore.GetFloat(Data.TriHorseDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.TriHorseLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.TriHorseBaseSpeed);

        DaysBeforeReproducing = DataStore.GetFloat(Data.TriHorseDaysBeforeReproducing);
        DaysBetweenReproductions = DataStore.GetFloat(Data.TriHorseDaysBetweenReproductions);
        StarvingDamageAmount = DataStore.GetFloat(Data.TriHorseStarvingDamageAmount);
        StructureCollisionDamageAmount = DataStore.GetFloat(Data.TriHorseStructureCollisionDamageAmount);
        SensingRadius = DataStore.GetFloat(Data.TriHorseSensingRadius);

        WanderParameters.Radius = DataStore.GetFloat(Data.TriHorseWanderRadius);
        WanderParameters.Distance = DataStore.GetFloat(Data.TriHorseWanderDistance);
        WanderParameters.Jitter = DataStore.GetFloat(Data.TriHorseWanderJitter);
        FlockingOptions.WanderWeight = DataStore.GetFloat(Data.TriHorseWanderWeight);
        FlockingOptions.AlignmentWeight = DataStore.GetFloat(Data.TriHorseAlignWeight);
        FlockingOptions.MinDispersion = DataStore.GetFloat(Data.TriHorseMinFlockDispersion);
        FlockingOptions.MaxDispersion = DataStore.GetFloat(Data.TriHorseMaxFlockDispersion);
        FlockingOptions.MinDispersionSquared = FlockingOptions.MinDispersion * FlockingOptions.MinDispersion;
        FlockingOptions.MaxDispersionSquared = FlockingOptions.MaxDispersion * FlockingOptions.MaxDispersion;

        m_FlockmateSensor = new OctreeSensor<TriHorse>(transform, SensingRadius, 20, OctreeManager.Get(OctreeType.Herbivore));
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

        if (desiredVel.sqrMagnitude < .2f)
            desiredVel = transform.forward;

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

        /*DataStore.SetIfDifferent(Data.TriHorseInitialScale, m_Grower.InitialScale);
        DataStore.SetIfDifferent(Data.TriHorseFinalScaleMin, m_Grower.FinalScaleMin);
        DataStore.SetIfDifferent(Data.TriHorseFinalScaleMax, m_Grower.FinalScaleMax);*/

        if (AgeData == null)
            AgeData = GetComponent<AgeDataComponent>();

        DataStore.SetIfDifferent(Data.TriHorseDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.TriHorseLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.TriHorseBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.TriHorseDaysBeforeReproducing, DaysBeforeReproducing);
        DataStore.SetIfDifferent(Data.TriHorseDaysBetweenReproductions, DaysBetweenReproductions);
        DataStore.SetIfDifferent(Data.TriHorseStarvingDamageAmount, StarvingDamageAmount);
        DataStore.SetIfDifferent(Data.TriHorseStructureCollisionDamageAmount, StructureCollisionDamageAmount);
        DataStore.SetIfDifferent(Data.TriHorseMinFlockDispersion, FlockingOptions.MinDispersion);
        DataStore.SetIfDifferent(Data.TriHorseMaxFlockDispersion, FlockingOptions.MaxDispersion);
        DataStore.SetIfDifferent(Data.TriHorseWanderRadius, WanderParameters.Radius);
        DataStore.SetIfDifferent(Data.TriHorseWanderDistance, WanderParameters.Distance);
        DataStore.SetIfDifferent(Data.TriHorseWanderJitter, WanderParameters.Jitter);
        DataStore.SetIfDifferent(Data.TriHorseWanderWeight, FlockingOptions.WanderWeight);
        DataStore.SetIfDifferent(Data.TriHorseAlignWeight, FlockingOptions.AlignmentWeight);
        if (DataStore.SetIfDifferent(Data.TriHorseSensingRadius, SensingRadius))
        {
            if (m_FlockmateSensor != null)
                m_FlockmateSensor.Range = SensingRadius;
            NoduleSensor.Range = SensingRadius;
        }
    }

    public static void ChangeTriHorseData(Data key, string value, IEnumerable<TriHorse> triHorseEnum)
    {
        var triHorses = triHorseEnum.ToList();
        switch (key)
        {
            case Data.TriHorseInitialScale:
                var initScale = float.Parse(value);
                triHorses.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.TriHorseFinalScaleMin:
                var scaleMin = float.Parse(value);
                triHorses.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.TriHorseFinalScaleMax:
                var scaleMax = float.Parse(value);
                triHorses.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.TriHorseDaysBeforeReproducing:
                var beforeReproducing = float.Parse(value);
                triHorses.ForEach(h => h.DaysBeforeReproducing = beforeReproducing);
                break;
            case Data.TriHorseDaysBetweenReproductions:
                var betweenRepro = float.Parse(value);
                triHorses.ForEach(h => h.DaysBetweenReproductions = betweenRepro);
                break;
            case Data.TriHorseStarvingDamageAmount:
                var starvingDamage = float.Parse(value);
                triHorses.ForEach(h => h.StarvingDamageAmount = starvingDamage);
                break;
            case Data.TriHorseStructureCollisionDamageAmount:
                var collisionDamage = float.Parse(value);
                triHorses.ForEach(h => h.StructureCollisionDamageAmount = collisionDamage);
                break;
            case Data.TriHorseMinFlockDispersion:
                var minFlockDispersion = float.Parse(value);
                triHorses.ForEach(h =>
                {
                    h.FlockingOptions.MinDispersion = minFlockDispersion;
                    h.FlockingOptions.MinDispersionSquared = minFlockDispersion * minFlockDispersion;
                });
                break;
            case Data.TriHorseMaxFlockDispersion:
                var maxFlockDispersion = float.Parse(value);
                triHorses.ForEach(h =>
                {
                    h.FlockingOptions.MaxDispersion = maxFlockDispersion;
                    h.FlockingOptions.MaxDispersionSquared = maxFlockDispersion * maxFlockDispersion;
                });
                break;
            case Data.TriHorseSensingRadius:
                var senseRadius = float.Parse(value);
                triHorses.ForEach(h =>
                {
                    h.SensingRadius = senseRadius;
                    h.NoduleSensor.Range = senseRadius;
                });
                break;
            case Data.TriHorseWanderRadius:
                var wandRadius = float.Parse(value);
                triHorses.ForEach(h => h.WanderParameters.Radius = wandRadius);
                break;
            case Data.TriHorseWanderDistance:
                var wandDist = float.Parse(value);
                triHorses.ForEach(h => h.WanderParameters.Distance = wandDist);
                break;
            case Data.TriHorseWanderJitter:
                var wandJitter = float.Parse(value);
                triHorses.ForEach(h => h.WanderParameters.Jitter = wandJitter);
                break;
            case Data.TriHorseWanderWeight:
                var wandWeight = float.Parse(value);
                triHorses.ForEach(h => h.FlockingOptions.WanderWeight = wandWeight);
                break;
            case Data.TriHorseAlignWeight:
                var alignWeight = float.Parse(value);
                triHorses.ForEach(h => h.FlockingOptions.AlignmentWeight = alignWeight);
                break;
            case Data.TriHorseLifeSpan:
                var lifeSpan = float.Parse(value);
                triHorses.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.TriHorseBaseSpeed:
                var baseSpeed = float.Parse(value);
                triHorses.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.TriHorseDaysToGrown:
                var dtg = float.Parse(value);
                triHorses.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}

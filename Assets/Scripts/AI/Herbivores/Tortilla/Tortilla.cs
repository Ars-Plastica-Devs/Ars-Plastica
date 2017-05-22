using System.Collections.Generic;
using System.Linq;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(TortillaAnimAudioController))]
[SpawnableCreature("tortilla", HerbivoreType.Tortilla)]
public class Tortilla : HerbivoreBase
{
    private TortillaAnimAudioController m_AnimAudioController;

    private bool m_EatingNodule;

    private int m_DaysOfStarvingDamageTaken;
    private int m_NodulesEatenWhileFullLife;
    private int m_FlockingUpdateRate = 2;
    private int m_FlockingUpdateCounter;

    private float m_TimeSinceEating;
    private float m_LastDayOfReproduction;

    private IProximitySensor<Tortilla> m_FlockmateSensor;

    [SyncVar]
    public float Scale = 1f;

    public override HerbivoreType Type {
        get { return HerbivoreType.Tortilla; }
    }

    public override void OnStartServer()
    {
        m_AnimAudioController = GetComponent<TortillaAnimAudioController>();
        m_AnimAudioController.OnEatingFinished += OnEatingFinished;
        m_AnimAudioController.OnDyingFinished += Die;

        Grower = new ScaledGrowth(transform, 
            DataStore.GetFloat(Data.TortillaInitialScale), 
            DataStore.GetFloat(Data.TortillaFinalScaleMin), 
            DataStore.GetFloat(Data.TortillaFinalScaleMax));

        Grower.StartGrowing();
        Scale = Grower.Scale;

        AgeData.DaysToGrown = DataStore.GetFloat(Data.TortillaDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.TortillaLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.TortillaBaseSpeed);

        DaysBeforeReproducing = DataStore.GetFloat(Data.TortillaDaysBeforeReproducing);
        DaysBetweenReproductions = DataStore.GetFloat(Data.TortillaDaysBetweenReproductions);
        StarvingDamageAmount = DataStore.GetFloat(Data.TortillaStarvingDamageAmount);
        StructureCollisionDamageAmount = DataStore.GetFloat(Data.TortillaStructureCollisionDamageAmount);
        
        SensingRadius = DataStore.GetFloat(Data.TortillaSensingRadius);

        WanderParameters.Radius = DataStore.GetFloat(Data.TortillaWanderRadius);
        WanderParameters.Distance = DataStore.GetFloat(Data.TortillaWanderDistance);
        WanderParameters.Jitter = DataStore.GetFloat(Data.TortillaWanderJitter);
        FlockingOptions.WanderWeight = DataStore.GetFloat(Data.TortillaWanderWeight);
        FlockingOptions.AlignmentWeight = DataStore.GetFloat(Data.TortillaAlignWeight);
        FlockingOptions.MinDispersion = DataStore.GetFloat(Data.TortillaMinFlockDispersion);
        FlockingOptions.MaxDispersion = DataStore.GetFloat(Data.TortillaMaxFlockDispersion);
        FlockingOptions.MinDispersionSquared = FlockingOptions.MinDispersion * FlockingOptions.MinDispersion;
        FlockingOptions.MaxDispersionSquared = FlockingOptions.MaxDispersion * FlockingOptions.MaxDispersion;

        m_FlockmateSensor = new OctreeSensor<Tortilla>(transform, SensingRadius, 20, OctreeManager.Get(OctreeType.Herbivore));
        m_FlockmateSensor.RefreshRate = m_FlockmateSensor.RefreshRate.Randomize(.05f);

        base.OnStartServer();
        Scale = Grower.Scale;

        BehaviourBrain.In(BehaviourState.SeekingFood)
            .If(() => Health <= 0)
                .GoTo(BehaviourState.Death)
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
            .If(() => m_TimeSinceEating > DayClock.Singleton.DaysToSeconds(.4f))
                .GoTo(BehaviourState.SeekingFood)
            .If(CanReproduce)
                .GoTo(BehaviourState.Reproducing)
            .ExecuteWhileIn(Flocking);

        BehaviourBrain.In(BehaviourState.Eating)
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

        DataStore.SetIfDifferent(Data.TortillaDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.TortillaLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.TortillaBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.TortillaDaysBeforeReproducing, DaysBeforeReproducing);
        DataStore.SetIfDifferent(Data.TortillaDaysBetweenReproductions, DaysBetweenReproductions);
        DataStore.SetIfDifferent(Data.TortillaStarvingDamageAmount, StarvingDamageAmount);
        DataStore.SetIfDifferent(Data.TortillaStructureCollisionDamageAmount, StructureCollisionDamageAmount);
        DataStore.SetIfDifferent(Data.TortillaWanderRadius, WanderParameters.Radius);
        DataStore.SetIfDifferent(Data.TortillaWanderDistance, WanderParameters.Distance);
        DataStore.SetIfDifferent(Data.TortillaWanderJitter, WanderParameters.Jitter);
        DataStore.SetIfDifferent(Data.TortillaMinFlockDispersion, FlockingOptions.MinDispersion);
        DataStore.SetIfDifferent(Data.TortillaMaxFlockDispersion, FlockingOptions.MaxDispersion);
        DataStore.SetIfDifferent(Data.TortillaWanderWeight, FlockingOptions.WanderWeight);
        DataStore.SetIfDifferent(Data.TortillaAlignWeight, FlockingOptions.AlignmentWeight);
        if (DataStore.SetIfDifferent(Data.TortillaSensingRadius, SensingRadius))
        {
            if (m_FlockmateSensor != null)
                m_FlockmateSensor.Range = SensingRadius;
            NoduleSensor.Range = SensingRadius;
        }
    }

    public static void ChangeTortillaData(Data key, string value, IEnumerable<Tortilla> tortillaEnum)
    {
        var tortillas = tortillaEnum.ToList();
        switch (key)
        {
            case Data.TortillaInitialScale:
                var initScale = float.Parse(value);
                tortillas.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.TortillaFinalScaleMin:
                var scaleMin = float.Parse(value);
                tortillas.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.TortillaFinalScaleMax:
                var scaleMax = float.Parse(value);
                tortillas.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.TortillaDaysBeforeReproducing:
                var beforeReproducing = float.Parse(value);
                tortillas.ForEach(h => h.DaysBeforeReproducing = beforeReproducing);
                break;
            case Data.TortillaDaysBetweenReproductions:
                var betweenRepro = float.Parse(value);
                tortillas.ForEach(h => h.DaysBetweenReproductions = betweenRepro);
                break;
            case Data.TortillaStarvingDamageAmount:
                var starvingDamage = float.Parse(value);
                tortillas.ForEach(h => h.StarvingDamageAmount = starvingDamage);
                break;
            case Data.TortillaStructureCollisionDamageAmount:
                var collisionDamage = float.Parse(value);
                tortillas.ForEach(h => h.StructureCollisionDamageAmount = collisionDamage);
                break;
            case Data.TortillaMinFlockDispersion:
                var minFlockDispersion = float.Parse(value);
                tortillas.ForEach(h =>
                {
                    h.FlockingOptions.MinDispersion = minFlockDispersion;
                    h.FlockingOptions.MinDispersionSquared = minFlockDispersion * minFlockDispersion;
                });
                break;
            case Data.TortillaMaxFlockDispersion:
                var maxFlockDispersion = float.Parse(value);
                tortillas.ForEach(h =>
                {
                    h.FlockingOptions.MaxDispersion = maxFlockDispersion;
                    h.FlockingOptions.MaxDispersionSquared = maxFlockDispersion * maxFlockDispersion;
                });
                break;
            case Data.TortillaSensingRadius:
                var senseRadius = float.Parse(value);
                tortillas.ForEach(h =>
                {
                    h.SensingRadius = senseRadius;
                    h.NoduleSensor.Range = senseRadius;
                });
                break;
            case Data.TortillaWanderRadius:
                var wandRadius = float.Parse(value);
                tortillas.ForEach(h => h.WanderParameters.Radius = wandRadius);
                break;
            case Data.TortillaWanderDistance:
                var wandDist = float.Parse(value);
                tortillas.ForEach(h => h.WanderParameters.Distance = wandDist);
                break;
            case Data.TortillaWanderJitter:
                var wandJitter = float.Parse(value);
                tortillas.ForEach(h => h.WanderParameters.Jitter = wandJitter);
                break;
            case Data.TortillaWanderWeight:
                var wandWeight = float.Parse(value);
                tortillas.ForEach(h => h.FlockingOptions.WanderWeight = wandWeight);
                break;
            case Data.TortillaAlignWeight:
                var alignWeight = float.Parse(value);
                tortillas.ForEach(h => h.FlockingOptions.AlignmentWeight = alignWeight);
                break;
            case Data.TortillaLifeSpan:
                var lifeSpan = float.Parse(value);
                tortillas.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.TortillaBaseSpeed:
                var baseSpeed = float.Parse(value);
                tortillas.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.TortillaDaysToGrown:
                var dtg = float.Parse(value);
                tortillas.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}
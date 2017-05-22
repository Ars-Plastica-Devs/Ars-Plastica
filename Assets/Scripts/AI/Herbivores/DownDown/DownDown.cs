using System.Collections.Generic;
using System.Linq;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(DownDownAnimAudioController))]
[SpawnableCreature("down-down", HerbivoreType.DownDown)]
public class DownDown : HerbivoreBase
{
    private DownDownAnimAudioController m_AnimAudioController;

    private bool m_EatingNodule;
    private bool m_InDeathThrows;

    private int m_DaysOfStarvingDamageTaken;
    private int m_NodulesEatenWhileFullLife;
    /*private int m_FlockingUpdateRate = 2;
    private int m_FlockingUpdateCounter;*/

    private float m_TimeSinceEating;
    private float m_LastDayOfReproduction;

    private IProximitySensor<DownDown> m_FlockmateSensor;

    [SyncVar]
    public float Scale = 1f;

    public override HerbivoreType Type
    {
        get { return HerbivoreType.DownDown; }
    }

    public override void OnStartServer()
    {
        transform.rotation = Quaternion.AngleAxis(90f, Vector3.right);

        m_AnimAudioController = GetComponent<DownDownAnimAudioController>();
        m_AnimAudioController.OnEatingFinished += OnEatingFinished;
        m_AnimAudioController.OnDyingFinished += Die;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.DownDownInitialScale),
            DataStore.GetFloat(Data.DownDownFinalScaleMin),
            DataStore.GetFloat(Data.DownDownFinalScaleMax));

        Grower.StartGrowing();
        Scale = Grower.Scale;

        AgeData.DaysToGrown = DataStore.GetFloat(Data.DownDownDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.DownDownLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.DownDownBaseSpeed);

        DaysBeforeReproducing = DataStore.GetFloat(Data.DownDownDaysBeforeReproducing);
        DaysBetweenReproductions = DataStore.GetFloat(Data.DownDownDaysBetweenReproductions);
        StarvingDamageAmount = DataStore.GetFloat(Data.DownDownStarvingDamageAmount);
        StructureCollisionDamageAmount = DataStore.GetFloat(Data.DownDownStructureCollisionDamageAmount);
        SensingRadius = DataStore.GetFloat(Data.DownDownSensingRadius);

        WanderParameters.Radius = DataStore.GetFloat(Data.DownDownWanderRadius);
        WanderParameters.Distance = DataStore.GetFloat(Data.DownDownWanderDistance);
        WanderParameters.Jitter = DataStore.GetFloat(Data.DownDownWanderJitter);
        FlockingOptions.WanderWeight = DataStore.GetFloat(Data.DownDownWanderWeight);
        FlockingOptions.AlignmentWeight = DataStore.GetFloat(Data.DownDownAlignWeight);
        FlockingOptions.MinDispersion = DataStore.GetFloat(Data.DownDownMinFlockDispersion);
        FlockingOptions.MaxDispersion = DataStore.GetFloat(Data.DownDownMaxFlockDispersion);
        FlockingOptions.MinDispersionSquared = FlockingOptions.MinDispersion * FlockingOptions.MinDispersion;
        FlockingOptions.MaxDispersionSquared = FlockingOptions.MaxDispersion * FlockingOptions.MaxDispersion;

        m_FlockmateSensor = new OctreeSensor<DownDown>(transform, SensingRadius, 20, OctreeManager.Get(OctreeType.Herbivore));
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
            .ExecuteWhileIn(Wandering);

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
            Wandering();
            return;
        }
        
        var toTarget = (NoduleSensor.Closest.transform.position - transform.position);

        var dot = Vector3.Dot(toTarget, Vector3.down);
        if (dot < 0) //Is the nodule in the downward direction from where we are?
        {
            Wandering();
            return;
        }

        if (toTarget.sqrMagnitude < .2f)
            toTarget = transform.forward;

        var desiredVel = toTarget.normalized * BaseSpeed;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desiredVel, transform.up), 3f);
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

    private void Wandering()
    {
        //The DownDown flys almost exclusively down. Adding this tiny amount of 
        //downward velocity tends towards this behaviour
        var down = Vector3.down;
        var wanderAmount = Steering.Wander(gameObject, ref WanderParameters);
        var vel = ((down * .01f) + (wanderAmount * .99f)).normalized;

        if (vel == Vector3.zero)
            vel = transform.forward;

        //Use velocity so that physics continues to work
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel, transform.up), 5f);
        Rigidbody.velocity = vel * BaseSpeed;
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

        /*DataStore.SetIfDifferent(Data.DownDownInitialScale, m_Grower.InitialScale);
        DataStore.SetIfDifferent(Data.DownDownFinalScaleMin, m_Grower.FinalScaleMin);
        DataStore.SetIfDifferent(Data.DownDownFinalScaleMax, m_Grower.FinalScaleMax);*/

        if (AgeData == null)
            AgeData = GetComponent<AgeDataComponent>();

        DataStore.SetIfDifferent(Data.DownDownDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.DownDownLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.DownDownBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.DownDownDaysBeforeReproducing, DaysBeforeReproducing);
        DataStore.SetIfDifferent(Data.DownDownDaysBetweenReproductions, DaysBetweenReproductions);
        DataStore.SetIfDifferent(Data.DownDownStarvingDamageAmount, StarvingDamageAmount);
        DataStore.SetIfDifferent(Data.DownDownStructureCollisionDamageAmount, StructureCollisionDamageAmount);
        DataStore.SetIfDifferent(Data.DownDownMinFlockDispersion, FlockingOptions.MinDispersion);
        DataStore.SetIfDifferent(Data.DownDownMaxFlockDispersion, FlockingOptions.MaxDispersion);
        DataStore.SetIfDifferent(Data.DownDownWanderRadius, WanderParameters.Radius);
        DataStore.SetIfDifferent(Data.DownDownWanderDistance, WanderParameters.Distance);
        DataStore.SetIfDifferent(Data.DownDownWanderJitter, WanderParameters.Jitter);
        DataStore.SetIfDifferent(Data.DownDownWanderWeight, FlockingOptions.WanderWeight);
        DataStore.SetIfDifferent(Data.DownDownAlignWeight, FlockingOptions.AlignmentWeight);
        if (DataStore.SetIfDifferent(Data.DownDownSensingRadius, SensingRadius))
        {
            if (m_FlockmateSensor != null)
                m_FlockmateSensor.Range = SensingRadius;
            NoduleSensor.Range = SensingRadius;
        }
    }

    public static void ChangeDownDownData(Data key, string value, IEnumerable<DownDown> ownDownEnum)
    {
        var downDowns = ownDownEnum.ToList();
        switch (key)
        {
            case Data.DownDownInitialScale:
                var initScale = float.Parse(value);
                downDowns.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.DownDownFinalScaleMin:
                var scaleMin = float.Parse(value);
                downDowns.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.DownDownFinalScaleMax:
                var scaleMax = float.Parse(value);
                downDowns.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.DownDownDaysBeforeReproducing:
                var beforeReproducing = float.Parse(value);
                downDowns.ForEach(h => h.DaysBeforeReproducing = beforeReproducing);
                break;
            case Data.DownDownDaysBetweenReproductions:
                var betweenRepro = float.Parse(value);
                downDowns.ForEach(h => h.DaysBetweenReproductions = betweenRepro);
                break;
            case Data.DownDownStarvingDamageAmount:
                var starvingDamage = float.Parse(value);
                downDowns.ForEach(h => h.StarvingDamageAmount = starvingDamage);
                break;
            case Data.DownDownStructureCollisionDamageAmount:
                var collisionDamage = float.Parse(value);
                downDowns.ForEach(h => h.StructureCollisionDamageAmount = collisionDamage);
                break;
            case Data.DownDownMinFlockDispersion:
                var minFlockDispersion = float.Parse(value);
                downDowns.ForEach(h =>
                {
                    h.FlockingOptions.MinDispersion = minFlockDispersion;
                    h.FlockingOptions.MinDispersionSquared = minFlockDispersion * minFlockDispersion;
                });
                break;
            case Data.DownDownMaxFlockDispersion:
                var maxFlockDispersion = float.Parse(value);
                downDowns.ForEach(h =>
                {
                    h.FlockingOptions.MaxDispersion = maxFlockDispersion;
                    h.FlockingOptions.MaxDispersionSquared = maxFlockDispersion * maxFlockDispersion;
                });
                break;
            case Data.DownDownSensingRadius:
                var senseRadius = float.Parse(value);
                downDowns.ForEach(h =>
                {
                    h.SensingRadius = senseRadius;
                    h.NoduleSensor.Range = senseRadius;
                });
                break;
            case Data.DownDownWanderRadius:
                var wandRadius = float.Parse(value);
                downDowns.ForEach(h => h.WanderParameters.Radius = wandRadius);
                break;
            case Data.DownDownWanderDistance:
                var wandDist = float.Parse(value);
                downDowns.ForEach(h => h.WanderParameters.Distance = wandDist);
                break;
            case Data.DownDownWanderJitter:
                var wandJitter = float.Parse(value);
                downDowns.ForEach(h => h.WanderParameters.Jitter = wandJitter);
                break;
            case Data.DownDownWanderWeight:
                var wandWeight = float.Parse(value);
                downDowns.ForEach(h => h.FlockingOptions.WanderWeight = wandWeight);
                break;
            case Data.DownDownAlignWeight:
                var alignWeight = float.Parse(value);
                downDowns.ForEach(h => h.FlockingOptions.AlignmentWeight = alignWeight);
                break;
            case Data.DownDownLifeSpan:
                var lifeSpan = float.Parse(value);
                downDowns.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.DownDownBaseSpeed:
                var baseSpeed = float.Parse(value);
                downDowns.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.DownDownDaysToGrown:
                var dtg = float.Parse(value);
                downDowns.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}
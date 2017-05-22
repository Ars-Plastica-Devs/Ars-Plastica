using System.Collections.Generic;
using System.Linq;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
public class FloatGrassLargeBlade : FloatGrassBlade
{
    private float m_SpeedModifier; //Adds variability to this creatures speed
    private float m_LifespawnModifier; //Adds variability to this creatures lifespan
    private Vector3 m_FloatVel;
    private Vector3 m_StartPos;

    [SyncVar] private float m_Scale;

    public override PlantType Type
    {
        get { return PlantType.FloatGrassLargeBlade; }
    }

    public int NodulesPerDay;
    public float NoduleDispersalRange;

    public float CheckForPlayerRange = 10f;
    public float CheckForPlayerRate = 2f;
    private float m_CheckForPlayerCounter;

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.FloatGLBladeInitialYScale),
            DataStore.GetFloat(Data.FloatGLBladeFinalYScaleMin),
            DataStore.GetFloat(Data.FloatGLBladeFinalYScaleMax),
            ScaledGrowth.GrowthAxes.All);

        NoduleProducer = GetComponent<DaytimeNoduleProducer>() 
                         ?? gameObject.AddComponent<DaytimeNoduleProducer>();
        NoduleProducer.Type = NoduleType.Floating;

        DaysToGrown = DataStore.GetFloat(Data.FloatGLBladeDaysToGrown);
        FloatSpeed = DataStore.GetFloat(Data.FloatGLBladeFloatSpeed);
        FloatRange = DataStore.GetFloat(Data.FloatGLBladeFloatRange);
        CheckForPlayerRate = DataStore.GetFloat(Data.FloatGLBladePlayerCheckRate);
        CheckForPlayerRange = DataStore.GetFloat(Data.FloatGLBladePlayerCheckRange);
        NoduleProducer.NodulesPerCycle = DataStore.GetInt(Data.FloatGLBladeNodulesPerDay);
        NoduleProducer.NoduleDispersalRange = DataStore.GetInt(Data.FloatGLBladeNoduleDispersalRange);
        NodulesPerDay = NoduleProducer.NodulesPerCycle;
        NoduleDispersalRange = NoduleProducer.NoduleDispersalRange;

        m_FloatVel = new Vector3(0, Random.Range(0, 2) == 0 ? -FloatSpeed : FloatSpeed, 0);
        m_StartPos = transform.position;

        m_LifespawnModifier = Random.Range(.9f, 1.1f);
        m_SpeedModifier = Random.Range(.95f, 1.05f);

        GrowthBrain.In(GrowthState.Growing)
            .If(() => DaysOld > DaysToGrown * m_LifespawnModifier)
                .GoTo(GrowthState.Grown)
            .ExecuteOnEntry(GrowingStart)
            .ExecuteWhileIn(GrowingUpdate/*, AvoidPlayerUpdate*/, NoduleProducer.NoduleReleaseUpdate);

        GrowthBrain.In(GrowthState.Grown)
            .If(() => DaysOld > (DaysToGrown * 2f * m_LifespawnModifier))
                .GoTo(GrowthState.Dead)
            .ExecuteOnEntry(GrownStart)
            .ExecuteWhileIn(GrownUpdate/*, AvoidPlayerUpdate*/, NoduleProducer.NoduleReleaseUpdate);

        GrowthBrain.In(GrowthState.Dead)
            .DoOnce(Die)
                .If(() => true)
                    .GoTo(GrowthState.Growing);

        GrowthBrain.Initialize(GrowthState.Growing);
    }

    private void Update()
    {
        if (isClient && !isServer)
            transform.localScale = new Vector3(m_Scale, m_Scale, m_Scale);
    }

    public override void BladeUpdate(float dt)
    {
        if (DayClock.Singleton != null)
            DaysOld = DayClock.Singleton.SecondsToDays(Time.time - BirthTime);

        GrowthBrain.Update(dt);

        if (isServer && Grower != null && Grower.Scale.PercentDifference(m_Scale) > .03f)
            m_Scale = Grower.Scale;
    }

    public override void BladeFixedUpdate()
    {
        var outOfRange = Mathf.Abs(m_StartPos.y - transform.position.y) > FloatRange;

        //Change blade float direction if needed
        if (outOfRange)
            m_FloatVel = new Vector3(m_FloatVel.x, -m_FloatVel.y, m_FloatVel.x);

        transform.position += m_FloatVel * Time.fixedDeltaTime * m_SpeedModifier;
    }

    private void AvoidPlayerUpdate()
    {
        m_CheckForPlayerCounter += Time.deltaTime;
        if (m_CheckForPlayerCounter > CheckForPlayerRate)
        {
            m_CheckForPlayerCounter = 0f;
            var closestPlayer = OctreeManager.Get(OctreeType.Player).GetClosestObject(transform.position);

            var vectorToStart = (new Vector3(m_StartPos.x, transform.position.y, m_StartPos.z) - transform.position).normalized * FloatSpeed;

            //Make sure there is a player in range
            if (closestPlayer != null &&
                (closestPlayer.position - transform.position).sqrMagnitude < CheckForPlayerRange * CheckForPlayerRange)
            {
                var evadeVector = Steering.Evade(gameObject, closestPlayer.gameObject, FloatSpeed);

                //Go to start XZ pos if that is close to the evade vector
                m_FloatVel = Vector3.Angle(evadeVector, vectorToStart) < 90f
                            ? new Vector3(vectorToStart.x, m_FloatVel.y, vectorToStart.z)
                            : new Vector3(evadeVector.x, m_FloatVel.y, evadeVector.z);

                return;
            }

            //Are we not close to our starting position on the horizontal plane
            if ((transform.position - new Vector3(m_StartPos.x, transform.position.y, m_StartPos.z)).sqrMagnitude > 2f)
            {
                //Float towards start on the horizontal
                m_FloatVel = new Vector3(vectorToStart.x, m_FloatVel.y, vectorToStart.z);
            }
            else
            {
                m_FloatVel = new Vector3(0, m_FloatVel.y, 0);
            }
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.FloatGLBladeDaysToGrown, DaysToGrown);
        DataStore.SetIfDifferent(Data.FloatGLBladeFloatSpeed, FloatSpeed);
        DataStore.SetIfDifferent(Data.FloatGLBladeFloatRange, FloatRange);
        DataStore.SetIfDifferent(Data.FloatGLBladePlayerCheckRate, CheckForPlayerRate);
        DataStore.SetIfDifferent(Data.FloatGLBladePlayerCheckRange, CheckForPlayerRange);
        DataStore.SetIfDifferent(Data.FloatGLBladeNodulesPerDay, NodulesPerDay);
        DataStore.SetIfDifferent(Data.FloatGLBladeNoduleDispersalRange, NoduleDispersalRange);
    }

    public static void ChangeFloatGrassLargeBladeData(Data key, string value, IEnumerable<FloatGrassLargeBlade> bladeEnumerable)
    {
        //ToList for the easier foreach syntax
        var blades = bladeEnumerable.ToList();
        switch (key)
        {
            case Data.FloatGLBladeInitialYScale:
                var initScale = float.Parse(value);
                blades.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.FloatGLBladeFinalYScaleMin:
                var scaleMin = float.Parse(value);
                blades.ForEach(b => ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin);
                break;
            case Data.FloatGLBladeFinalYScaleMax:
                var scaleMax = float.Parse(value);
                blades.ForEach(b => ((ScaledGrowth)b.Grower).FinalScaleMax = scaleMax);
                break;
            case Data.FloatGLBladeDaysToGrown:
                var daysToGrown = float.Parse(value);
                blades.ForEach(b => b.DaysToGrown = daysToGrown);
                break;
            case Data.FloatGLBladeFloatSpeed:
                var speed = float.Parse(value);
                blades.ForEach(b => b.FloatSpeed = speed);
                break;
            case Data.FloatGLBladeFloatRange:
                var range = float.Parse(value);
                blades.ForEach(b => b.FloatRange = range);
                break;
            case Data.FloatGLBladePlayerCheckRate:
                var rate = float.Parse(value);
                blades.ForEach(b => b.CheckForPlayerRate = rate);
                break;
            case Data.FloatGLBladePlayerCheckRange:
                var playerRange = float.Parse(value);
                blades.ForEach(b => b.CheckForPlayerRange = playerRange);
                break;
            case Data.FloatGLBladeNodulesPerDay:
                var perDay = int.Parse(value);
                blades.ForEach(b =>
                {
                    b.NoduleProducer.NodulesPerCycle = perDay;
                    b.NodulesPerDay = perDay;
                });
                break;
            case Data.FloatGLBladeNoduleDispersalRange:
                var disperseRange = float.Parse(value);
                blades.ForEach(b =>
                {
                    b.NoduleProducer.NoduleDispersalRange = disperseRange;
                    b.NoduleDispersalRange = disperseRange;
                });
                break;
        }
    }
}

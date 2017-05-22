using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class FloatGrassSmallBlade : FloatGrassBlade
{
    private float m_SpeedModifier; //Adds variability to this creatures speed
    private float m_LifespawnModifier; //Adds variability to this creatures lifespan
    private Vector3 m_FloatVel;
    private Vector3 m_StartPos;

    [SyncVar] private float m_Scale;

    public override PlantType Type
    {
        get { return PlantType.FloatGrassSmallBlade; }
    }

    public int NodulesPerNight;
    public float NoduleDispersalRange;

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        NoduleProducer = GetComponent<NighttimeNoduleProducer>()
                         ?? gameObject.AddComponent<NighttimeNoduleProducer>();

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.FloatGSBladeInitialYScale),
            DataStore.GetFloat(Data.FloatGSBladeFinalYScaleMin),
            DataStore.GetFloat(Data.FloatGSBladeFinalYScaleMax),
            ScaledGrowth.GrowthAxes.Y);

        DaysToGrown = DataStore.GetFloat(Data.FloatGSBladeDaysToGrown);
        FloatSpeed = DataStore.GetFloat(Data.FloatGSBladeFloatSpeed);
        FloatRange = DataStore.GetFloat(Data.FloatGSBladeFloatRange);
        NoduleProducer.NodulesPerCycle = DataStore.GetInt(Data.FloatGSBladeNodulesPerNight);
        NoduleProducer.NoduleDispersalRange = DataStore.GetInt(Data.FloatGSBladeNoduleDispersalRange);
        NodulesPerNight = NoduleProducer.NodulesPerCycle;
        NoduleDispersalRange = NoduleProducer.NoduleDispersalRange;

        m_FloatVel = new Vector3(0, Random.Range(0, 2) == 0 ? -FloatSpeed : FloatSpeed, 0);
        m_StartPos = transform.position;

        m_LifespawnModifier = Random.Range(.9f, 1.1f);
        m_SpeedModifier = Random.Range(.95f, 1.05f);

        GrowthBrain.In(GrowthState.Growing)
            .If(() => DaysOld > DaysToGrown * m_LifespawnModifier)
                .GoTo(GrowthState.Grown)
            .ExecuteOnEntry(GrowingStart)
            .ExecuteWhileIn(GrowingUpdate, NoduleProducer.NoduleReleaseUpdate);

        GrowthBrain.In(GrowthState.Grown)
            .If(() => DaysOld > (DaysToGrown * 2f * m_LifespawnModifier))
                .GoTo(GrowthState.Dead)
            .ExecuteOnEntry(GrownStart)
            .ExecuteWhileIn(GrownUpdate, NoduleProducer.NoduleReleaseUpdate);

        GrowthBrain.In(GrowthState.Dead)
            .DoOnce(Die)
                .If(() => true)
                    .GoTo(GrowthState.Growing);

        GrowthBrain.Initialize(GrowthState.Growing);
    }

    private void Update()
    {
        if (isClient && !isServer)
            transform.localScale = new Vector3(transform.localScale.x, m_Scale, transform.localScale.z);
    }

    public override void BladeUpdate(float dt)
    {
        if (DayClock.Singleton != null)
            DaysOld = DayClock.Singleton.SecondsToDays(Time.time - BirthTime);

        GrowthBrain.Update(dt);

        if (isServer && Grower != null && Mathf.Abs(Grower.Scale - m_Scale) > (.03f * Grower.Scale))
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

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.FloatGSBladeDaysToGrown, DaysToGrown);
        DataStore.SetIfDifferent(Data.FloatGSBladeFloatSpeed, FloatSpeed);
        DataStore.SetIfDifferent(Data.FloatGSBladeFloatRange, FloatRange);
        DataStore.SetIfDifferent(Data.FloatGSBladeNodulesPerNight, NodulesPerNight);
        DataStore.SetIfDifferent(Data.FloatGSBladeNoduleDispersalRange, NoduleDispersalRange);
    }

    public static void ChangeFloatGrassSmallBladeData(Data key, string value, IEnumerable<FloatGrassSmallBlade> bladeEnumerable)
    {
        //ToList for the easier foreach syntax
        var blades = bladeEnumerable.ToList();
        switch (key)
        {
            case Data.FloatGSBladeInitialYScale:
                var initScale = float.Parse(value);
                blades.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.FloatGSBladeFinalYScaleMin:
                var scaleMin = float.Parse(value);
                blades.ForEach(b => ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin);
                break;
            case Data.FloatGSBladeFinalYScaleMax:
                var scaleMax = float.Parse(value);
                blades.ForEach(b => ((ScaledGrowth)b.Grower).FinalScaleMax = scaleMax);
                break;
            case Data.FloatGSBladeDaysToGrown:
                var daysToGrown = float.Parse(value);
                blades.ForEach(b => b.DaysToGrown = daysToGrown);
                break;
            case Data.FloatGSBladeFloatSpeed:
                var speed = float.Parse(value);
                blades.ForEach(b => b.FloatSpeed = speed);
                break;
            case Data.FloatGSBladeFloatRange:
                var range = float.Parse(value);
                blades.ForEach(b => b.FloatRange = range);
                break;
            case Data.FloatGSBladeNodulesPerNight:
                var perNight = int.Parse(value);
                blades.ForEach(b =>
                {
                    b.NoduleProducer.NodulesPerCycle = perNight;
                    b.NodulesPerNight = perNight;
                });
                break;
            case Data.FloatGSBladeNoduleDispersalRange:
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

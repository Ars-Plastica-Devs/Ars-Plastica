using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(SporeGunAnimAudioController))]
[SpawnableCreature("spore-gun", PlantType.SporeGun)]
public class SporeGun : PlantBase
{
    private bool m_ShotSpore;
    private SporeGunAnimAudioController m_AnimAudioController;
    private ScaledGrowth m_Grower;

    public override PlantType Type
    {
        get { return PlantType.SporeGun; }
    }

    public DaytimeNoduleProducer NoduleProducer;

    public AgeData AgeData;
    public float NoduleFiringSpread;
    public float NoduleSpeed;
    public int NodulesPerDay;

    [SyncVar] public float Scale;

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        m_AnimAudioController = GetComponent<SporeGunAnimAudioController>();

        if (NoduleProducer == null)
            NoduleProducer = GetComponent<DaytimeNoduleProducer>();
        if (NoduleProducer == null)
            Debug.LogError("NoduleProducer is set to null in SporeGun", this);

        NoduleProducer.Type = NoduleType.SporeGun;
        NoduleProducer.RotationOption = NoduleSpawnRotationOption.Self;
        NoduleProducer.enabled = false;

        m_Grower = new ScaledGrowth(transform, 
            DataStore.GetFloat(Data.SporeGunInitialScale),
            DataStore.GetFloat(Data.SporeGunFinalScaleMin),
            DataStore.GetFloat(Data.SporeGunFinalScaleMax));

        AgeData.LifeSpan = DataStore.GetFloat(Data.SporeGunLifeSpan);
        AgeData.DaysToGrown = DataStore.GetFloat(Data.SporeGunDaysToGrown);
        NodulesPerDay = DataStore.GetInt(Data.SporeGunNodulesPerDay);
        NoduleFiringSpread = DataStore.GetFloat(Data.SporeGunNoduleFiringSpread);
        NoduleSpeed = DataStore.GetFloat(Data.SporeGunNoduleSpeed);
        NoduleProducer.NodulesPerCycle = NodulesPerDay;
        NoduleProducer.NoduleDirectionSpread = NoduleFiringSpread;
        NoduleProducer.OnNoduleSpawned += OnNoduleSpawn;

        GrowthBrain.In(GrowthState.Growing)
            .If(() => AgeData.DaysOld > AgeData.DaysToGrown)
                .GoTo(GrowthState.Grown)
            .ExecuteOnEntry(GrowingStart)
            .ExecuteWhileIn(GrowingUpdate);

        GrowthBrain.In(GrowthState.Grown)
            .If(() => AgeData.DaysOld > AgeData.LifeSpan)
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
        if (Scale != 0f)
            transform.localScale = new Vector3(Scale, Scale, Scale);

        if (!isServer)
            return;

        AgeData.DaysOld = DayClock.Singleton.SecondsToDays(Time.time - BirthTime);

        GrowthBrain.Update(Time.deltaTime);

        if (isServer && m_Grower.Scale.PercentDifference(Scale) > .03f)
            Scale = m_Grower.Scale;
    }

    private void GrowingStart()
    {
        m_Grower.StartGrowing();
        Scale = m_Grower.Scale;
    }

    private void GrowingUpdate()
    {
        m_Grower.GrowthUpdate(AgeData.DaysOld / AgeData.DaysToGrown);
    }

    private void GrownStart()
    {
        NoduleProducer.enabled = true;
    }

    private void GrownUpdate()
    {
        if (!m_ShotSpore)
            return;

        m_ShotSpore = false;
        m_AnimAudioController.DoShoot();
    }

    private void Die()
    {
        Ecosystem.Singleton.KillPlant(this);
    }

    private void OnNoduleSpawn(Nodule n)
    {
        //Might seem kind of backwards, but this lets us keep all SporeGun handling in this class.
        ((SporeGunNodule) n).Speed = DataStore.GetFloat(Data.SporeGunNoduleSpeed);

        m_ShotSpore = true;
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.SporeGunLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.SporeGunDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.SporeGunNodulesPerDay, NodulesPerDay);
        DataStore.SetIfDifferent(Data.SporeGunNoduleFiringSpread, NoduleFiringSpread);
        DataStore.SetIfDifferent(Data.SporeGunNoduleSpeed, NoduleSpeed);
    }

    public static void ChangeSporeGunData(Data key, string value, IEnumerable<SporeGun> gunsEnumerable)
    {
        var guns = gunsEnumerable.ToList();
        switch (key)
        {
            case Data.SporeGunLifeSpan:
                var lifeSpan = float.Parse(value);
                guns.ForEach(b => b.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.SporeGunDaysToGrown:
                var daysToGrown = float.Parse(value);
                guns.ForEach(b => b.AgeData.DaysToGrown = daysToGrown);
                break;
            case Data.SporeGunInitialScale:
                var initScale = float.Parse(value);
                guns.ForEach(b => b.m_Grower.InitialScale = initScale);
                break;
            case Data.SporeGunFinalScaleMin:
                var scaleMin = float.Parse(value);
                guns.ForEach(b =>
                {
                    b.m_Grower.FinalScaleMin = scaleMin;
                    b.m_Grower.RecalculateFinalScale();
                });
                break;
            case Data.SporeGunFinalScaleMax:
                var scaleMax = float.Parse(value);
                guns.ForEach(b =>
                {
                    b.m_Grower.FinalScaleMax = scaleMax;
                    b.m_Grower.RecalculateFinalScale();
                });
                break;
            case Data.SporeGunNodulesPerDay:
                var nodulesPerDay = int.Parse(value);
                guns.ForEach(b =>
                {
                    b.NoduleProducer.NodulesPerCycle = nodulesPerDay;
                    b.NodulesPerDay = nodulesPerDay;
                });
                break;
            case Data.SporeGunNoduleFiringSpread:
                var firingSpread = float.Parse(value);
                guns.ForEach(b =>
                {
                    b.NoduleProducer.NoduleDirectionSpread = firingSpread;
                    b.NoduleFiringSpread = firingSpread;
                });
                break;
            case Data.SporeGunNoduleSpeed:
                var noduleSpeed = float.Parse(value);
                guns.ForEach(b => b.NoduleSpeed = noduleSpeed);
                break;
        }
    }
}

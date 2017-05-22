using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[SpawnableCreature("air-plant", PlantType.AirPlant)]
public class AirPlant : PlantBase
{
    private ScaledGrowth m_Grower;

    [SyncVar] private int m_MeshGroup;
    [SyncVar] private int m_IndexIsMeshGroup;

    public override PlantType Type
    {
        get { return PlantType.AirPlant; }
    }

    public float LifeSpan;
    public float DaysToGrown;
    public float DaysOld;

    [SyncVar] public float Scale;

    public GameObject[] MeshGroupOptions;

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        m_Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.AirPlantInitialScale),
            DataStore.GetFloat(Data.AirPlantFinalScaleMin),
            DataStore.GetFloat(Data.AirPlantFinalScaleMax));

        LifeSpan = DataStore.GetFloat(Data.AirPlantLifeSpan);
        DaysToGrown = DataStore.GetFloat(Data.AirPlantDaysToGrown);

        GrowthBrain.In(GrowthState.Growing)
            .If(() => DaysOld > DaysToGrown)
                .GoTo(GrowthState.Grown)
            .ExecuteOnEntry(GrowingStart)
            .ExecuteWhileIn(GrowingUpdate);

        GrowthBrain.In(GrowthState.Grown)
            .If(() => DaysOld > LifeSpan)
                .GoTo(GrowthState.Dead)
            .ExecuteOnEntry(GrownStart)
            .ExecuteWhileIn(GrownUpdate);

        GrowthBrain.In(GrowthState.Dead)
            .DoOnce(Die)
                .If(() => true)
                    .GoTo(GrowthState.Growing);

        GrowthBrain.Initialize(GrowthState.Growing);

        transform.rotation = Random.rotation;
    }

    public override void OnStartServer()
    {
        SelectMesh();
    }

    public override void OnStartClient()
    {
        MeshGroupOptions[m_MeshGroup].transform.GetChild(m_IndexIsMeshGroup).gameObject.SetActive(true);
    }

    private void Update()
    {
        if (Scale != 0f)
            transform.localScale = new Vector3(Scale, Scale, Scale);

        if (!isServer)
            return;

        DaysOld = DayClock.Singleton.SecondsToDays(Time.time - BirthTime);

        GrowthBrain.Update(Time.deltaTime);

        if (isServer && m_Grower.Scale.PercentDifference(Scale) > .03f)
            Scale = m_Grower.Scale;
    }

    private void GrowingStart()
    {
        m_Grower.StartGrowing();
    }

    private void GrowingUpdate()
    {
        m_Grower.GrowthUpdate(DaysOld / DaysToGrown);
    }

    private void GrownStart()
    {
    }

    private void GrownUpdate()
    {
    }

    private void Die()
    {
        Ecosystem.Singleton.KillPlant(this);
    }

    [Server]
    private void SelectMesh()
    {
        m_MeshGroup = Random.Range(0, MeshGroupOptions.Length);
        var meshGroup = MeshGroupOptions[m_MeshGroup];

        m_IndexIsMeshGroup = Random.Range(0, meshGroup.transform.childCount);
        meshGroup.transform.GetChild(m_IndexIsMeshGroup).gameObject.SetActive(true);
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.AirPlantLifeSpan, LifeSpan);
        DataStore.SetIfDifferent(Data.AirPlantDaysToGrown, DaysToGrown);
    }

    public static void ChangeAirPlantData(Data key, string value, IEnumerable<AirPlant> plantEnum)
    {
        var plants = plantEnum.ToList();
        switch (key)
        {
            case Data.AirPlantLifeSpan:
                var lifeSpan = float.Parse(value);
                plants.ForEach(b => b.LifeSpan = lifeSpan);
                break;
            case Data.AirPlantDaysToGrown:
                var daysToGrown = float.Parse(value);
                plants.ForEach(b => b.DaysToGrown = daysToGrown);
                break;
            case Data.AirPlantInitialScale:
                var initScale = float.Parse(value);
                plants.ForEach(b => b.m_Grower.InitialScale = initScale);
                break;
            case Data.AirPlantFinalScaleMin:
                var scaleMin = float.Parse(value);
                plants.ForEach(b =>
                {
                    b.m_Grower.FinalScaleMin = scaleMin;
                    b.m_Grower.RecalculateFinalScale();
                });
                break;
            case Data.AirPlantFinalScaleMax:
                var scaleMax = float.Parse(value);
                plants.ForEach(b =>
                {
                    b.m_Grower.FinalScaleMin = scaleMax;
                    b.m_Grower.RecalculateFinalScale();
                });
                break;
        }
    }
}

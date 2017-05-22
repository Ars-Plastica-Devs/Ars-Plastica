using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class FloatGrassSmallCluster : FloatGrassCluster
{
    private Vector3 m_FloatVel;
    private Vector3 m_StartPos;

    public override PlantType Type
    {
        get { return PlantType.FloatGrassSmallCluster; }
    }

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        MinBladecount = DataStore.GetInt(Data.FloatGSClusterMinBladeCount);
        MaxBladeCount = DataStore.GetInt(Data.FloatGSClusterMaxBladeCount);
        BladeSpawnHorizontalRange = DataStore.GetFloat(Data.FloatGSClusterSpawnHorizontalRange);
        BladeSpawnVerticalRange = DataStore.GetFloat(Data.FloatGSClusterSpawnVerticalRange);
        FloatSpeed = DataStore.GetFloat(Data.FloatGSClusterFloatSpeed);
        FloatRange = DataStore.GetFloat(Data.FloatGSClusterFloatRange);

        m_FloatVel = new Vector3(0, Random.Range(0, 2) == 0 ? -FloatSpeed : FloatSpeed, 0);
        m_StartPos = transform.position;

        SpawnBlades(PlantType.FloatGrassSmallBlade);
    }

    protected void SpawnBlades(PlantType bladePlantType)
    {
        var count = Random.Range(MinBladecount, MaxBladeCount);
        Blades = new FloatGrassBlade[count];

        for (var i = 0; i < count; i++)
        {
            var offset = Random.insideUnitSphere;
            offset = new Vector3(offset.x * BladeSpawnHorizontalRange, offset.y * BladeSpawnVerticalRange, offset.z * BladeSpawnHorizontalRange);
            Blades[i] = Ecosystem.Singleton.SpawnPlant(transform.position + offset, transform.rotation, bladePlantType).GetComponent<FloatGrassSmallBlade>();
            NetworkServer.Spawn(Blades[i].gameObject);
            Blades[i].transform.parent = transform;
        }
    }

    private const int UPDATE_RATE = 2; //# of frames to not update
    private int _updateCounter;
    private void Update()
    {
        if (!isServer)
            return;

        _updateCounter++;
        if ((_updateCounter > UPDATE_RATE))
        {
            _updateCounter = 0;
            UpdateBlades(Time.deltaTime * (UPDATE_RATE + 1));
        }
        
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        var outOfRange = Mathf.Abs(m_StartPos.y - transform.position.y) > FloatRange;

        //Change blade float direction if needed
        if (outOfRange)
            m_FloatVel = new Vector3(m_FloatVel.x, -m_FloatVel.y, m_FloatVel.x);

        transform.position += m_FloatVel * Time.fixedDeltaTime;

        FixedUpdateBlades();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.FloatGSClusterMinBladeCount, MinBladecount);
        DataStore.SetIfDifferent(Data.FloatGSClusterMaxBladeCount, MaxBladeCount);
        DataStore.SetIfDifferent(Data.FloatGSClusterSpawnHorizontalRange, BladeSpawnHorizontalRange);
        DataStore.SetIfDifferent(Data.FloatGSClusterSpawnVerticalRange, BladeSpawnVerticalRange);
        DataStore.SetIfDifferent(Data.FloatGSClusterFloatSpeed, FloatSpeed);
        DataStore.SetIfDifferent(Data.FloatGSClusterFloatRange, FloatRange);
    }

    public static void ChangeFloatGrassClusterData(Data key, string value, IEnumerable<FloatGrassSmallCluster> clusterEnumerable)
    {
        var clusters = clusterEnumerable.ToList();

        switch (key)
        {
            case Data.FloatGSClusterMinBladeCount:
                var minCount = int.Parse(value);
                clusters.ForEach(b => b.MinBladecount = minCount);
                break;
            case Data.FloatGSClusterMaxBladeCount:
                var maxCount = int.Parse(value);
                clusters.ForEach(b => b.MaxBladeCount = maxCount);
                break;
            case Data.FloatGSClusterSpawnHorizontalRange:
                var horiz = float.Parse(value);
                clusters.ForEach(b => b.BladeSpawnHorizontalRange = horiz);
                break;
            case Data.FloatGSClusterSpawnVerticalRange:
                var vert = float.Parse(value);
                clusters.ForEach(b => b.BladeSpawnVerticalRange = vert);
                break;
            case Data.FloatGSClusterFloatSpeed:
                var speed = float.Parse(value);
                clusters.ForEach(b => b.FloatSpeed = speed);
                break;
            case Data.FloatGSClusterFloatRange:
                var range = float.Parse(value);
                clusters.ForEach(b => b.FloatRange = range);
                break;
        }
    }
}

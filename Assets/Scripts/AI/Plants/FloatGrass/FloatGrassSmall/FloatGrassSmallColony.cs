using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[SpawnableCreature("floatgs-colony", PlantType.FloatGrassSmallColony)]
public class FloatGrassSmallColony : FloatGrassColony
{
    public override PlantType Type
    {
        get { return PlantType.FloatGrassSmallColony; }
    }

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        MinClusterCount = DataStore.GetInt(Data.FloatGSColonyMinClusterCount);
        MaxClusterCount = DataStore.GetInt(Data.FloatGSColonyMaxClusterCount);
        ClusterSpawnHorizontalRange = DataStore.GetInt(Data.FloatGSColonySpawnHorizontalRange);
        ClusterSpawnVerticalRange = DataStore.GetInt(Data.FloatGSColonySpawnVerticalRange);

        SpawnClusters(PlantType.FloatGrassSmallCluster);
    }

    protected void SpawnClusters(PlantType clusterType)
    {
        var count = Random.Range(MinClusterCount, MaxClusterCount);
        Clusters = new FloatGrassCluster[count];

        for (var i = 0; i < count; i++)
        {
            var offset = Random.insideUnitSphere;
            offset = new Vector3(offset.x * ClusterSpawnHorizontalRange, offset.y * ClusterSpawnVerticalRange, offset.z * ClusterSpawnHorizontalRange);
            Clusters[i] = Ecosystem.Singleton.SpawnPlant(transform.position + offset, transform.rotation, clusterType).GetComponent<FloatGrassSmallCluster>();
            NetworkServer.Spawn(Clusters[i].gameObject);
            Clusters[i].transform.parent = transform;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        if (MinClusterCount != DataStore.GetFloat(Data.FloatGSColonyMinClusterCount))
        {
            DataStore.Set(Data.FloatGSColonyMinClusterCount, MinClusterCount);
        }
        if (MaxClusterCount != DataStore.GetFloat(Data.FloatGSColonyMaxClusterCount))
        {
            DataStore.Set(Data.FloatGSColonyMaxClusterCount, MaxClusterCount);
        }
        if (ClusterSpawnHorizontalRange != DataStore.GetFloat(Data.FloatGSColonySpawnHorizontalRange))
        {
            DataStore.Set(Data.FloatGSColonySpawnHorizontalRange, ClusterSpawnHorizontalRange);
        }
        if (ClusterSpawnVerticalRange != DataStore.GetFloat(Data.FloatGSColonySpawnVerticalRange))
        {
            DataStore.Set(Data.FloatGSColonySpawnVerticalRange, ClusterSpawnVerticalRange);
        }
    }

    public static void ChangeFloatGrassSmallColonyData(Data key, string value, IEnumerable<FloatGrassSmallColony> coloniesEnumerable)
    {
        var colonies = coloniesEnumerable.ToList();

        switch (key)
        {
            case Data.FloatGSColonyMinClusterCount:
                var minCount = int.Parse(value);
                colonies.ForEach(b => b.MinClusterCount = minCount);
                break;
            case Data.FloatGSColonyMaxClusterCount:
                var maxCount = int.Parse(value);
                colonies.ForEach(b => b.MaxClusterCount = maxCount);
                break;
            case Data.FloatGSColonySpawnHorizontalRange:
                var horiz = float.Parse(value);
                colonies.ForEach(b => b.ClusterSpawnHorizontalRange = horiz);
                break;
            case Data.FloatGSColonySpawnVerticalRange:
                var vert = float.Parse(value);
                colonies.ForEach(b => b.ClusterSpawnVerticalRange = vert);
                break;
        }
    }
}

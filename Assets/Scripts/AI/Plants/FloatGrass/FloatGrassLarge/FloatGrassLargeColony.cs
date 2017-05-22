using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[SpawnableCreature("floatgl-colony", PlantType.FloatGrassLargeColony)]
public class FloatGrassLargeColony : FloatGrassColony
{
    public override PlantType Type
    {
        get { return PlantType.FloatGrassLargeColony; }
    }

    protected override void Start ()
	{
        base.Start();

	    if (!isServer)
	        return;

        MinClusterCount = DataStore.GetInt(Data.FloatGLColonyMinClusterCount);
        MaxClusterCount = DataStore.GetInt(Data.FloatGLColonyMaxClusterCount);
        ClusterSpawnHorizontalRange = DataStore.GetInt(Data.FloatGLColonySpawnHorizontalRange);
        ClusterSpawnVerticalRange = DataStore.GetInt(Data.FloatGLColonySpawnVerticalRange);

	    SpawnClusters(PlantType.FloatGrassLargeCluster);
	}

    protected void SpawnClusters(PlantType clusterType)
    {
        var count = Random.Range(MinClusterCount, MaxClusterCount);
        Clusters = new FloatGrassCluster[count];

        for (var i = 0; i < count; i++)
        {
            Invoke("SpawnSingleCluster", Random.value);
        }
    }

    private void SpawnSingleCluster()
    {
        var index = -1;
        for (var i = 0; i < Clusters.Length; i++)
        {
            if (Clusters[i] != null) continue;
            index = i;
            break;
        }
        if (index == -1)
            return;

        var offset = Random.insideUnitSphere;
        offset = new Vector3(offset.x * ClusterSpawnHorizontalRange, offset.y * ClusterSpawnVerticalRange, offset.z * ClusterSpawnHorizontalRange);
        Clusters[index] = Ecosystem.Singleton.SpawnPlant(transform.position + offset, transform.rotation, PlantType.FloatGrassLargeCluster).GetComponent<FloatGrassLargeCluster>();
        NetworkServer.Spawn(Clusters[index].gameObject);
        Clusters[index].transform.parent = transform;
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.FloatGLColonyMinClusterCount, MinClusterCount);
        DataStore.SetIfDifferent(Data.FloatGLColonyMaxClusterCount, MaxClusterCount);
        DataStore.SetIfDifferent(Data.FloatGLColonySpawnHorizontalRange, ClusterSpawnHorizontalRange);
        DataStore.SetIfDifferent(Data.FloatGLColonySpawnVerticalRange, ClusterSpawnVerticalRange);
    }

    public static void ChangeFloatGrassLargeColonyData(Data key, string value, IEnumerable<FloatGrassLargeColony> coloniesEnumerable)
    {
        var colonies = coloniesEnumerable.ToList();

        switch (key)
        {
            case Data.FloatGLColonyMinClusterCount:
                var minCount = int.Parse(value);
                colonies.ForEach(b => b.MinClusterCount = minCount);
                break;
            case Data.FloatGLColonyMaxClusterCount:
                var maxCount = int.Parse(value);
                colonies.ForEach(b => b.MaxClusterCount = maxCount);
                break;
            case Data.FloatGLColonySpawnHorizontalRange:
                var horiz = float.Parse(value);
                colonies.ForEach(b => b.ClusterSpawnHorizontalRange = horiz);
                break;
            case Data.FloatGLColonySpawnVerticalRange:
                var vert = float.Parse(value);
                colonies.ForEach(b => b.ClusterSpawnVerticalRange = vert);
                break;
        }
    }
}

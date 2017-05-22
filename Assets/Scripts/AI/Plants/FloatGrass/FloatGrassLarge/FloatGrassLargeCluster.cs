using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class FloatGrassLargeCluster : FloatGrassCluster
{
    private Vector3 m_FloatVel;
    private Vector3 m_StartPos;

    public override PlantType Type {
        get { return PlantType.FloatGrassLargeCluster; }
    }

    protected override void Start ()
    {
	    base.Start();

	    if (!isServer)
	        return;

        MinBladecount = DataStore.GetInt(Data.FloatGLClusterMinBladeCount);
        MaxBladeCount = DataStore.GetInt(Data.FloatGLClusterMaxBladeCount);
        BladeSpawnHorizontalRange = DataStore.GetFloat(Data.FloatGLClusterSpawnHorizontalRange);
        BladeSpawnVerticalRange = DataStore.GetFloat(Data.FloatGLClusterSpawnVerticalRange);
        FloatSpeed = DataStore.GetFloat(Data.FloatGLClusterFloatSpeed);
        FloatRange = DataStore.GetFloat(Data.FloatGLClusterFloatRange);

        m_FloatVel = new Vector3(0, Random.Range(0, 2) == 0 ? -FloatSpeed : FloatSpeed, 0);
        m_StartPos = transform.position;

        SpawnBlades(PlantType.FloatGrassLargeBlade);
    }

    protected void SpawnBlades(PlantType bladePlantType)
    {
        var count = Random.Range(MinBladecount, MaxBladeCount);
        Blades = new FloatGrassBlade[count];

        for (var i = 0; i < count; i++)
        {
            var offset = Random.insideUnitSphere;
            offset = new Vector3(offset.x * BladeSpawnHorizontalRange, offset.y * BladeSpawnVerticalRange, offset.z * BladeSpawnHorizontalRange);
            Blades[i] = Ecosystem.Singleton.SpawnPlant(transform.position + offset, transform.rotation, bladePlantType).GetComponent<FloatGrassLargeBlade>();
            NetworkServer.Spawn(Blades[i].gameObject);
            Blades[i].transform.parent = transform;
        }
    }

    private bool _UpdatedLastFrame;
    private void Update()
    {
        if (!isServer)
            return;

        if ((_UpdatedLastFrame = !_UpdatedLastFrame))
            UpdateBlades(Time.deltaTime * 2f);
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

        DataStore.SetIfDifferent(Data.FloatGLClusterMinBladeCount, MinBladecount);
        DataStore.SetIfDifferent(Data.FloatGLClusterMaxBladeCount, MaxBladeCount);
        DataStore.SetIfDifferent(Data.FloatGLClusterSpawnHorizontalRange, BladeSpawnHorizontalRange);
        DataStore.SetIfDifferent(Data.FloatGLClusterSpawnVerticalRange, BladeSpawnVerticalRange);
        DataStore.SetIfDifferent(Data.FloatGLClusterFloatSpeed, FloatSpeed);
        DataStore.SetIfDifferent(Data.FloatGLClusterFloatRange, FloatRange);
    }

    public static void ChangeFloatGrassClusterData(Data key, string value, IEnumerable<FloatGrassLargeCluster> clusterEnumerable)
    {
        var clusters = clusterEnumerable.ToList();

        switch (key)
        {
            case Data.FloatGLClusterMinBladeCount:
                var minCount = int.Parse(value);
                clusters.ForEach(b => b.MinBladecount = minCount);
                break;
            case Data.FloatGLClusterMaxBladeCount:
                var maxCount = int.Parse(value);
                clusters.ForEach(b => b.MaxBladeCount = maxCount);
                break;
            case Data.FloatGLClusterSpawnHorizontalRange:
                var horiz = float.Parse(value);
                clusters.ForEach(b => b.BladeSpawnHorizontalRange = horiz);
                break;
            case Data.FloatGLClusterSpawnVerticalRange:
                var vert = float.Parse(value);
                clusters.ForEach(b => b.BladeSpawnVerticalRange = vert);
                break;
            case Data.FloatGLClusterFloatSpeed:
                var speed = float.Parse(value);
                clusters.ForEach(b => b.FloatSpeed = speed);
                break;
            case Data.FloatGLClusterFloatRange:
                var range = float.Parse(value);
                clusters.ForEach(b => b.FloatRange = range);
                break;
        }
    }
}

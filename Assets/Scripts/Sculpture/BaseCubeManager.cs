using System.Collections.Generic;
using UnityEngine;

public abstract class BaseCubeManager : Sculpture
{
    protected List<GameObject> Cubes = new List<GameObject>();

    public string[] TriggeringTags = { "Player", "RemotePlayer" };
    public uint SideLength;
    public GameObject CubePrefab;

    public abstract float CubeSize { get; set; }

    protected abstract void SpawnCubes();
    protected abstract void RepositionCubes();

    protected virtual void Start()
    {
        if (CubePrefab == null)
            Debug.LogError("CubePrefab is null on BaseCubeManager", this);

        if (!isServer) return;

        SpawnCubes();
    }
}

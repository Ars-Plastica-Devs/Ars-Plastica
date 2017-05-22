using UnityEngine;

public abstract class SpawnablePlacer
{
    public abstract Vector3 GetSpawnPosition(GameObject sender);
    public abstract Quaternion GetSpawnRotation(GameObject sender);
}

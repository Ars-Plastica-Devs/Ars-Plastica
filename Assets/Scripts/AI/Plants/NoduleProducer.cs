using UnityEngine;

public enum NoduleSpawnRotationOption
{
    Identity,
    Self
}

public abstract class NoduleProducer : MonoBehaviour
{
    protected int NodulesSpawnedThisCycle;

    public int NodulesPerCycle;
    public float NoduleDispersalRange;
    /// <summary>
    /// The offset from the NoduleProducer's location to spawn the nodule, in local coordinates
    /// </summary>
    public Vector3 BaseNoduleSpawnOffset = Vector3.zero;
    public float NoduleDirectionSpread;

    public NoduleType Type = NoduleType.Floating;
    public NoduleSpawnRotationOption RotationOption = NoduleSpawnRotationOption.Identity;

    public delegate void OnNoduleSpawnDelegate(Nodule n);
    public event OnNoduleSpawnDelegate OnNoduleSpawned;

    public virtual void NoduleReleaseUpdate() { }

    protected void StartEmittingNodules()
    {
        if (!enabled)
            return;

        NodulesSpawnedThisCycle = 0;
        InvokeRepeating("EmitNodule", 0f, DayClock.Singleton.DaysToSeconds(.4f + (Random.value * .2f)) / NodulesPerCycle);
    }

    private void EmitNodule()
    {
        if (Ecosystem.Singleton == null || !Ecosystem.Singleton.CanAddNodule())
        {
            NodulesSpawnedThisCycle++;
            if (NodulesSpawnedThisCycle > NodulesPerCycle)
            {
                CancelInvoke("EmitNodule");
            }
            return;
        }
        

        var offsetX = Random.value * NoduleDispersalRange;
        var offsetZ = Random.value * NoduleDispersalRange;
        var pos = transform.position + transform.InverseTransformVector(BaseNoduleSpawnOffset) + transform.InverseTransformVector(new Vector3(offsetX, 0f, offsetZ));
        var rot = (RotationOption == NoduleSpawnRotationOption.Identity)
                        ? Quaternion.identity
                        : transform.rotation;

        if (NoduleDirectionSpread != 0f)
        {
            var eulerRandomness = new Vector3(Random.Range(-NoduleDirectionSpread, NoduleDirectionSpread), 
                                            Random.Range(-NoduleDirectionSpread, NoduleDirectionSpread));
            rot *= Quaternion.Euler(eulerRandomness);
        }

        var n = Ecosystem.Singleton.SpawnNodule(pos, rot, Type);

        if (OnNoduleSpawned != null)
            OnNoduleSpawned(n);

        NodulesSpawnedThisCycle++;
        if (NodulesSpawnedThisCycle > NodulesPerCycle)
        {
            CancelInvoke("EmitNodule");
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}

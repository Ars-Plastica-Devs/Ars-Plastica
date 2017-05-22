using UnityEngine;
using UnityEngine.Networking;

public class LocalPlayerLayerSet : NetworkBehaviour
{
    public string LayerName;
    public GameObject[] ObjectsToSet;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (!isLocalPlayer) return;

        var l = LayerMask.NameToLayer(LayerName);
        foreach (var o in ObjectsToSet)
        {
            o.layer = l;
        }

        Destroy(this);
    }
}

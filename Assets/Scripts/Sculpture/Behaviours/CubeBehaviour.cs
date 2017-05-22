using Assets.Scripts.Networking;
using UnityEngine;
using UnityEngine.Networking;

public abstract class CubeBehaviour : NetworkBehaviour, IClientSpawningListener
{
    public abstract NetworkInstanceId ParentNetID { get; set; }

    public override void OnStartClient()
    {
        //if (transform.parent == null)
            ClientSpawningBroadcaster.Singleton.RegisterListener(ParentNetID, this);
    }

    public void OnObjectSpawned(GameObject obj)
    {
        transform.SetParent(obj.transform, true);

        var cubeManager = obj.GetComponent<BaseCubeManager>();
        if (cubeManager != null)
        {
            transform.localScale = Vector3.one * cubeManager.CubeSize;
        }
    }
}
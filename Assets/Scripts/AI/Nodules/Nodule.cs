using UnityEngine;
using UnityEngine.Networking;

public abstract class Nodule : NetworkBehaviour
{
    [SyncVar]
    public short ID;

    [HideInInspector]
    public NoduleType Type;

    private void Start()
    {
        var net = GetComponent<NetworkProximityChecker>();

        if (net != null)
        {
            net.visUpdateInterval += Random.value * .5f;
        }

        var rel = GetComponent<NetworkRelevanceChecker>();

        if (rel != null)
        {
            rel.UpdateInterval += Random.value * .5f;
        }
    }

    [Server]
    public void ServerEnable(Vector3 pos, Quaternion rot)
    {
        RpcEnable(pos, rot);
        gameObject.SetActive(true);
        transform.position = pos;
        transform.rotation = rot;
    }

    [ClientRpc]
    private void RpcEnable(Vector3 pos, Quaternion rot)
    {
        gameObject.SetActive(true);
        transform.position = pos;
        transform.rotation = rot;
    }

    [Server]
    public void ServerDisable()
    {
        RpcDisable();
        gameObject.SetActive(false);
    }

    [ClientRpc]
    private void RpcDisable()
    {
        gameObject.SetActive(false);
    }

    public abstract void GetEaten(Creature eater);
}

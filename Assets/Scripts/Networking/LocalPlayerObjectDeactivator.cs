using UnityEngine;
using UnityEngine.Networking;

public class LocalPlayerObjectDeactivator : NetworkBehaviour
{
    public GameObject[] Objects;

    public override void OnStartLocalPlayer()
    {
        foreach (var b in Objects)
        {
            b.SetActive(false);
        }
    }
}

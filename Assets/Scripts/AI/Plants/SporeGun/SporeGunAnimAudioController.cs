using UnityEngine.Networking;

public class SporeGunAnimAudioController : AnimAudioController
{
    [Server]
    public void DoShoot()
    {
        if (!isClient)
            Anim.SetTrigger("shoot");

        RpcDoShoot();
    }

    [ClientRpc]
    private void RpcDoShoot()
    {
        Anim.SetTrigger("shoot");
    }
}

using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
// ReSharper disable ConvertClosureToMethodGroup

/// <summary>
/// Acts as a connection between the local HUDManager and the HUDManager on the server.
/// Allows the local HUD to send information to the server side.
/// </summary>
public class NetworkedPlayerHUDBridge : NetworkBehaviour
{
    private HUDManager m_HUD;

    private void Start()
    {
        m_HUD = HUDManager.Singleton;
    }

    [Client]
    public void PostDirectorMessage(string msg)
    {
        CmdPostDirectorMessage(msg);
    }

    [Command]
    private void CmdPostDirectorMessage(string msg)
    {
        m_HUD.ServerPostDirectorMessage(msg);
    }

    [Client]
    public void PostMessage(string msg)
    {
        CmdPostMessage(msg);
    }

    [Command]
    private void CmdPostMessage(string msg)
    {
        m_HUD.ServerPostMessage(msg);
    }

    /*[Server]
    public void ServerPlayerNameSet(string n)
    {
        //gameObject.name = n;
        //GameObject.FindGameObjectWithTag("MainUI").GetComponent<HUDManager>().ServerAddPlayer(gameObject);
        //RpcPlayerNameSet(n);
    }*/

    /*[ClientRpc]
    private void RpcPlayerNameSet(string n)
    {
        gameObject.name = n;
    }*/

    [Client]
    public void KickPlayer(string playerName)
    {
        CmdKickPlayer(playerName);
    }

    [Command]
    private void CmdKickPlayer(string playerName)
    {
        var player = GameObject.Find(playerName);

        player.GetComponent<NetworkedPlayerHUDBridge>().RpcDisconnectSelf();
    }

    [Client]
    public void TeleportPlayer(string playerName, Vector3 loc)
    {
        CmdTeleportPlayer(playerName, loc);
    }

    [Command]
    private void CmdTeleportPlayer(string playerName, Vector3 loc)
    {
        var player = GameObject.Find(playerName);
        player.transform.position = loc;
        player.GetComponent<NetworkedPlayerHUDBridge>().RpcTeleportSelf(loc);
    }

    [ClientRpc]
    private void RpcTeleportSelf(Vector3 loc)
    {
        transform.position = loc;
    }

    [ClientRpc]
    private void RpcDisconnectSelf()
    {
        if (!isLocalPlayer) return;
        NetworkManager.singleton.StopClient();
    }

    [Client]
    public void PlayAudioStream(string[] playerNames, string stream)
    {
        CmdPlayAudioStream(playerNames, stream);
    }

    [Command]
    private void CmdPlayAudioStream(string[] playerNames, string stream)
    {
        foreach (var player in playerNames.Select(s => GameObject.Find(s)))
        {
            player.GetComponent<NetworkedPlayerHUDBridge>().RpcPlayAudioStream(stream);
        }
    }

    [ClientRpc]
    private void RpcPlayAudioStream(string stream)
    {
        if (!isLocalPlayer) return;
        m_HUD.PlayAudioStream(stream);
    }
}

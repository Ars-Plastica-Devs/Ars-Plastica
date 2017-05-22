using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/*
 * Creates unique identity for client player.
 * Syncs name and avatar prefab to use, then sets them.
 * If this script is attached to the localPlayer (client's player), it tells the server it's identity.
 * If !localPlayer (networked player, the other players in your world), it gets identity from server and changes its gameObject on the client accordingly (name, playerAvatar)
 * */
public class Player_ID : NetworkBehaviour
{
    public delegate void PlayerInitializedDelegate(GameObject obj);
    /// <summary>
    /// Called on the server whenever a player is connected and
    /// has it's identity set
    /// </summary>
    public static event PlayerInitializedDelegate OnPlayerSetupComplete;

    private NetworkInstanceId m_PlayerNetID;

    [SyncVar(hook="OnPlayerIdentitySet")]
    public string PlayerUniqueIdentity;
   // [SyncVar]
   // public string PlayerAvatar; //avatar is synced by name of prefab

    public override void OnStartLocalPlayer()
    {
        m_PlayerNetID = GetNetIdentity();
        SetIdentity();
    }

    private void Start()
    {
        //SetAvatar();
        SetIdentity();

        if (isLocalPlayer)
        {
            var t = transform.Find("Name");
            if (t != null)
            {
                t.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    private void Update()
    {
        /*if (transform.name == "" || transform.name == "Player(Clone)")
        {
            SetIdentity();
        }*/
    }

    /*
	 * Notify server of our identity.
	 * */
    [Client]
    private NetworkInstanceId GetNetIdentity()
    {
        return GetComponent<NetworkIdentity>().netId;
    }


    private void SetIdentity()
    {
        if (!isLocalPlayer)
        {
            transform.name = PlayerUniqueIdentity;
            var t = transform.Find("Name");
            if (t != null)
            {
                t.GetComponent<TextMesh>().text = transform.name;
            }
        }
        else
        {
            transform.name = MakeUniqueIdentity();
            CmdTellServerMyIdentity(transform.name, GetEmail());
        }
    }

    private string GetEmail()
    {
        var nm = FindObjectOfType<NetworkManager>();
        if (nm != null)
        {
            return nm.GetComponent<ClientMenuHUD>().PlayerEmail.text;
        }
        return null;
    }

    string MakeUniqueIdentity()
    {
        var uniqueName = "Player " + m_PlayerNetID;

        var nm = FindObjectOfType<NetworkManager>();
        if (nm != null)
        {
            var setName = nm.GetComponent<ClientMenuHUD>().PlayerName.text;
            if (!setName.Equals(""))
            {
                uniqueName = setName;
            }
        }

        return uniqueName;
    }

    [Command]
    private void CmdTellServerMyIdentity(string playerName, string email)
    {
        transform.name = playerName;
        PlayerUniqueIdentity = playerName;

        EmailSaver.SaveEmail(email);

        if (OnPlayerSetupComplete != null)
            OnPlayerSetupComplete(gameObject);
    }

    private void OnPlayerIdentitySet(string id)
    {
        PlayerUniqueIdentity = id;
        transform.name = id;
        var t = transform.Find("Name");
        if (t != null)
        {
            t.GetComponent<TextMesh>().text = id;
        }
    }

    [Client]
    private string GetAvatarIdentity()
    {
        var nm = FindObjectOfType<NetworkManager>();
        var cmh = nm.GetComponent<ClientMenuHUD>();

        var avatarSelected = cmh != null 
            ? cmh.PlayerAvatar 
            : nm.GetComponent<ClientMenuHUD>().PossibleAvatars[0];

        return avatarSelected.name;

    }

    /*void SetAvatar()
    {
        GameObject avatar = null;
        var nm = FindObjectOfType<NetworkManager>();
        var cmh = nm.GetComponent<ClientMenuHUD>();
        var prefab = !isLocalPlayer 
            ? PlayerAvatar 
            : GetAvatarIdentity();

        foreach (var go in cmh.PossibleAvatars.Where(go => go.name == prefab))
        {
            avatar = go;
        }

        if (avatar == null)
        {
            avatar = cmh.PossibleAvatars[0];
        }

        avatar = Instantiate(avatar);
        avatar.transform.SetParent(transform);
        avatar.transform.localPosition = Vector3.zero;
    }*/
}

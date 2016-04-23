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
    private NetworkInstanceId m_PlayerNetID;

    [SyncVar]
    public string PlayerUniqueIdentity;
    [SyncVar]
    public string PlayerAvatar; //avatar is synced by name of prefab

    public override void OnStartLocalPlayer()
    {
        GetNetIdentity();
        SetIdentity();

    }

    private void Start()
    {
        SetAvatar();
    }

    private void Update()
    {
        if (transform.name == "" || transform.name == "Player(Clone)")
        {
            SetIdentity();
        }
    }

    /*
	 * Notify server of our identity.
	 * */
    [Client]
    void GetNetIdentity()
    {
        m_PlayerNetID = GetComponent<NetworkIdentity>().netId;
        CmdTellServerMyIdentity(MakeUniqueIdentity(), GetAvatarIdentity());
    }


    void SetIdentity()
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
        }
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
    void CmdTellServerMyIdentity(string name, string avatar)
    {
        PlayerUniqueIdentity = name;
        PlayerAvatar = avatar;
    }

    [Client]
    string GetAvatarIdentity()
    {
        var nm = FindObjectOfType<NetworkManager>();
        var cmh = nm.GetComponent<ClientMenuHUD>();

        var avatarSelected = cmh != null 
            ? cmh.PlayerAvatar 
            : nm.GetComponent<ClientMenuHUD>().PossibleAvatars[0];

        return avatarSelected.name;

    }

    void SetAvatar()
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
    }
}

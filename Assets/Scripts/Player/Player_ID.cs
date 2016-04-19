using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

/*
 * Creates unique identity for client player.
 * Syncs name and avatar prefab to use, then sets them.
 * If this script is attached to the localPlayer (client's player), it tells the server it's identity.
 * If !localPlayer (networked player, the other players in your world), it gets identity from server and changes its gameObject on the client accordingly (name, playerAvatar)
 * */
public class Player_ID : NetworkBehaviour {

	[SyncVar] public string playerUniqueIdentity;
	[SyncVar] public string playerAvatar; //avatar is synced by name of prefab

	private Renderer rend;
	private NetworkInstanceId playerNetID;
	private Transform myTransform;

	public override void OnStartLocalPlayer() {
		GetNetIdentity ();
		SetIdentity ();

	}

	void Start() {
		SetAvatar ();
	}

	// Use this for initialization
	void Awake () {
		myTransform = transform;
	}
	
	// Update is called once per frame
	void Update () {
		if(myTransform.name == "" || myTransform.name == "Player(Clone)") {
			SetIdentity();
		}
	}

	/*
	 * Notify server of our identity.
	 * */
	[Client]
	void GetNetIdentity() {
		playerNetID = GetComponent<NetworkIdentity> ().netId;
		CmdTellServerMyIdentity (MakeUniqueIdentity(), GetAvatarIdentity());
	}


	void SetIdentity() {
		if (!isLocalPlayer) {
			myTransform.name = playerUniqueIdentity;
			Transform t = transform.Find ("Name");
			if (t != null) {
				t.GetComponent<TextMesh> ().text = myTransform.name;
			}
		} else {
			myTransform.name = MakeUniqueIdentity ();
		}
	}

	string MakeUniqueIdentity() {
		string uniqueName = "Player " + playerNetID.ToString ();

		NetworkManager nm = FindObjectOfType<NetworkManager> ();
		if (nm != null) {
			string setName = nm.GetComponent<ClientMenuHUD> ().playerName.text;
			if (!setName.Equals("")) {
				uniqueName = setName;
			}
		} 

		return uniqueName;
 	}

		

	[Command]
	void CmdTellServerMyIdentity(string name, string avatar) {
		playerUniqueIdentity = name;
		playerAvatar = avatar;
	}

	[Client]
	string GetAvatarIdentity() {
		GameObject avatarSelected;
		NetworkManager nm = FindObjectOfType<NetworkManager> ();
		ClientMenuHUD cmh = nm.GetComponent<ClientMenuHUD> ();
		if (cmh != null) {
			avatarSelected = cmh.playerAvatar;
		} else {
			avatarSelected = nm.GetComponent<ClientMenuHUD> ().possibleAvatars [0];
		}

		return avatarSelected.name;

	}

	void SetAvatar() {
		GameObject _avatar = null;
		string prefab;
		NetworkManager nm = FindObjectOfType<NetworkManager> ();
		ClientMenuHUD cmh = nm.GetComponent<ClientMenuHUD> ();
		if (!isLocalPlayer) {
			prefab = playerAvatar;
		} else {
			prefab = GetAvatarIdentity();
		}

		foreach (GameObject go in cmh.possibleAvatars) {
			if (go.name == prefab) {
				_avatar = go;
			}
		}

		if (_avatar == null) {
			_avatar = cmh.possibleAvatars [0];
		}
		_avatar = Instantiate (_avatar);
		_avatar.transform.SetParent (this.transform);
		_avatar.transform.localPosition = Vector3.zero;
	}
		
}

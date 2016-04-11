using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Player_ID : NetworkBehaviour {

	[SyncVar] public string playerUniqueIdentity;
	[SyncVar] public string playerAvatar;



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
			Debug.Log ("name " + setName);
			if (!setName.Equals("")) {
				uniqueName = setName;
				Debug.Log (uniqueName);
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
			Debug.Log (avatarSelected + " ");
		} else {
			avatarSelected = nm.GetComponent<ClientMenuHUD> ().possibleAvatars [0];
			Debug.Log ("Uhoh");
		}

		return avatarSelected.name;

	}

	void SetAvatar() {
		GameObject _a = null;
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
				_a = go;
			}
		}

		if (_a == null) {
			_a = cmh.possibleAvatars [0];
		}
		Debug.Log ("setting avatar" + _a);
		_a = Instantiate (_a);
		_a.transform.SetParent (this.transform);
		_a.transform.localPosition = Vector3.zero;
	}
		
}

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Player_ID : NetworkBehaviour {

	[SyncVar] public string playerUniqueIdentity;
//	[SyncVar] public Color playerColor;

	public Renderer rend;

	private NetworkInstanceId playerNetID;
	private Transform myTransform;

	public override void OnStartLocalPlayer() {
		GetNetIdentity ();
		SetIdentity ();
	}

	// Use this for initialization
	void Awake () {
		myTransform = transform;
//		playerColor = Color.white;
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
		CmdTellServerMyIdentity (MakeUniqueIdentity(), MakeUniqueColor());
	}


	void SetIdentity() {
		if (!isLocalPlayer) {
			myTransform.name = playerUniqueIdentity;
		} else {
			myTransform.name = MakeUniqueIdentity ();
		}
	}

	string MakeUniqueIdentity() {
		string uniqueName = "Player " + playerNetID.ToString ();

		NetworkManager nm = FindObjectOfType<NetworkManager> ();
		if (nm) {
			string setName = nm.GetComponent<ClientMenuHUD> ().playerName.text;
			Debug.Log ("name " + setName);
			if (!setName.Equals("")) {
				uniqueName = setName;
			}
		} 

		return uniqueName;
 	}

	Color MakeUniqueColor() {
		NetworkManager nm = FindObjectOfType<NetworkManager> ();
		if (nm) {
			
			Color clr = nm.GetComponent<ClientMenuHUD> ().playerColor;
			return clr;
		} else {
			return Color.white;
		}
	}
		

	[Command]
	void CmdTellServerMyIdentity(string name, Color clr) {
		playerUniqueIdentity = name;
//		playerColor = clr;
	}
		
}

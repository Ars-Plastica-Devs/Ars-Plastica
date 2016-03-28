using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ServerStart : NetworkBehaviour {


	public NetworkIdentity[] serverIdentities;

	// Use this for initialization
	void Start () {
		if (isServer) {
			InstantiateServerIdentities ();
		}
	}



	void InstantiateServerIdentities() {
		foreach (NetworkIdentity nid in serverIdentities) {
			nid.enabled = true;
			nid.gameObject.SetActive (true);
		}
	}

	
	// Update is called once per frame
	void Update () {
	
	}
}

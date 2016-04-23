using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Server : NetworkBehaviour {

	public NetworkManager nm;

	public NetworkIdentity[] serverIdentities;

	// Use this for initialization
	void Start () {

		nm.StartServer();
		nm.ServerChangeScene (nm.onlineScene);

		InstantiateServerIdentities();

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

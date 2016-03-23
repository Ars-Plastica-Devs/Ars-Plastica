using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Server : NetworkBehaviour {

	public NetworkManager nm;

	// Use this for initialization
	void Start () {
		nm.StartServer();
		nm.ServerChangeScene (nm.onlineScene);

	}
		

	// Update is called once per frame
	void Update () {
	
	}
}

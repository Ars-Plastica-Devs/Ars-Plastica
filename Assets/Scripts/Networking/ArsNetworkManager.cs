using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ArsNetworkManager : NetworkManager {

	public bool isDevelopment = true;
	public bool selfContainedHost = true;
	public bool skipStartScreen = false;
	public bool isServer = false;
	public string productionServerAddress = "ps529225.dreamhostps.com";
	public string localServerAddress = "localhost";

	// Use this for initialization
	void Start () {
		if (isDevelopment) {
			this.networkAddress = localServerAddress;
		} else {
			this.networkAddress = productionServerAddress;
		}

		//Check if we should start a server in production
		if (isServer) {
			StartServer();
			ServerChangeScene (this.onlineScene);
			InstantiateServerIdentities();
		}
	}

	public override void OnStartClient (NetworkClient client)
	{

	}

	public void ArsStartClient() {
		if (isDevelopment && selfContainedHost) {
			this.StartHost ();
		} else {
			this.StartClient ();
		}
	}

	private void InstantiateServerIdentities() {

	}

	// Update is called once per frame
	void Update () {
	
	}
}

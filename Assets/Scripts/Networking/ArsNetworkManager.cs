using UnityEngine.Networking;

public class ArsNetworkManager : NetworkManager
{
    private bool m_ServerRunning;
	public bool IsDevelopment = true;
	public bool SelfContainedHost = true;
	public bool SkipStartScreen = false;
	public bool IsServer = false;
	public string ProductionServerAddress = "ps529225.dreamhostps.com";
	public string LocalServerAddress = "localhost";

	// Use this for initialization
	void Start ()
	{
	    networkAddress = IsDevelopment 
            ? LocalServerAddress 
            : ProductionServerAddress;

	    //Check if we should start a server in production
		if (IsServer)
		{
		    m_ServerRunning = true;
            StartServer();
			ServerChangeScene (onlineScene);
			InstantiateServerIdentities();
		}
	}

    public override void OnStartClient (NetworkClient client)
	{
		base.OnStartClient (client);
	}

	public void ArsStartClient() {
		if (IsDevelopment && SelfContainedHost && !m_ServerRunning)
		{
		    m_ServerRunning = true;
            StartHost ();
            NetworkServer.SpawnObjects();
        }
        else
        {
			StartClient ();
            NetworkServer.SpawnObjects();
        }
	}

	private void InstantiateServerIdentities() {

	}
}

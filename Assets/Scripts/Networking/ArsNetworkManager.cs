using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class ArsNetworkManager : NetworkManager
{
    private const int PLAYER_LOG_SIZE_LIMIT = 10000000;
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
            InvokeRepeating("LimitLogFile", 60f, 60f);
		}
	}

    private void LimitLogFile()
    {
        //This implementation only works on linux
        //TODO: Add a windows version for testing purposes
        if (Application.platform != RuntimePlatform.LinuxPlayer)
            return;

        var path = ".config/unity3d/" + Application.companyName + "/" + Application.productName + "/Player.log";

        //Trash the old data, keep only the most recent 5mb
        var playerLogFile = new FileInfo(path);
        if (playerLogFile.Exists && playerLogFile.Length > PLAYER_LOG_SIZE_LIMIT)
        {
            var bytes = new byte[5000000];

            //Read the last 5mb
            using (var br = new BinaryReader(new FileStream(playerLogFile.FullName, FileMode.Open, FileAccess.Read)))
            {
                br.BaseStream.Seek(-5000000, SeekOrigin.End);
                br.Read(bytes, 0, 5000000);
            }

            //Write the last 5mb
            using (var bw = new BinaryWriter(new FileStream(playerLogFile.FullName, FileMode.Create, FileAccess.Write)))
            {
                bw.Write(bytes);
            }
        }
    }

    public void ArsStartClient()
    {
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

	private void InstantiateServerIdentities()
    {

	}
}

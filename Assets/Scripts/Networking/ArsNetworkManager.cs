using UnityEngine;
using UnityEngine.Networking;

public static class MessageType
{
    /*public static short Motion = MsgType.Highest + 1;
    public static short CompressedMotion = MsgType.Highest + 2;
    public static short PositionChange = MsgType.Highest + 3;
    public static short RotationChange = MsgType.Highest + 4;
    public static short CompressedRotationChange = MsgType.Highest + 5;*/

    public static short Motion = 100;
    public static short CompressedMotion = 103;
    public static short PositionChange = 101;
    public static short RotationChange = 102;
    public static short CompressedRotationChange = 104;
}

public class ArsNetworkManager : NetworkManager
{
    public string CurrentVersionNumber; 
    private bool m_ServerRunning;
	public bool IsDevelopment = true;
	public bool SelfContainedHost = true;
	public bool SkipStartScreen = false;
	public bool IsServer = false;
	public string DHServerAddress = "ps529225.dreamhostps.com";
    public string AWSServerAddress = "spanglerlabs.net";
	public string LocalServerAddress = "localhost";

    public delegate void PlayerDisconnected(GameObject obj);
    public event PlayerDisconnected OnPlayerDisconnect;

    public ServerListing ServerListing;

    private void Awake()
    {
        //connectionConfig.MaxSentMessageQueueSize = 2048;
        //connectionConfig.MaxCombinedReliableMessageCount = 20;

        //Debug.Log("It's Alive! (ArsNetworkManager.cs)");
        useWebSockets = true;
        ServerListing =
            ServerListing.FromWebJSON(
                "https://docs.google.com/document/d/1LLT3FxiRC-NoI6OnRHlMUSd5O1_I4zB-ABDR4-wCNp0/export?format=txt");
        Debug.Log("Server Count: " + ServerListing.Servers.Length);
    }

    // Use this for initialization
	private void Start ()
	{
	    networkPort = 7777;
	    networkAddress = IsDevelopment 
            ? LocalServerAddress 
            : AWSServerAddress;

	    //Check if we should start a server in production
		if (IsServer)
		{
		    EmailSaver.SaveEmails = true;

		    networkAddress = !IsDevelopment 
                ? DataStore.Get(Data.ServerAddress) 
                : LocalServerAddress;

            m_ServerRunning = true;
            StartServer();
			ServerChangeScene(onlineScene);

            if (Application.isPlaying)
                InvokeRepeating("WriteToNetworkLog", 5f, 5f);
		}
    }

    public void ArsStartClient()
    {
		if (IsDevelopment && SelfContainedHost && !m_ServerRunning)
		{
		    m_ServerRunning = true;

		    StartHost();

            //Tried to start host, but host is already active. Connect as client
		    if (!NetworkServer.active)
		    {
                StartClient();
                RegisterMessageHandlers();
            }

            NetworkServer.SpawnObjects();
        }
        else
        {
			StartClient();
            RegisterMessageHandlers();
            NetworkServer.SpawnObjects();
        }
	}

    private void RegisterMessageHandlers()
    {
        client.RegisterHandler(MessageType.Motion, OnMotionMessage);
        client.RegisterHandler(MessageType.CompressedMotion, OnCompressedMotionMessage);
        client.RegisterHandler(MessageType.PositionChange, OnPositionChangeMessage);
        client.RegisterHandler(MessageType.RotationChange, OnRotationChangeMessage);
        client.RegisterHandler(MessageType.CompressedRotationChange, OnCompressedRotationChangeMessage);
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        conn.SetChannelOption(0, ChannelOption.MaxPendingBuffers, 48);
        base.OnServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        if (conn.playerControllers.Count == 0)
            return;

        var player = conn.playerControllers[0];

        if (OnPlayerDisconnect != null)
            OnPlayerDisconnect(player.gameObject);

        base.OnServerDisconnect(conn);
    }

    private static void OnMotionMessage(NetworkMessage netmsg)
    {
        var msg = netmsg.ReadMessage<MotionMessage>();
        var target = ClientScene.FindLocalObject(msg.TargetNetID);

        if (target == null)
            return;

        target.GetComponent<MotionSync>().ReceiveMotionMessage(msg);
    }

    private static void OnCompressedMotionMessage(NetworkMessage netmsg)
    {
        var msg = netmsg.ReadMessage<CompressedMotionMessage>();
        var target = ClientScene.FindLocalObject(msg.TargetNetID);

        if (target == null)
            return;

        target.GetComponent<MotionSync>().ReceiveCompressedMotionMessage(msg);
    }

    private static void OnPositionChangeMessage(NetworkMessage netmsg)
    {
        var msg = netmsg.ReadMessage<PositionChangeMessage>();
        var target = ClientScene.FindLocalObject(msg.TargetNetID);

        if (target == null)
            return;

        target.GetComponent<MotionSync>().ReceivePositionChangeMessage(msg);
    }

    private static void OnRotationChangeMessage(NetworkMessage netmsg)
    {
        var msg = netmsg.ReadMessage<RotationChangeMessage>();
        var target = ClientScene.FindLocalObject(msg.TargetNetID);

        if (target == null)
            return;

        target.GetComponent<MotionSync>().ReceiveRotationChangeMessage(msg);
    }

    private static void OnCompressedRotationChangeMessage(NetworkMessage netmsg)
    {
        var msg = netmsg.ReadMessage<CompressedRotationChangeMessage>();
        var target = ClientScene.FindLocalObject(msg.TargetNetID);

        if (target == null)
            return;

        target.GetComponent<MotionSync>().ReceiveRotationChangeMessage(msg);
    }
}

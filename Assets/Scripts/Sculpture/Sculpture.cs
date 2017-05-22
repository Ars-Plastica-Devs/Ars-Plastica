using System;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Sculpture : NetworkBehaviour, ICommandReceiver
{
    public abstract Dictionary<string, Func<string>> GetCurrentData();
    public abstract float GetBoundingSphereRadius();

    public override void OnStartClient()
    {
        base.OnStartClient();
        CommandProcessor.PendingReceivers.Add(gameObject);
        ClientSpawningBroadcaster.Singleton.OnNetIDSpawned(netId);
    }

    public virtual bool IsCommandRelevant(string cmd, GameObject sender = null)
    {
        return cmd.StartsWith(netId + " ");
    }

    public virtual string RunCommand(string cmd, GameObject sender)
    {
        var tokens = cmd.Split(' ');

        if (tokens.Length == 1)
            return "Not a valid command";

        if (tokens[1] == "delete" && tokens.Length == 2)
            return DoDeleteCommand();
        if (tokens[1] == "rotate" && tokens.Length == 5)
            return DoRotateCommand(float.Parse(tokens[2]), float.Parse(tokens[3]), float.Parse(tokens[4]));
        if (tokens[1] == "translate" && tokens.Length == 5)
            return DoTranslateCommand(float.Parse(tokens[2]), float.Parse(tokens[3]), float.Parse(tokens[4]));

        return "Not a valid command";
    }

    private string DoDeleteCommand()
    {
        NetworkServer.Destroy(gameObject);
        return "Destroyed the Sculpture";
    }

    private string DoRotateCommand(float x, float y, float z)
    {
        var rot = Quaternion.Euler(x, y, z);
        transform.rotation *= rot;

        return "";
    }

    private string DoTranslateCommand(float x, float y, float z)
    {
        transform.position += new Vector3(x, y, z);

        return "";
    }
}

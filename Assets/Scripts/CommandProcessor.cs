using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CommandProcessor : NetworkBehaviour, ICommandReceiver
{
    public static HashSet<GameObject> PendingReceivers = new HashSet<GameObject>(); 

    //TODO: Cache the FirstPersonConotroller
    private readonly HashSet<GameObject> m_Receivers = new HashSet<GameObject>();
    private bool m_SentCommand;
    private string m_Output;
    private Vector3 m_StartingPosition; //used for the /home command
    public Action<string> OnOutputReceived;

    private void Start()
    {
        enabled = isLocalPlayer;
    }

    public override void OnStartServer()
    {
        m_StartingPosition = transform.position;
    }

    public override void OnStartLocalPlayer()
    {
        RegisterReceiver(gameObject);
    }

    private void Update()
    {
        if (PendingReceivers.Count == 0)
            return;

        foreach (var pendingReceiver in PendingReceivers)
        {
            RegisterReceiver(pendingReceiver);
        }
        PendingReceivers.Clear();
    }

    public void DeactivateGameInput()
    {
        if (!isLocalPlayer) return;
        GetComponent<FirstPersonController>().enabled = false;
        GetComponent<FirstPersonController>().MouseOnly = false;
    }

    public void EnableInput()
    {
        if (!isLocalPlayer) return;
        GetComponent<FirstPersonController>().enabled = true;
        GetComponent<FirstPersonController>().MouseOnly = false;
    }

    public void ProcessCommand(string cmd)
    {
        if (!isLocalPlayer) return;

        //EnableInput();

        Debug.Log(cmd);
        m_SentCommand = true;
        Cmd_ProcessCommand(cmd, gameObject);
    }

    [Command]
    private void Cmd_ProcessCommand(string cmd, GameObject sender)
    {
        if (cmd.StartsWith("data "))
        {
            var tokens = cmd.Split(' ');
            DataStore.Set(tokens[1], tokens[2]);
            return;
        }

        m_Receivers.RemoveWhere(go => go == null);

        foreach (var receiver in m_Receivers.Where(rec => rec != null).SelectMany(go => go.GetComponents<ICommandReceiver>()))
        {
            if (receiver.IsCommandRelevant(cmd, sender))
            {
                try
                {
                    Debug.Log("Running " + cmd);
                    var result = receiver.RunCommand(cmd, sender);
                    RpcReceiveOutput(result);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    RpcReceiveOutput("The command '" + cmd + "' threw an exception: " + e.Message + "\n\t" + e.StackTrace);
                }
                return;
            }
        }

        RpcReceiveOutput("No receivers for this command");
    }

    [ClientRpc]
    private void RpcReceiveOutput(string output)
    {
        if (m_SentCommand)
        {
            m_SentCommand = false;
            m_Output = output;

            if (OnOutputReceived != null)
                OnOutputReceived(m_Output);
        }
    }

    private void RegisterReceiver(GameObject rec)
    {
        CmdRegisterReceiver(rec);
    }

    [Command]
    private void CmdRegisterReceiver(GameObject rec)
    {
        m_Receivers.Add(rec);
    }

    public bool IsCommandRelevant(string cmd, GameObject sender = null)
    {
        return (sender == gameObject) && (cmd.StartsWith("set-loc") || cmd.StartsWith("home"));
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        if (sender != gameObject)
            return "";

        var tokens = cmd.Split(' ');

        if (tokens[0] == "home")
        {
            var command = new[]
            {
                "set-loc",
                m_StartingPosition.x.ToString(),
                m_StartingPosition.y.ToString(),
                m_StartingPosition.z.ToString()
            };

            return DoSetLocationCommand(command, sender);
        }
        if (tokens[0] == "set-loc")
        {
            return DoSetLocationCommand(tokens, sender);
        }

        return "";
    }

    private string DoSetLocationCommand(string[] tokens, GameObject sender)
    {
        var x = float.Parse(tokens[1]);
        var y = float.Parse(tokens[2]);
        var z = float.Parse(tokens[3]);
        var pos = new Vector3(x, y, z);

        sender.transform.position = pos;
        RpcSetLocation(pos);

        return "Set your current location to " + x + " " + y + " " + z;
    }

    [ClientRpc]
    private void RpcSetLocation(Vector3 loc)
    {
        transform.position = loc;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CommandProcessor : NetworkBehaviour, ICommandReceiver
{
    //TODO: Cache the FirstPersonConotroller
    private readonly HashSet<GameObject> m_Receivers = new HashSet<GameObject>();
    private bool m_SentCommand;
    private string m_Output;
    private Vector3 m_StartingPosition; //used for the /home command
    public Action<string> OnOutputReceived;

    private void Start()
    {
        enabled = isLocalPlayer;

        if (isLocalPlayer)
        {
            RegisterAsReceiver();
        }

        if (isServer)
        {
            m_StartingPosition = transform.position;
        }
    }

    private void RegisterAsReceiver()
    {
        RegisterReceiver(gameObject);
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

        EnableInput();

        Debug.Log(cmd);
        m_SentCommand = true;
        Cmd_ProcessCommand(cmd, gameObject);
    }

    [Command]
    private void Cmd_ProcessCommand(string cmd, GameObject sender)
    {
        foreach (var receiver in m_Receivers.Where(rec => rec != null).Select(go => go.GetComponent<ICommandReceiver>()))
        {
            if (receiver.IsCommandRelevant(cmd))
            {
                var result = receiver.RunCommand(cmd, sender);
                RpcReceiveOutput(result);
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

    public void RegisterReceiver(GameObject rec)
    {
        CmdRegisterReceiver(rec);
    }

    [Command]
    private void CmdRegisterReceiver(GameObject rec)
    {
        m_Receivers.Add(rec);
    }

    public bool IsCommandRelevant(string cmd)
    {
        return cmd.StartsWith("set-loc") || cmd.StartsWith("home");
    }

    public string RunCommand(string cmd, GameObject sender)
    {
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

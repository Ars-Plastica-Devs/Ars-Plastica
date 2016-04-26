using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class CommandProcessor : NetworkBehaviour
{
    //TODO: Cache the FirstPersonConotroller
    private readonly HashSet<GameObject> m_Receivers = new HashSet<GameObject>();
    private bool m_SentCommand;
    private string m_Output;
    public Action<string> OnOutputReceived;

    private void Start()
    {
        enabled = isLocalPlayer;
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
}

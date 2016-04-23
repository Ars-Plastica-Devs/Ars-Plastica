using UnityEngine;
using UnityEngine.Networking;

public class MakeTransparent : NetworkBehaviour
{
    private int m_TriggeredCount;

    public string TriggeringTag = "Player";

    [SyncVar]
    public bool Visible = true;

    //Needs to be here to be able to enable/disable in the editor
    private void Update()
    {
    }

    [ClientRpc]
    public void RpcActivateBehaviour()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }

    [ClientRpc]
    public void RpcDeactivateBehaviour()
    {
        GetComponent<MeshRenderer>().enabled = true;
    }

    public void ActivateBehaviour()
    {
        GetComponent<MeshRenderer>().enabled = false;
        RpcActivateBehaviour();
    }

    public void DeactivateBehaviour()
    {
        GetComponent<MeshRenderer>().enabled = true;
        RpcDeactivateBehaviour();
    }

    public void Reset()
    {
        m_TriggeredCount = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || !isServer) return;

        if (other.gameObject.tag == TriggeringTag)
        {
            m_TriggeredCount++;

            if (m_TriggeredCount == 1)
            {
                ActivateBehaviour();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled || !isServer) return;

        if (other.gameObject.tag == TriggeringTag)
        {
            m_TriggeredCount--;

            if (m_TriggeredCount == 0)
            {
                DeactivateBehaviour();
            }
        }
    }
}
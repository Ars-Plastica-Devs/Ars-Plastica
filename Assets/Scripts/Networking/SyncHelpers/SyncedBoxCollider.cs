using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Syncs isTrigger, size, and center of a BoxCollider across the network
/// </summary>
public class SyncedBoxCollider : NetworkBehaviour
{
    [SyncVar(hook="OnIsTriggerChange")] private bool m_IsTrigger;
    [SyncVar(hook="OnSizeChange")] private Vector3 m_Size;
    [SyncVar(hook="OnCenterChange")] private Vector3 m_Center;

    public BoxCollider Collider;

    public bool IsTrigger
    {
        get { return Collider.isTrigger; }
        set
        {
            Collider.isTrigger = value;
            m_IsTrigger = value;
        }
    }

    public Vector3 Size
    {
        get { return Collider.size; }
        set
        {
            Collider.size = value;
            m_Size = value;
        }
    }

    public Vector3 Center
    {
        get { return Collider.center; }
        set
        {
            Collider.center = value;
            m_Center = value;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        m_IsTrigger = Collider.isTrigger;
        m_Center = Collider.center;
        m_Size = Collider.size;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Collider.isTrigger = m_IsTrigger;
        Collider.center = m_Center;
        Collider.size = m_Size;
    }

    private void OnIsTriggerChange(bool newVal)
    {
        m_IsTrigger = newVal;
        Collider.isTrigger = m_IsTrigger;
    }

    private void OnSizeChange(Vector3 newVal)
    {
        m_Size = newVal;
        Collider.size = m_Size;
    }

    private void OnCenterChange(Vector3 newVal)
    {
        m_Center = newVal;
        Collider.center = m_Center;
    }
}

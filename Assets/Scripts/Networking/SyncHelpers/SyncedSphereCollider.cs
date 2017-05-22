using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Syncs trigger, radius, and center of a SphereCollider across the network
/// </summary>
public class SyncedSphereCollider : NetworkBehaviour
{
    [SyncVar(hook="OnIsTriggerChange")] private bool m_IsTrigger;
    [SyncVar(hook="OnRadiusChange")] private float m_Radius;
    [SyncVar(hook="OnCenterChange")] private Vector3 m_Center;

    public SphereCollider Collider;

    public bool IsTrigger
    {
        get { return Collider.isTrigger; }
        set
        {
            Collider.isTrigger = value;
            m_IsTrigger = value;
        }
    }

    public float Radius
    {
        get { return Collider.radius; }
        set
        {
            Collider.radius = value;
            m_Radius = value;
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
        m_Radius = Collider.radius;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Collider.isTrigger = m_IsTrigger;
        Collider.center = m_Center;
        Collider.radius = m_Radius;
    }

    private void OnIsTriggerChange(bool newVal)
    {
        m_IsTrigger = newVal;
        Collider.isTrigger = m_IsTrigger;
    }

    private void OnRadiusChange(float newVal)
    {
        m_Radius = newVal;
        Collider.radius = m_Radius;
    }

    private void OnCenterChange(Vector3 newVal)
    {
        m_Center = newVal;
        Collider.center = m_Center;
    }

    //Somehow, this is fixing a NetworkReader.ReadByte out of range error.
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        base.OnDeserialize(reader, initialState);
    }
}

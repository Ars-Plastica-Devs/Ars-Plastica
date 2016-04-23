using UnityEngine;
using UnityEngine.Networking;

public class NoduleController : NetworkBehaviour
{
    private Rigidbody m_Rigidbody;
    private float m_Counter;
    public float SendRate = 1f;
    public float FloatSpeed = 1f;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.velocity = new Vector3(0, FloatSpeed, 0);

        if (!isServer) return;

        RpcPostMovementData(transform.position, m_Rigidbody.velocity);
    }

    private void Update()
    {
        m_Rigidbody.velocity = new Vector3(0, FloatSpeed, 0);
        if (!isServer) return;

        m_Counter += Time.deltaTime;
        if (m_Counter > SendRate)
        {
            m_Counter = 0f;
            RpcPostMovementData(transform.position, m_Rigidbody.velocity);
        }
    }

    [ClientRpc]
    private void RpcPostMovementData(Vector3 pos, Vector3 vel)
    {
        transform.position = pos;
        m_Rigidbody.velocity = vel;
    }
}

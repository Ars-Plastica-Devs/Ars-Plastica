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

        if (!isServer) return;

        FloatSpeed = DataStore.GetFloat("NoduleFloatSpeed");

        RpcPostMovementData(transform.position, m_Rigidbody.velocity);
        RpcSetFloatSpeed(FloatSpeed);
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
        if (m_Rigidbody == null) m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.velocity = vel;
    }

    [ClientRpc]
    private void RpcSetFloatSpeed(float floatSpeed)
    {
        FloatSpeed = floatSpeed;
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        if (FloatSpeed != DataStore.GetFloat("NoduleFloatSpeed"))
        {
            DataStore.Set("NoduleFloatSpeed", FloatSpeed);
        }
    }
}

using UnityEngine;
using UnityEngine.Networking;

public class MotionSync : NetworkBehaviour
{
    [SyncVar] private Vector3 m_SyncPos;
    [SyncVar] private Quaternion m_SyncRot;

    private Vector3 m_LastPos;
    private Quaternion m_LastRot;

    public float LerpRate = 10f;
    public float PosThreshold = .5f;
    public float RotThreshold = 2;

    private void Update()
    {
        TransmitMotion();
        LerpMotion();
    }

    private void TransmitMotion()
    {
        if (!isServer)
        {
            return;
        }

        if (Vector3.Distance(transform.position, m_LastPos) > PosThreshold || Quaternion.Angle(transform.rotation, m_LastRot) > RotThreshold)
        {
            m_LastPos = transform.position;
            m_LastRot = transform.rotation;

            m_SyncPos = transform.position;
            m_SyncRot = transform.rotation;
        }
    }

    private void LerpMotion()
    {
        if (isServer)
        {
            return;
        }

        transform.position = Vector3.Lerp(transform.position, m_SyncPos, Time.deltaTime * LerpRate);

        transform.rotation = Quaternion.Lerp(transform.rotation, m_SyncRot, Time.deltaTime * LerpRate);
    }
}

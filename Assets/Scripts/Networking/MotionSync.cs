using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MotionMessage : MessageBase
{
    public NetworkInstanceId TargetNetID;
    public Vector3 Pos;
    public Quaternion Rot;
}

public class CompressedMotionMessage : MessageBase
{
    public NetworkInstanceId TargetNetID;
    public Vector3 Pos;
    public short RotX;
    public short RotY;
    public short RotZ;
}

public class PositionChangeMessage : MessageBase
{
    public NetworkInstanceId TargetNetID;
    public Vector3 Pos;
}

public class RotationChangeMessage : MessageBase
{
    public NetworkInstanceId TargetNetID;
    public Quaternion Rot;
}

public class CompressedRotationChangeMessage : MessageBase
{
    public NetworkInstanceId TargetNetID;
    public short X;
    public short Y;
    public short Z;
}

[NetworkSettings(channel = 1, sendInterval = 0.1f)]
public class MotionSync : NetworkBehaviour
{
    private NetworkRelevanceChecker m_RelChecker;
    [SyncVar] private Vector3 m_SyncPos;
    [SyncVar] private Quaternion m_SyncRot;

    private Vector3 m_LastPos;
    private Quaternion m_LastRot;

    public float LerpRate = 10f;
    public float PosThreshold = .5f;
    public float RotThreshold = 2;
    public float SnapThreshold = 5f;

    public float SendRate = .1f;
    private float m_SendRateCounter;

    public bool UseRelevance = true;
    public bool CompressRotation = true;

    private void Awake()
    {
        SendRate = Random.Range(SendRate * .95f, SendRate * 1.05f);

        m_RelChecker = GetComponent<NetworkRelevanceChecker>();
        UseRelevance = UseRelevance && m_RelChecker != null;
    }

    public override void OnStartServer()
    {
        m_SyncPos = transform.position;
        m_SyncRot = transform.rotation;

        m_LastPos = m_SyncPos;
        m_LastRot = m_SyncRot;
    }

    public override void OnStartClient()
    {
        //m_SyncPos = transform.position;
        //m_SyncRot = transform.rotation;
        transform.position = m_SyncPos;
        transform.rotation = m_SyncRot;
    }

    private int m_SentCount = 0;
    private bool m_ForceSend;
    private void Update()
    {
        m_SendRateCounter += Time.deltaTime;
        if (m_SendRateCounter > SendRate)
        {
            if (m_SentCount++ > 50 + Random.Range(-5, 6))
            {
                m_ForceSend = true;
                m_SentCount = 0;
            }

            m_SendRateCounter = 0f;
            TransmitMotion();
        }
        
        LerpMotion();
    }

    private void TransmitMotion()
    {
        if (!isServer)
            return;

        var transmitPos = (transform.position - m_LastPos).sqrMagnitude > PosThreshold * PosThreshold;
        var transmitRot = Quaternion.Angle(transform.rotation, m_LastRot) > RotThreshold;

        if (m_ForceSend || (transmitPos && transmitRot))
        {
            m_LastPos = transform.position;
            m_LastRot = transform.rotation;

            if (!UseRelevance/* || (isServer && isClient)*/)
            {
                m_SyncPos = transform.position;
                m_SyncRot = transform.rotation;
            }
            else
            {
                BroadcastMotionToRelevantPlayers();
            }
        }
        else if (transmitPos)
        {
            m_LastPos = transform.position;
            if (!UseRelevance/* || (isServer && isClient)*/)
            {
                m_SyncPos = transform.position;
            }
            else
            {
                BroadcastPositionChangeToRelevantPlayers();
            }
        }
        else if (transmitRot)
        {
            m_LastRot = transform.rotation;
            if (!UseRelevance/* || (isServer && isClient)*/)
            {
                m_SyncRot = transform.rotation;
            }
            else
            {
                BroadcastRotationChangeToRelevantPlayers();
            }
        }
    }

    private void LerpMotion()
    {
        if (isServer)
            return;

        if (Vector3.Distance(transform.position, m_SyncPos) > SnapThreshold)
        {
            transform.position = m_SyncPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, m_SyncPos, Time.deltaTime * LerpRate);
        }

        var newRot = Quaternion.Lerp(transform.rotation, m_SyncRot, Time.deltaTime * LerpRate);
        if (!float.IsNaN(newRot.x))
            transform.rotation = newRot;
    }

    [Server]
    private void BroadcastMotionToRelevantPlayers()
    {
        if (m_RelChecker.RelevantTo.Count == 0)
            return;

        if (!CompressRotation)
        {
            var msg = new MotionMessage
            {
                Pos = m_LastPos,
                Rot = m_LastRot,
                TargetNetID = netId
            };

            foreach (var nc in m_RelChecker.RelevantTo)
            {
                nc.Send(MessageType.Motion, msg);
            }
        }
        else
        {
            var msg = new CompressedMotionMessage
            {
                Pos = m_LastPos,
                RotX = (short)m_LastRot.eulerAngles.x,
                RotY = (short)m_LastRot.eulerAngles.y,
                RotZ = (short)m_LastRot.eulerAngles.z,
                TargetNetID = netId
            };

            foreach (var nc in m_RelChecker.RelevantTo)
            {
                nc.Send(MessageType.CompressedMotion, msg);
            }
        }
    }

    [Server]
    private void BroadcastPositionChangeToRelevantPlayers()
    {
        if (m_RelChecker.RelevantTo.Count == 0)
            return;

        var msg = new PositionChangeMessage
        {
            Pos = m_LastPos,
            TargetNetID = netId
        };
        foreach (var nc in m_RelChecker.RelevantTo)
        {
            nc.Send(MessageType.PositionChange, msg);
        }
    }

    [Server]
    private void BroadcastRotationChangeToRelevantPlayers()
    {
        if (m_RelChecker.RelevantTo.Count == 0)
            return;

        if (!CompressRotation)
        {
            var msg = new RotationChangeMessage
            {
                Rot = m_LastRot,
                TargetNetID = netId
            };
            foreach (var nc in m_RelChecker.RelevantTo)
            {
                nc.Send(MessageType.RotationChange, msg);
            }
        }
        else
        {
            var msg = new CompressedRotationChangeMessage
            {
                X = (short)m_LastRot.eulerAngles.x,
                Y = (short)m_LastRot.eulerAngles.y,
                Z = (short)m_LastRot.eulerAngles.z,
                TargetNetID = netId
            };
            foreach (var nc in m_RelChecker.RelevantTo)
            {
                nc.Send(MessageType.CompressedRotationChange, msg);
            }
        }
    }

    public void ReceiveMotionMessage(MotionMessage msg)
    {
        m_SyncPos = msg.Pos;
        m_SyncRot = msg.Rot;
    }

    public void ReceiveCompressedMotionMessage(CompressedMotionMessage msg)
    {
        m_SyncPos = msg.Pos;
        m_SyncRot = Quaternion.identity;
        m_SyncRot.eulerAngles = new Vector3(msg.RotX, msg.RotY, msg.RotZ);
    }

    public void ReceivePositionChangeMessage(PositionChangeMessage msg)
    {
        m_SyncPos = msg.Pos;
    }

    public void ReceiveRotationChangeMessage(RotationChangeMessage msg)
    {
        m_SyncRot = msg.Rot;
    }

    public void ReceiveRotationChangeMessage(CompressedRotationChangeMessage msg)
    {
        m_SyncRot = Quaternion.identity;
        m_SyncRot.eulerAngles = new Vector3(msg.X, msg.Y, msg.Z);
    }

    //Somehow, this is fixing a NetworkReader.ReadByte out of range error.
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        base.OnDeserialize(reader, initialState);
    }
}

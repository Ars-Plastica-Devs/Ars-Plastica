using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

public class PointingSwarm : CubeBehaviour
{
    private enum State
    {
        MovingToOffset,
        MovingToOrigin,
        Pointing,
        Idle
    }

    //We use NetworkIdentity here because we can sync those over the network
    private IProximitySensor<NetworkIdentity> m_PlayerSensor;

    [SyncVar]
    private Vector3 m_IdleLookAtPos;
    private Transform m_Target;
    [SyncVar]
    private State m_State = State.Idle;
    [SyncVar]
    private bool m_Active;
    private float m_InteractRange; //Distance is only checked on the server, no need to sync

    [SyncVar]
    public float Speed;
    [SyncVar]
    public Vector3 OffsetTarget;
    [SyncVar]
    public float DistanceFromTarget;
    public Transform OriginTransform;
    public Transform ControlledTransform;

    [SyncVar]
    private NetworkInstanceId m_ParentNetID;

    public override NetworkInstanceId ParentNetID
    {
        get { return m_ParentNetID; }
        set { m_ParentNetID = value; }
    }

    private void Start()
    {
        if (isServer)
            m_PlayerSensor = new OctreeSensor<NetworkIdentity>(OriginTransform, m_InteractRange, OctreeManager.Get(OctreeType.Player))
            {
                RefreshRate = .1f.Randomize(.03f)
            };
    }

    public void SetActive(bool active)
    {
        m_Active = active;

        //Activate is handled through sensors
        if (!m_Active)
            Deactivate();
    }

    public void SetInteractRange(float r)
    {
        m_InteractRange = r;
        m_PlayerSensor.Range = m_InteractRange;
    }

    public void SetIdleLootAtTarget(Vector3 pos)
    {
        m_IdleLookAtPos = pos;
        ControlledTransform.LookAt(m_IdleLookAtPos);
    }

    private void Update()
    {
        if (!isServer || !m_Active)
            return;

        m_PlayerSensor.SensorUpdate();
        CheckSensor(m_PlayerSensor.Closest);
    }

    private void CheckSensor(NetworkIdentity target)
    {
        if (target == null && m_Target != null)
        {
            Deactivate();
        }
        else if (m_Target == null && target != null)
        {
            Activate(target);
        }
        else if (m_Target != null && target != null && m_Target != target.transform)
        {
            SwitchTarget(target);
        }
    }

    private void Activate(NetworkIdentity target)
    {
        m_Target = target.transform;
        m_State = State.MovingToOffset;
        RpcSetTarget(target);
    }

    private void Deactivate()
    {
        m_Target = null;
        if (m_State != State.Idle)
            m_State = State.MovingToOrigin;
        else
            ControlledTransform.LookAt(m_IdleLookAtPos);
        RpcSetTarget(null);
    }

    private void SwitchTarget(NetworkIdentity target)
    {
        Debug.Assert(m_Target != target);
        m_Target = target.transform;
        m_State = State.MovingToOffset;
        RpcSetTarget(target);
    }

    [ClientRpc]
    private void RpcSetTarget(NetworkIdentity t)
    {
        m_Target = t == null ? null : t.transform;

        if (m_Target == null && m_State != State.Idle)
        {
            m_State = State.MovingToOrigin;
        }
    }

    private void FixedUpdate()
    {
        switch (m_State)
        {
            case State.Idle:
                //if (!m_Active)
                //break;
                ControlledTransform.LookAt(m_IdleLookAtPos);
                ControlledTransform.position = OriginTransform.position;
                break;
            case State.Pointing:
                DoPointing();
                break;
            case State.MovingToOffset:
                DoMoveToOffset();
                break;
            case State.MovingToOrigin:
                DoMoveToOrigin();
                break;
        }
    }

    private void DoPointing()
    {
        if (m_Target == null)
            return;

        MoveTo((OffsetTarget * DistanceFromTarget) + m_Target.position, false);
        ControlledTransform.LookAt(m_Target);
    }

    private void DoMoveToOffset()
    {
        if (MoveTo((OffsetTarget * DistanceFromTarget) + m_Target.position))
        {
            m_State = State.Pointing;
        }
    }

    private void DoMoveToOrigin()
    {
        if (MoveTo(OriginTransform.position))
        {
            m_State = State.Idle;
            ControlledTransform.LookAt(m_IdleLookAtPos);
            ControlledTransform.position = OriginTransform.position;
        }
    }

    private bool MoveTo(Vector3 target, bool rotate = true)
    {
        var vel = (target - ControlledTransform.position).normalized;
        var atTarget = vel == Vector3.zero;

        if (!atTarget)
            ControlledTransform.position = Vector3.MoveTowards(ControlledTransform.position, target, Speed * Time.fixedDeltaTime);
        else
            vel = ControlledTransform.forward;

        if (rotate)
            //ControlledTransform.localRotation = Quaternion.RotateTowards(ControlledTransform.localRotation, Quaternion.LookRotation(vel, ControlledTransform.up), 20f);
            ControlledTransform.LookAt(ControlledTransform.position + vel);

        return atTarget;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public class PointingSwarmManager : PrimitiveWallManager
{
    private IProximitySensor<Transform> m_PlayerSensor;
    private Transform m_CurrentTarget;

    public float InteractRange; //Range that each primitive interacts at
    public float DistanceFromTarget;
    public float Speed;
    public SyncedSphereCollider InteractableCollider;

    protected override void Start()
    {
        if (InteractableCollider == null)
            Debug.LogError("InteractableCollider is null on PointingSwarmManager", this);

        base.Start();

        StartCoroutine(InitializePlayerSensor());
        /*if (isServer)
            m_PlayerSensor = new OctreeSensor<Transform>(transform, CalculateEnclosingRadius(), OctreeManager.Get(OctreeType.Player))
            {
                RefreshRate = .1f.Randomize(.05f)
            };*/
    }

    private IEnumerator InitializePlayerSensor()
    {
        while (!OctreeManager.Contains(OctreeType.Player))
        {
            yield return new WaitForSeconds(.5f);
        }

        m_PlayerSensor = new OctreeSensor<Transform>(transform, CalculateEnclosingRadius(), OctreeManager.Get(OctreeType.Player))
        {
            RefreshRate = .1f.Randomize(.05f)
        };
    }

    private void Update()
    {
        if (!isServer)
            return;

        if (m_PlayerSensor == null)
            return;

        m_PlayerSensor.SensorUpdate();
        SwarmUpdate(m_PlayerSensor.Closest);
    }

    private void SwarmUpdate(Transform target)
    {
        if (target == null && m_CurrentTarget != null)
            DeactivatePrimitives();
        else if (m_CurrentTarget == null && target != null)
            ActivatePrimitives();

        m_CurrentTarget = target;
    }

    private void ActivatePrimitives()
    {
        var points = FibonacciSphere.Sample((uint)Primitives.Count);

        for (var i = 0; i < Primitives.Count; i++)
        {
            var ps = Primitives[i].GetComponent<PointingSwarm>();
            ps.SetInteractRange(InteractRange);
            ps.OffsetTarget = points[i];
            ps.DistanceFromTarget = DistanceFromTarget;
            ps.SetActive(true);
        }
    }

    private void DeactivatePrimitives()
    {
        for (var i = 0; i < Primitives.Count; i++)
        {
            Primitives[i].GetComponent<PointingSwarm>().SetActive(false);
        }
    }

    protected override void PrePrimitiveSpawned(GameObject obj)
    {
        obj.GetComponent<PointingSwarm>().Speed = Speed;
        obj.GetComponent<PointingSwarm>().ParentNetID = netId;
    }

    protected override void PostPrimitiveSpawned(GameObject obj)
    {

    }

    public override Dictionary<string, Func<string>> GetCurrentData()
    {
        return new Dictionary<string, Func<string>>
        {
            { "DistanceBetweenPrimitives", () => DistanceBetweenPrimitives.ToString() },
            { "Width", () => Width.ToString() },
            { "Height", () => Height.ToString() },
            { "PrimitiveScale", () => PrimitiveScale.ToString() },
            { "InteractRange", () => InteractRange.ToString() },
            { "DistanceFromTarget", () => DistanceFromTarget.ToString() },
            { "Speed", () => Speed.ToString() }
        };
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        switch (tokens[1])
        {
            case "InteractRange":
                var range = float.Parse(tokens[2]);
                SetInteractRange(range);
                return tokens[0] + " InteractRange changed to " + InteractRange;
            case "DistanceFromTarget":
                var dist = float.Parse(tokens[2]);
                SetDistanceFromTarget(dist);
                return tokens[0] + " DistanceFromTarget changed to " + DistanceFromTarget;
            case "Speed":
                var speed = float.Parse(tokens[2]);
                SetSpeed(speed);
                return tokens[0] + " Speed changed to " + Speed;
            default:
                return base.RunCommand(cmd, sender);
        }
    }

    private float DistanceToCorner()
    {
        return
            (new Vector3(Width * DistanceBetweenPrimitives / 2f, Height * DistanceBetweenPrimitives / 2f, 0f)).magnitude;
    }

    private float CalculateEnclosingRadius()
    {
        return DistanceToCorner() + InteractRange;
    }

    private void UpdateInteractRange()
    {
        InteractableCollider.Radius = DistanceToCorner();
    }

    public override float GetBoundingSphereRadius()
    {
        return DistanceToCorner() + InteractRange;
    }

    protected override void SpawnPrimitives()
    {
        base.SpawnPrimitives(); //This will place them at the wrong location
        RepositionPrimitives(); //Fix there positions - see below
        UpdateInteractRange();
    }

    protected override void RepositionPrimitives()
    {
        var points = FibonacciSphere.Sample((uint)Primitives.Count);

        for (var i = 0; i < Primitives.Count; i++)
        {
            var ps = Primitives[i].GetComponent<PointingSwarm>();
            ps.transform.position = transform.position + points[i] * DistanceFromTarget;
            ps.transform.LookAt(transform.position);
            ps.GetComponent<PointingSwarm>().SetIdleLootAtTarget(transform.position);
            RpcSetLocation(ps.GetComponent<NetworkIdentity>(), ps.transform.position, transform.position);
        }

        UpdateInteractRange();
    }

    [ClientRpc]
    private void RpcSetLocation(NetworkIdentity id, Vector3 pos, Vector3 lookAtPos)
    {
        id.transform.position = pos;
        id.GetComponent<PointingSwarm>().SetIdleLootAtTarget(lookAtPos);
    }

    [Server]
    private void SetInteractRange(float range)
    {
        InteractRange = range;
        foreach (var prim in Primitives)
        {
            prim.GetComponent<PointingSwarm>().SetInteractRange(range);
        }

        m_PlayerSensor.Range = CalculateEnclosingRadius() * 10f;

        RpcSetInteractRange(InteractRange);

        UpdateInteractRange();
    }

    [ClientRpc]
    private void RpcSetInteractRange(float interactRange)
    {
        InteractRange = interactRange;
        UpdateInteractRange();
    }

    [Server]
    private void SetDistanceFromTarget(float dist)
    {
        DistanceFromTarget = dist;
        foreach (var prim in Primitives)
        {
            prim.GetComponent<PointingSwarm>().DistanceFromTarget = DistanceFromTarget;
        }
        RpcSetDistanceFromTarget(DistanceFromTarget);
    }

    [ClientRpc]
    private void RpcSetDistanceFromTarget(float distanceFromTarget)
    {
        DistanceFromTarget = distanceFromTarget;
    }

    [Server]
    private void SetSpeed(float speed)
    {
        Speed = speed;
        foreach (var prim in Primitives)
        {
            prim.GetComponent<PointingSwarm>().Speed = Speed;
        }
        RpcSetSpeed(Speed);
    }

    [ClientRpc]
    private void RpcSetSpeed(float speed)
    {
        Speed = speed;
    }
}

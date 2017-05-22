using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public class AvoidOnAxisCubeManager : CubeManager
{
    [SyncVar]
    [SerializeField]
    private float m_CubeSize;

    public override float CubeSize
    {
        get { return m_CubeSize; }
        set { m_CubeSize = value; }
    }

    public AvoidOnAxis.AvoidParameters AvoidParams = new AvoidOnAxis.AvoidParameters(6f, 6f, 18f, 10f, .4f);

    public SyncedSphereCollider InteractableCollider;

    protected override void Start()
    {
        if (InteractableCollider == null)
            Debug.LogError("InteractableCollider is null on AvoidOnAxisCubeManager", this);
        base.Start();
    }

    protected override void SpawnCubes()
    {
        base.SpawnCubes();

        CalculateColliderAcitvationRange();
    }

    protected override void PreCubeSpawned(GameObject cube)
    {
        cube.GetComponent<AvoidOnAxis>().Initialize();
        cube.GetComponent<AvoidOnAxis>().AvoidParams = AvoidParams;
        cube.GetComponent<AvoidOnAxis>().SetInteractionRadius(CubeInteractionRadius);
        cube.GetComponent<AvoidOnAxis>().ParentNetID = netId;
    }

    protected override void BaseActivateBehaviour()
    {
        ActivateBehaviour();
        RpcActivateBehaviour();
    }

    protected override void BaseDeactivateBehaviour()
    {
        DeactivateBehaviour();
        RpcDeactivateBehaviour();
    }

    [ClientRpc]
    protected void RpcActivateBehaviour()
    {
        ActivateBehaviour();
    }

    [ClientRpc]
    protected void RpcDeactivateBehaviour()
    {
        DeactivateBehaviour();
    }

    protected void ActivateBehaviour()
    {
        /*foreach (var cube in Cubes)
        {
            cube.GetComponent<SphereCollider>().enabled = true;
        }*/
    }

    protected void DeactivateBehaviour()
    { 
        /*foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().DeactivateBehaviour();
            cube.GetComponent<SphereCollider>().enabled = false;
        }*/
    }

    protected override void RepositionCubes()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().Reset();
        }

        base.RepositionCubes();

        CalculateColliderAcitvationRange();
    }

    public override Dictionary<string, Func<string>> GetCurrentData()
    {
        return new Dictionary<string, Func<string>>
        {
            { "CubeInteractRadius", () => CubeInteractionRadius.ToString() },
            { "GapFactor", () => GapFactor.ToString() },
            { "SideLength", () => SideLength.ToString() },
            { "Color", () => Color.r + " " + Color.g + " " + Color.b },
            { "CubeSize", () => CubeSize.ToString() },
            { "DistanceToTravel", () => AvoidParams.DistanceToTravel.ToString() },
            { "DistanceVariation", () => AvoidParams.DistanceVariation.ToString() },
            { "Speed", () => AvoidParams.Speed.ToString() },
            { "SpeedVariation", () => AvoidParams.SpeedVariation.ToString() }
        };
    }

    private void CalculateColliderAcitvationRange()
    {
        var colliderActivationRange = GetBoundingSphereRadius();

        var sc = GetComponent<SphereCollider>();
        sc.radius = colliderActivationRange + CubeInteractionRadius;
        InteractableCollider.Radius = colliderActivationRange;

        var colliders = Physics.OverlapSphere(transform.position, colliderActivationRange + CubeInteractionRadius).ToList();

        //Remove colliders that don't trigger us.
        for (var i = 0; i < colliders.Count; i++)
        {
            if (TriggeringTags.Contains(colliders[i].tag)) continue;

            colliders.RemoveAt(i);
            i--;
        }

        TriggeredCount = colliders.Count;
        EvaluateTriggeredCount();
    }

    [Server]
    protected override void SetCubeInteractionRadius(float r)
    {
        if (CubeInteractionRadius == r)
            return;

        CubeInteractionRadius = r;
        RpcSetCubeInteractionRadius(CubeInteractionRadius);
        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().SetInteractionRadius(CubeInteractionRadius);
        }
        CalculateColliderAcitvationRange();
    }

    [ClientRpc]
    private void RpcSetCubeInteractionRadius(float r)
    {
        CubeInteractionRadius = r;
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        try
        {
            switch (tokens[1])
            {
                case "DistanceToTravel":
                    SetDistanceToTravel(float.Parse(tokens[2]));
                    return tokens[0] + " DistanceToTravel changed to " + AvoidParams.DistanceToTravel;
                case "DistanceVariation":
                    SetDistanceVariation(float.Parse(tokens[2]));
                    return tokens[0] + " DistanceVariation changed to " + AvoidParams.DistanceVariation;
                case "Speed":
                    var newSpeed = float.Parse(tokens[2]);
                    if (newSpeed < 0)
                    {
                        return "Cannot changed Speed to " + newSpeed;
                    }
                    SetSpeed(newSpeed);
                    return tokens[0] + " Speed changed to " + AvoidParams.Speed;
                case "SpeedVariation":
                    SetSpeedVariation(float.Parse(tokens[2]));
                    return tokens[0] + " SpeedVariation changed to " + AvoidParams.SpeedVariation;
                default:
                    return base.RunCommand(cmd, sender);
            }
        }
        catch
        {
            return "Not a valid command";
        }
    }

    [Server]
    private void SetSpeedVariation(float newVal)
    {
        if (AvoidParams.SpeedVariation == newVal)
            return;

        AvoidParams.SpeedVariation = newVal;

        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.SpeedVariation = AvoidParams.SpeedVariation;
        }
    }

    [Server]
    private void SetSpeed(float newVal)
    {
        if (AvoidParams.Speed == newVal)
            return;

        AvoidParams.Speed = newVal;

        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.Speed = AvoidParams.Speed;
        }
    }
    
    [Server]
    private void SetDistanceVariation(float newVal)
    {
        if (AvoidParams.DistanceVariation == newVal)
            return;

        AvoidParams.DistanceVariation = newVal;

        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.DistanceVariation = AvoidParams.DistanceVariation;
        }
    }

    [Server]
    private void SetDistanceToTravel(float newVal)
    {
        if (AvoidParams.DistanceToTravel == newVal)
            return;

        AvoidParams.DistanceToTravel = newVal;

        foreach (var cube in Cubes)
        {
            cube.GetComponent<AvoidOnAxis>().AvoidParams.DistanceToTravel = AvoidParams.DistanceToTravel;
        }
    }

    //Somehow, this is fixing a NetworkReader.ReadByte out of range error.
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        base.OnDeserialize(reader, initialState);
    }
}

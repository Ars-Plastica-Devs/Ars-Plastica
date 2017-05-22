using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Octree;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public class ShrinkOnProximityCubeManager : CubeManager
{
    [SyncVar]
    [SerializeField]
    private float m_CubeSize;

    public override float CubeSize
    {
        get { return m_CubeSize; }
        set { m_CubeSize = value; }
    }

    public SyncedSphereCollider InteractableCollider;

    protected override void Start()
    {
        if (InteractableCollider == null)
            Debug.LogError("InteractableCollider is null on ShrinkOnProximityCubeManager", this);

        base.Start();

        CalculateColliderAcitvationRange();

        if (isServer)
            SetCubeInteractionRadius(CubeInteractionRadius);
    }

    protected override void PreCubeSpawned(GameObject cube)
    {
        cube.GetComponent<ShrinkOnProximity>().ParentNetID = netId;
    }

    protected override void PostCubeSpawned(GameObject cube)
    {
        cube.GetComponent<ShrinkOnProximity>().Initialize(CubeInteractionRadius, CubeSize);
        RpcOnCubeSpawned(cube);
    }

    [ClientRpc]
    private void RpcOnCubeSpawned(GameObject cube)
    {
        if (cube == null)
            return;

        Cubes.Add(cube);
    }

    protected override void BaseActivateBehaviour()
    {
        ActivateBehaviour();
    }

    protected override void BaseDeactivateBehaviour()
    {
        DeactivateBehaviour();
    }

    protected void ActivateBehaviour()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<ShrinkOnProximity>().Initialize(CubeInteractionRadius, CubeSize);
        }
    }

    protected void DeactivateBehaviour()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<ShrinkOnProximity>().enabled = false;
        }
    }

    protected override void RepositionCubes()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<ShrinkOnProximity>().Reset();
        }

        base.RepositionCubes();

        CalculateColliderAcitvationRange();
    }

    [Server]
    protected override void SetCubeSize(float newSize)
    {
        base.SetCubeSize(newSize);

        RpcSetShrinkCubeSize(newSize);
    }

    [ClientRpc]
    private void RpcSetShrinkCubeSize(float newSize)
    {
        foreach (var cube in Cubes.Where(c => c != null))
        {
            cube.GetComponent<ShrinkOnProximity>().SetInitialScale(newSize);
        }
    }

    public override Dictionary<string, Func<string>> GetCurrentData()
    {
        return new Dictionary<string, Func<string>>
        {
            { "CubeInteractRadius", () => CubeInteractionRadius.ToString() },
            { "GapFactor", () => GapFactor.ToString() },
            { "SideLength", () => SideLength.ToString() },
            { "Color", () => Color.r + " " + Color.g + " " + Color.b },
            { "CubeSize", () => CubeSize.ToString() }
        };
    }

    private void CalculateColliderAcitvationRange()
    {
        var colliderActivationRange = GetBoundingSphereRadius();

        var sc = GetComponent<SphereCollider>();
        sc.radius = colliderActivationRange + CubeInteractionRadius;
        InteractableCollider.Radius = colliderActivationRange;

        if (OctreeManager.Get(OctreeType.Player) != null)
        {
            var colliders = OctreeManager.Get(OctreeType.Player).GetObjectsInRange(transform.position, colliderActivationRange + CubeInteractionRadius);

            TriggeredCount = colliders.Length;
            EvaluateTriggeredCount();
        }
    }

    [Server]
    protected override void SetCubeInteractionRadius(float r)
    {
        CubeInteractionRadius = r;
        RpcUpdateCubeInteractRadius(r);
        CalculateColliderAcitvationRange();
    }

    [ClientRpc]
    private void RpcUpdateCubeInteractRadius(float r)
    {
        CubeInteractionRadius = r;
        UpdateCubeInteractRadius();
        CalculateColliderAcitvationRange();
    }

    protected virtual void UpdateCubeInteractRadius()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<ShrinkOnProximity>().SetInteractionRadius(CubeInteractionRadius);
        }
    }
}

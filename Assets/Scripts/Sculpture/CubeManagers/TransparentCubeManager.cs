using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Octree;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public class TransparentCubeManager : CubeManager
{
    private IEnumerator m_StartCoroutine;
    private IEnumerator m_StopCoroutine;

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
            Debug.LogError("InteractableCollider is null on TransparentCubeManager", this);

        base.Start();

        CalculateColliderAcitvationRange();

        if (isServer)
            SetCubeInteractionRadius(CubeInteractionRadius);
    }

    protected override void PreCubeSpawned(GameObject cube)
    {
        cube.GetComponent<MakeTransparent>().ParentNetID = netId;
    }

    protected override void PostCubeSpawned(GameObject cube)
    {
        //cube.GetComponent<MakeTransparent>().Initialize(CubeInteractionRadius, CubeSize);
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
        if (m_StopCoroutine != null)
            StopCoroutine(m_StopCoroutine);

        m_StartCoroutine = ActivateOverTime(Cubes.Count / 4);
        StartCoroutine(m_StartCoroutine);
    }

    private IEnumerator ActivateOverTime(int perFrame)
    {
        var thisFrame = 0;
        for (var i = 0; i < Cubes.Count; i++, thisFrame++)
        {
            Cubes[i].GetComponent<MakeTransparent>().Initialize(CubeInteractionRadius, CubeSize);
            if (thisFrame >= perFrame)
            {
                thisFrame = 0;
                yield return 0;
            }
        }
    }

    protected void DeactivateBehaviour()
    {
        if (m_StartCoroutine != null)
            StopCoroutine(m_StartCoroutine);

        m_StopCoroutine = DeactivateOverTime(Cubes.Count / 60); //We spread this over 60 frames because this is surprisingly hard on performance
        StartCoroutine(m_StopCoroutine);
    }

    private IEnumerator DeactivateOverTime(int perFrame)
    {
        var thisFrame = 0;
        for (var i = 0; i < Cubes.Count; i++, thisFrame++)
        {
            Cubes[i].GetComponent<MakeTransparent>().enabled = false;
            if (thisFrame >= perFrame)
            {
                thisFrame = 0;
                yield return 0;
            }
        }
    }

    protected override void RepositionCubes()
    {
        foreach (var cube in Cubes)
        {
            cube.GetComponent<MakeTransparent>().Reset();
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
            { "CubeSize", () => CubeSize.ToString() }
        };
    }

    private void CalculateColliderAcitvationRange()
    {
        var boundingRadius = GetBoundingSphereRadius();

        GetComponent<SphereCollider>().radius = boundingRadius + CubeInteractionRadius;
        InteractableCollider.Radius = boundingRadius;

        if (OctreeManager.Get(OctreeType.Player) != null)
        {
            var colliders = OctreeManager.Get(OctreeType.Player).GetObjectsInRange(transform.position, boundingRadius + CubeInteractionRadius);

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
            cube.GetComponent<MakeTransparent>().SetInteractionRadius(CubeInteractionRadius);
        }
    }
}

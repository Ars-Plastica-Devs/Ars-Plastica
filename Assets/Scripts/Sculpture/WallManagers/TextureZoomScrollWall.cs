using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TextureZoomScrollWall : Sculpture
{
    private float m_OneScale;
    private float m_TwoScale;
    private float m_ThreeScale;

    [SerializeField] private bool m_CanScale = true;

    [SyncVar(hook="OnScaleChange")] private Vector3 m_Scale;

    [SyncVar] public float OneScaleSpeed;
    [SyncVar] public float TwoScaleSpeed;
    [SyncVar] public float ThreeScaleSpeed;

    [SyncVar] public float MinScale = .2f;
    [SyncVar] public float MaxScale = 1f;

    [SyncVar] public float OneSpeed;
    [SyncVar] public float TwoSpeed;
    [SyncVar] public float ThreeSpeed;

    public Renderer RenderTarget;

    private void Start()
    {
        if (RenderTarget == null)
            Debug.LogError("RenderTarget is null on TextureZoomScrollWall", this);
        //m_Scale = transform.localScale;
    }

    public override void OnStartServer()
    {
        m_Scale = transform.localScale;
    }

    public override void OnStartClient()
    {
        Debug.Assert(RenderTarget.materials.Length == 3);

        base.OnStartClient();

        transform.localScale = m_Scale;

        m_OneScale = RenderTarget.materials[0].GetTextureScale("_MainTex").x;
        m_TwoScale = RenderTarget.materials[1].GetTextureScale("_MainTex").x;
        m_ThreeScale = RenderTarget.materials[2].GetTextureScale("_MainTex").x;

        if (!isServer)
            return;
        //Hmmm... this stuff will never run in a Server-Client architecture, which is not intended.
        //But it everything still works so bleh

        InvokeRepeating("SyncOffsets", 2f, 1f);
        //InvokeRepeating("SyncScales", 2f, 1f);
    }

    private void Update()
    {
        MoveMaterialOffset(RenderTarget.materials[0], OneSpeed * Time.deltaTime);
        MoveMaterialOffset(RenderTarget.materials[1], TwoSpeed * Time.deltaTime);
        MoveMaterialOffset(RenderTarget.materials[2], ThreeSpeed * Time.deltaTime);

        AdjustTextureScales();
    }

    private void MoveMaterialOffset(Material m, float x)
    {
        var offset = m.GetTextureOffset("_MainTex");
        offset += new Vector2(x, 0);
        m.SetTextureOffset("_MainTex", offset);
    }

    private void SetMaterialOffset(Material m, float x)
    {
        var offset = m.GetTextureOffset("_MainTex");
        offset = new Vector2(x, 0);
        m.SetTextureOffset("_MainTex", offset);
    }

    private void AdjustTextureScales()
    {
        m_OneScale += OneScaleSpeed * Time.deltaTime;
        m_TwoScale += TwoScaleSpeed * Time.deltaTime;
        m_ThreeScale += ThreeScaleSpeed * Time.deltaTime;

        var one = Mathf.PingPong(m_OneScale, MaxScale - MinScale) + MinScale;
        var two = Mathf.PingPong(m_TwoScale, MaxScale - MinScale) + MinScale;
        var three = Mathf.PingPong(m_ThreeScale, MaxScale - MinScale) + MinScale;

        RenderTarget.materials[0].SetTextureScale("_MainTex", new Vector2(one, one));
        RenderTarget.materials[1].SetTextureScale("_MainTex", new Vector2(two, two));
        RenderTarget.materials[2].SetTextureScale("_MainTex", new Vector2(three, three));
    }

    private void SyncOffsets()
    {
        RpcSyncOffsets(RenderTarget.materials[0].mainTextureOffset.x,
                        RenderTarget.materials[1].mainTextureOffset.x,
                        RenderTarget.materials[2].mainTextureOffset.x);
    }

    [ClientRpc]
    private void RpcSyncOffsets(float one, float two, float three)
    {
        SetMaterialOffset(RenderTarget.materials[0], one);
        SetMaterialOffset(RenderTarget.materials[1], two);
        SetMaterialOffset(RenderTarget.materials[2], three);
    }

    private void SyncScales()
    {
        RpcSyncScales(RenderTarget.materials[0].GetTextureScale("_MainTex").x,
            RenderTarget.materials[1].GetTextureScale("_MainTex").x,
            RenderTarget.materials[2].GetTextureScale("_MainTex").x);
    }

    [ClientRpc]
    private void RpcSyncScales(float one, float two, float three)
    {
        RenderTarget.materials[0].SetTextureScale("_MainTex", new Vector2(one, one));
        RenderTarget.materials[1].SetTextureScale("_MainTex", new Vector2(two, two));
        RenderTarget.materials[2].SetTextureScale("_MainTex", new Vector2(three, three));
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        var tokens = cmd.Split(' ');

        switch (tokens[1])
        {
            case "OneSpeed":
                OneSpeed = float.Parse(tokens[2]);
                return "Set OneSpeed to " + OneSpeed;
            case "TwoSpeed":
                TwoSpeed = float.Parse(tokens[2]);
                return "Set TwoSpeed to " + TwoSpeed;
            case "ThreeSpeed":
                ThreeSpeed = float.Parse(tokens[2]);
                return "Set ThreeSpeed to " + ThreeSpeed;
            case "OneScaleSpeed":
                OneScaleSpeed = float.Parse(tokens[2]);
                return "Set OneScaleSpeed to " + OneScaleSpeed;
            case "TwoScaleSpeed":
                TwoScaleSpeed = float.Parse(tokens[2]);
                return "Set TwoScaleSpeed to " + TwoScaleSpeed;
            case "ThreeScaleSpeed":
                ThreeScaleSpeed = float.Parse(tokens[2]);
                return "Set ThreeScaleSpeed to " + ThreeScaleSpeed;
            case "XScale":
                if (!m_CanScale)
                    return "Scaling is not permitted on this object";
                var x = float.Parse(tokens[2]);
                SetScale(new Vector3(x, transform.localScale.y, transform.localScale.z));
                return "Set XScale to " + x;
            case "YScale":
                if (!m_CanScale)
                    return "Scaling is not permitted on this object";
                var y = float.Parse(tokens[2]);
                SetScale(new Vector3(transform.localScale.x, transform.localScale.y, y));//Yes, y goes to the z axis. The base wall is rotated so that z is down
                return "Set YScale to " + y;
            case "MinScale":
                MinScale = float.Parse(tokens[2]);
                return "Set MinScale to " + MinScale;
            case "MaxScale":
                MaxScale = float.Parse(tokens[2]);
                return "Set MaxScale to " + MaxScale;
            default:
                return base.RunCommand(cmd, sender);
        }
    }

    private void SetScale(Vector3 scale)
    {
        transform.localScale = scale;
        m_Scale = scale;
    }

    private void OnScaleChange(Vector3 scale)
    {
        m_Scale = scale;
        transform.localScale = m_Scale;
    }

    public override Dictionary<string, Func<string>> GetCurrentData()
    {
        return new Dictionary<string, Func<string>>
        {
            { "OneSpeed", () => OneSpeed.ToString() },
            { "TwoSpeed", () => TwoSpeed.ToString() },
            { "ThreeSpeed", () => ThreeSpeed.ToString() },
            { "OneScaleSpeed", () => OneScaleSpeed.ToString() },
            { "TwoScaleSpeed", () => TwoScaleSpeed.ToString() },
            { "ThreeScaleSpeed", () => ThreeScaleSpeed.ToString() },
            { "XScale", () => transform.localScale.x.ToString() },
            { "YScale", () => transform.localScale.z.ToString() },
            { "MinScale", () => MinScale.ToString() },
            { "MaxScale", () => MaxScale.ToString() }
        };
    }

    public override float GetBoundingSphereRadius()
    {
        var b = RenderTarget.bounds;
        return Mathf.Max(b.size.x, b.size.y, b.size.z);
    }
}
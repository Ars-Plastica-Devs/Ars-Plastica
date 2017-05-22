using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TextureZoomWall : Sculpture
{
    private float m_OneScale;
    private float m_TwoScale;
    private float m_ThreeScale;

    [SyncVar(hook = "OnScaleChange")] private Vector3 m_Scale;

    [SyncVar] public float OneScaleSpeed;
    [SyncVar] public float TwoScaleSpeed;
    [SyncVar] public float ThreeScaleSpeed;

    [SyncVar] public float MinScale = .2f;
    [SyncVar] public float MaxScale = 1;
    
    public Renderer RenderTarget;

    private void Start()
    {
        if (RenderTarget == null)
            Debug.LogError("RenderTarget is null on TextureZoomWall", this);

        m_Scale = transform.localScale;
    }

    public override void OnStartClient()
    {
        Debug.Assert(RenderTarget.materials.Length == 3);

        base.OnStartClient();

        transform.localScale = m_Scale;

        if (!isServer)
            return;

        InvokeRepeating("SyncScales", 2f, 1f);

        m_OneScale = RenderTarget.materials[0].GetTextureScale("_MainTex").x;
        m_TwoScale = RenderTarget.materials[1].GetTextureScale("_MainTex").x;
        m_ThreeScale = RenderTarget.materials[2].GetTextureScale("_MainTex").x;
    }

    private void Update()
    {
        AdjustTextureScales();
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
                var x = float.Parse(tokens[2]);
                SetScale(new Vector3(x, transform.localScale.y, transform.localScale.z));
                return "Set XScale to " + x;
            case "YScale":
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

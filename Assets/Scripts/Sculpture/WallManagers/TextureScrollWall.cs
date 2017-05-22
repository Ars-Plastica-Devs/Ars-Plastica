using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TextureScrollWall : Sculpture
{
    [SyncVar(hook="OnScaleChange")] private Vector3 m_Scale;
    [SyncVar] public float OneSpeed;
    [SyncVar] public float TwoSpeed;
    [SyncVar] public float ThreeSpeed;

    public Renderer RenderTarget;

    private void Start()
    {
        if (RenderTarget == null)
            Debug.LogError("RenderTarget is null on TextureScrollWall", this);

        m_Scale = transform.localScale;
    }

    public override void OnStartClient()
    {
        Debug.Assert(RenderTarget.materials.Length == 3);

        base.OnStartClient();

        transform.localScale = m_Scale;

        if (!isServer)
            return;

        InvokeRepeating("SyncOffsets", 2f, 1f);
    }

    private void Update()
    {
        MoveMaterialOffset(RenderTarget.materials[0], OneSpeed * Time.deltaTime);
        MoveMaterialOffset(RenderTarget.materials[1], TwoSpeed * Time.deltaTime);
        MoveMaterialOffset(RenderTarget.materials[2], ThreeSpeed * Time.deltaTime);
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
            case "XScale":
                var x = float.Parse(tokens[2]);
                SetScale(new Vector3(x, transform.localScale.y, transform.localScale.z));
                return "Set XScale to " + x;
            case "YScale":
                var y = float.Parse(tokens[2]);
                SetScale(new Vector3(transform.localScale.x, transform.localScale.y, y));//Yes, y goes to the z axis. The base wall is rotated so that z is down
                return "Set YScale to " + y;
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
            { "XScale", () => transform.localScale.x.ToString() },
            { "YScale", () => transform.localScale.z.ToString() }
        };
    }

    public override float GetBoundingSphereRadius()
    {
        var b = RenderTarget.bounds;
        return Mathf.Max(b.size.x, b.size.y, b.size.z);
    }
}
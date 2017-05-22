using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public class PixelBoardCubeWallManager : CubeWallManager
{
    [SyncVar]
    [SerializeField]
    private float m_CubeSize;

    public override float CubeSize
    {
        get { return m_CubeSize; }
        set { m_CubeSize = value; }
    }

    public Color PositiveColor;
    public Color NegativeColor;

    public SyncedBoxCollider InteractableCollider;

    protected override void Start()
    {
        if (InteractableCollider == null)
            Debug.LogError("InteractableCollider is null on PixelBoardCubeWallManager", this);

        base.Start();

        CalculateEnclosingCollider();
    }

    protected override void PreCubeSpawned(GameObject cube, int x, int y)
    {
        cube.GetComponent<ColorToggle>().PositiveColor = PositiveColor;
        cube.GetComponent<ColorToggle>().NegativeColor = NegativeColor;
        cube.GetComponent<ColorToggle>().ParentNetID = netId;
    }

    protected override void PostCubeSpawned(GameObject cube)
    {
        cube.GetComponent<ColorToggle>().Initialize();
    }

    protected override void RepositionCubes()
    {
        base.RepositionCubes();

        CalculateEnclosingCollider();
    }

    public override Dictionary<string, Func<string>> GetCurrentData()
    {
        return new Dictionary<string, Func<string>>
        {
            { "GapFactor", () => GapFactor.ToString() },
            { "SideLength", () => SideLength.ToString() },
            { "PositiveColor", () => PositiveColor.r + " " + PositiveColor.g + " " + PositiveColor.b },
            { "NegativeColor", () => NegativeColor.r + " " + NegativeColor.g + " " + NegativeColor.b },
            { "CubeSize", () => CubeSize.ToString() }
        };
    }

    private void CalculateEnclosingCollider()
    {
        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = new Vector3(CubeSize * size.x, CubeSize * size.y, CubeSize * size.z);
        var boundingSize = cubeSize * (SideLength) * GapFactor;
        boundingSize = new Vector3(boundingSize.x, boundingSize.y, cubeSize.z);

        var bc = GetComponent<BoxCollider>();
        bc.size = boundingSize;
        InteractableCollider.Size = boundingSize;
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        try
        {
            switch (tokens[1])
            {
                case "PositiveColor":
                    var rp = Mathf.Clamp(float.Parse(tokens[2]), 0f, 1f);
                    var gp = Mathf.Clamp(float.Parse(tokens[3]), 0f, 1f);
                    var bp = Mathf.Clamp(float.Parse(tokens[4]), 0f, 1f);

                    SetPositiveColor(rp, gp, bp);
                    return tokens[0] + " Positive Color changed to " + "{" + rp + ", " + gp + ", " + bp + "}";
                case "NegativeColor":
                    var r = Mathf.Clamp(float.Parse(tokens[2]), 0f, 1f);
                    var g = Mathf.Clamp(float.Parse(tokens[3]), 0f, 1f);
                    var b = Mathf.Clamp(float.Parse(tokens[4]), 0f, 1f);

                    SetNegativeColor(r, g, b);
                    return tokens[0] + " Negative Color changed to " + "{" + r + ", " + g + ", " + b + "}";
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
    private void SetPositiveColor(float r, float g, float b)
    {
        PositiveColor = new Color(r, g, b);
        RpcSetPositiveColor(r, g, b);
        foreach (var cube in Cubes)
        {
            cube.GetComponent<ColorToggle>().PositiveColor = PositiveColor;
        }
    }

    [ClientRpc]
    private void RpcSetPositiveColor(float r, float g, float b)
    {
        PositiveColor = new Color(r, g, b);
    }

    [Server]
    private void SetNegativeColor(float r, float g, float b)
    {
        NegativeColor = new Color(r, g, b);
        RpcSetNegativeColor(r, g, b);
        foreach (var cube in Cubes)
        {
            cube.GetComponent<ColorToggle>().NegativeColor = NegativeColor;
        }
    }

    [ClientRpc]
    private void RpcSetNegativeColor(float r, float g, float b)
    {
        NegativeColor = new Color(r, g, b);
    }
}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public abstract class CubeManager : BaseCubeManager
{
    private const uint SIDE_LENGTH_MAX = 30;
    protected int TriggeredCount;

    public Color Color = Color.white;
    public float GapFactor = 1f;
    public float CubeInteractionRadius = 20f;

    protected override void SpawnCubes()
    {
        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = size * CubeSize;

        //we want the cube of cubes to be centered on this game object
        var centerOffset = cubeSize * (SideLength / 2f) * GapFactor;
        var cubeOffset = (cubeSize * GapFactor / 2f);
        var finalOffset = -centerOffset + cubeOffset;

        for (var k = 0; k < SideLength; k++)
        {
            for (var j = 0; j < SideLength; j++)
            {
                for (var i = 0; i < SideLength; i++)
                {
                    var pos = (new Vector3(cubeSize.x * i, cubeSize.y * j, cubeSize.z * k) * GapFactor) + finalOffset;
                    var cube = (GameObject)Instantiate(CubePrefab, pos, Quaternion.identity);
                    cube.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);
                    cube.transform.SetParent(transform, false);

                    PreCubeSpawned(cube);

                    NetworkServer.Spawn(cube);
                    
                    Cubes.Add(cube);

                    //RpcSetAsChild(cube);

                    PostCubeSpawned(cube);
                }
            }
        }

        SetColor(Color.r, Color.g, Color.b);
    }

    public override float GetBoundingSphereRadius()
    {
        var cubeLength = CubePrefab.GetComponent<Renderer>().bounds.size.x * GapFactor;
        return cubeLength * SideLength * Mathf.Sqrt(3f) * .5f;
    }

    protected override void RepositionCubes()
    {
        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = new Vector3(CubeSize * size.x, CubeSize * size.y, CubeSize * size.z);

        //we want the cube of cubes to be centered on this game object
        var centerOffset = cubeSize * (SideLength / 2f) * GapFactor;
        var cubeOffset = (cubeSize * GapFactor / 2f);
        var finalOffset = /*transform.position */-centerOffset + cubeOffset;

        for (var k = 0; k < SideLength; k++)
        {
            for (var j = 0; j < SideLength; j++)
            {
                for (var i = 0; i < SideLength; i++)
                {
                    //3d index into a flattened list
                    var cube = Cubes[(int)(i + (j * SideLength) + (k * SideLength * SideLength))];
                    cube.transform.localPosition = (new Vector3(cubeSize.x * i, cubeSize.y * j, cubeSize.z * k) * GapFactor)
                                                + finalOffset;
                    cube.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);
                    RpcSetCubeTransformData(cube, cube.transform.position, CubeSize);
                }
            }
        }
    }

    protected virtual void PreCubeSpawned(GameObject cube) { }
    protected virtual void PostCubeSpawned(GameObject cube) { }

    protected abstract void BaseActivateBehaviour();
    protected abstract void BaseDeactivateBehaviour();
    [Server]
    protected abstract void SetCubeInteractionRadius(float r);

    protected void OnTriggerEnter(Collider other)
    {
        if (!enabled || !isServer) return;

        if (TriggeringTags.Contains(other.tag))
        {
            TriggeredCount++;

            if (TriggeredCount == 1)
            {
                BaseActivateBehaviour();
            }
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (!enabled || !isServer) return;

        if (TriggeringTags.Contains(other.tag))
        {
            TriggeredCount--;

            if (TriggeredCount <= 0)
            {
                TriggeredCount = 0;
                BaseDeactivateBehaviour();
            }
        }
    }

    /// <summary>
    /// Rechecks the current TriggeredCount, calling Active/RpcDeactivateBehaviour as needed.
    /// Call if a child class has to modify TriggeredCount.
    /// </summary>
    protected void EvaluateTriggeredCount()
    {
        if (TriggeredCount >= 1)
        {
            BaseActivateBehaviour();
        }
        else if (TriggeredCount == 0)
        {
            BaseDeactivateBehaviour();
        }
    }

    [ClientRpc]
    private void RpcSetAsChild(GameObject cube)
    {
        cube.transform.parent = transform;
    }

    [ClientRpc]
    private void RpcSetCubeTransformData(GameObject cube, Vector3 position, float scale)
    {
        cube.transform.position = position;
        cube.transform.localScale = new Vector3(scale, scale, scale);
    }

    //[ClientRpc]
    private void ClearNullCubes()
    {
        for (var i = 0; i < Cubes.Count; i++)
        {
            if (Cubes[i] != null) continue;
            Cubes.RemoveAt(i);
            i--;
        }
    }

    [Server]
    protected virtual void SetGapFactor(float gapFactor)
    {
        if (GapFactor == gapFactor)
            return;

        GapFactor = gapFactor;
        RepositionCubes();

        RpcSetGapFactor(GapFactor);
    }

    [ClientRpc]
    private void RpcSetGapFactor(float gapFactor)
    {
        GapFactor = gapFactor;
    }

    [Server]
    protected virtual void SetSideLength(uint length)
    {
        if (SideLength == length)
            return;

        SideLength = Math.Min(SIDE_LENGTH_MAX, length);
        RpcSetSideLength(SideLength);

        foreach (var cube in Cubes)
        {
            NetworkServer.Destroy(cube);
        }
        Cubes.Clear();
        SpawnCubes();
    }

    [ClientRpc]
    private void RpcSetSideLength(uint length)
    {
        SideLength = length;
    }

    [Server]
    protected virtual void SetCubeSize(float newSize)
    {
        if (CubeSize == newSize)
            return;

        CubeSize = newSize;

        RepositionCubes();

        RpcSetCubeSize(CubeSize);
    }

    [ClientRpc]
    private void RpcSetCubeSize(float newSize)
    {
        CubeSize = newSize;
    }

    [Server]
    protected virtual void SetColor(float r, float g, float b)
    {
        Color = new Color(r, g, b);
        ChangeColor(Color);
    }

    [ClientRpc]
    protected virtual void RpcChangeColor(GameObject cube, float r, float g, float b)
    {
        cube.GetComponent<Renderer>().material.color = new Color(r, g, b);
    }

    protected virtual void ChangeColor(Color c)
    {
        var newColor = c;
        foreach (var cube in Cubes)
        {
            cube.GetComponent<Renderer>().material.color = newColor;
            RpcChangeColor(cube, c.r, c.g, c.b);
        }
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        try
        {
            switch (tokens[1])
            {
                case "CubeInteractRadius":
                    var radius = float.Parse(tokens[2]);
                    if (radius < 0)
                    {
                        return "Cannot change CubeInteractRadius to " + radius;
                    }
                    SetCubeInteractionRadius(radius);
                    return tokens[0] + "CubeInteractRadius changed to " + radius;
                case "GapFactor":
                    SetGapFactor(float.Parse(tokens[2]));
                    return tokens[0] + " GapFactor changed to " + GapFactor;
                case "SideLength":
                    var length = uint.Parse(tokens[2]);
                    SetSideLength(length);
                    return tokens[0] + " SideLength changed to " + SideLength;
                case "Color":
                    var r = Mathf.Clamp(float.Parse(tokens[2]), 0f, 1f);
                    var g = Mathf.Clamp(float.Parse(tokens[3]), 0f, 1f);
                    var b = Mathf.Clamp(float.Parse(tokens[4]), 0f, 1f);

                    SetColor(r, g, b);
                    return tokens[0] + " CubeColor changed to " + "{" + r + ", " + g + ", " + b + "}";
                case "CubeSize":
                    var newSize = float.Parse(tokens[2]);
                    if (newSize <= 0)
                    {
                        return "Cannot change CubeSize to " + newSize;
                    }
                    SetCubeSize(newSize);
                    return tokens[0] + " CubeSize changed to " + newSize;
                default:
                    return base.RunCommand(cmd, sender);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e, this);
            return e.Message;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        if (CubePrefab == null)
            return;

        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = size * CubeSize;

        Gizmos.DrawCube(transform.position, cubeSize * SideLength * GapFactor);

        var centerOffset = cubeSize * (SideLength / 2f) * GapFactor;
        var cubeOffset = (cubeSize * GapFactor / 2f);
        var finalOffset = transform.position - centerOffset + cubeOffset;

        for (var k = 0; k < SideLength; k++)
        {
            for (var j = 0; j < SideLength; j++)
            {
                for (var i = 0; i < SideLength; i++)
                {
                    var pos = (new Vector3(cubeSize.x * i, cubeSize.y * j, cubeSize.z * k) * GapFactor) + finalOffset;
                    Gizmos.DrawWireCube(pos, cubeSize);
                }
            }
        }
    }
}

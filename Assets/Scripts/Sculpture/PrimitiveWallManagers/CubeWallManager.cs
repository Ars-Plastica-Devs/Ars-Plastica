using System;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.2f)]
public abstract class CubeWallManager : BaseCubeManager
{
    private const uint SIDE_LENGTH_MAX = 30;
    public float GapFactor = 1f;

    protected override void SpawnCubes()
    {
        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = size * CubeSize;

        //we want the cube of cubes to be centered on this game object
        var centerOffset = cubeSize * (SideLength / 2f) * GapFactor;
        centerOffset = new Vector3(centerOffset.x, centerOffset.y, 0f);
        var cubeOffset = (cubeSize * GapFactor / 2f);
        cubeOffset = new Vector3(cubeOffset.x, cubeOffset.y, 0f);
        var finalOffset = /*transform.position */-centerOffset + cubeOffset;

        for (var j = 0; j < SideLength; j++)
        {
            for (var i = 0; i < SideLength; i++)
            {
                var pos = (new Vector3(cubeSize.x * i, cubeSize.y * j, 0) * GapFactor) + finalOffset;
                var cube = (GameObject)Instantiate(CubePrefab, pos, Quaternion.identity);
                cube.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);
                cube.transform.SetParent(transform, false);

                PreCubeSpawned(cube, i, j);

                Cubes.Add(cube);
                NetworkServer.Spawn(cube);

                RpcSetAsChild(cube);

                PostCubeSpawned(cube);
            }
        }
    }

    protected override void RepositionCubes()
    {
        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = new Vector3(CubeSize * size.x, CubeSize * size.y, CubeSize * size.z);

        //we want the cube of cubes to be centered on this game object
        var centerOffset = cubeSize * (SideLength / 2f) * GapFactor;
        centerOffset = new Vector3(centerOffset.x, centerOffset.y, 0f);
        var cubeOffset = (cubeSize * GapFactor / 2f);
        cubeOffset = new Vector3(cubeOffset.x, cubeOffset.y, 0f);
        var finalOffset = /*transform.position */-centerOffset + cubeOffset;

        for (var j = 0; j < SideLength; j++)
        {
            for (var i = 0; i < SideLength; i++)
            {
                //2d index into a flattened list
                var cube = Cubes[(int)(i + (j * SideLength))];
                cube.transform.localPosition = (new Vector3(cubeSize.x * i, cubeSize.y * j, 0) * GapFactor)
                                            + finalOffset;
                cube.transform.localScale = new Vector3(CubeSize, CubeSize, CubeSize);
                RpcSetCubeTransformData(cube, cube.transform.position, CubeSize);
            }
        }
    }

    public override float GetBoundingSphereRadius()
    {
        var cubeLength = CubePrefab.GetComponent<Renderer>().bounds.size.x * GapFactor;
        return cubeLength * SideLength * Mathf.Sqrt(3f) * .5f;
    }

    protected virtual void PreCubeSpawned(GameObject cube, int x, int y) { }
    protected virtual void PostCubeSpawned(GameObject cube) { }

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

    [ClientRpc]
    private void RpcClearNullCubes()
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
        RpcSetSideLength(length);
        var newSize = (int)(SideLength * SideLength);

        //Shrinking
        if (newSize < Cubes.Count)
        {
            for (var i = newSize; i < Cubes.Count; i++)
            {
                //Destroy(Cubes[i]);
                NetworkServer.Destroy(Cubes[i]);
            }
            Cubes = Cubes.GetRange(0, newSize);
            RpcClearNullCubes();
            RepositionCubes();
        }
        //Getting bigger
        else
        {
            while (Cubes.Count < newSize)
            {
                //Spawn at Zero, will reposition outside of the while loop
                var cube = (GameObject)Instantiate(CubePrefab, Vector3.zero, Quaternion.identity);
                cube.transform.parent = transform;

                Cubes.Add(cube);
                PreCubeSpawned(cube, (int) (Cubes.Count - (Cubes.Count / SideLength)), (int) (Cubes.Count / SideLength));

                NetworkServer.Spawn(cube);

                RpcSetAsChild(cube);

                PostCubeSpawned(cube);
            }

            RepositionCubes();
        }
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
        RpcSetCubeSize(CubeSize);

        RepositionCubes();
    }

    [ClientRpc]
    private void RpcSetCubeSize(float newSize)
    {
        CubeSize = newSize;
    }

    [Server]
    protected virtual void SetColor(float r, float g, float b)
    {
        ChangeColor(r, g, b);
    }

    [ClientRpc]
    protected virtual void RpcChangeColor(GameObject cube, float r, float g, float b)
    {
        cube.GetComponent<Renderer>().material.color = new Color(r, g, b);
    }

    protected virtual void ChangeColor(float r, float g, float b)
    {
        var newColor = new Color(r, g, b);
        foreach (var cube in Cubes)
        {
            cube.GetComponent<Renderer>().material.color = newColor;
            RpcChangeColor(cube, r, g, b);
        }
    }

    public override string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        switch (tokens[1])
        {
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

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        if (CubePrefab == null)
            return;

        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = size * CubeSize;

        var boundingBoxSize = cubeSize * SideLength * GapFactor;
        boundingBoxSize = new Vector3(boundingBoxSize.x, boundingBoxSize.y, CubeSize);
        Gizmos.DrawCube(transform.position, boundingBoxSize);

        var centerOffset = cubeSize * (SideLength / 2f) * GapFactor;
        centerOffset = new Vector3(centerOffset.x, centerOffset.y, 0f);
        var cubeOffset = (cubeSize * GapFactor / 2f);
        cubeOffset = new Vector3(cubeOffset.x, cubeOffset.y, 0f);
        var finalOffset = transform.position - centerOffset + cubeOffset;

        for (var j = 0; j < SideLength; j++)
        {
            for (var i = 0; i < SideLength; i++)
            {
                var pos = (new Vector3(cubeSize.x * i, cubeSize.y * j, 0) * GapFactor) + finalOffset;
                Gizmos.DrawWireCube(pos, cubeSize);
            }
        }
    }
}

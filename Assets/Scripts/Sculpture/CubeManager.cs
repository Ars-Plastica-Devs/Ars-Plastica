using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public abstract class CubeManager : NetworkBehaviour, ICommandReceiver
{
    [SyncVar]
    private float m_CubeSize;
    protected List<GameObject> Cubes = new List<GameObject>();
    protected int TriggeredCount;
    
    [SyncVar]
    public int SideLength;
    public string TriggeringTag;
    public GameObject CubePrefab;
    [SyncVar]
    public float GapFactor;

    protected virtual void Start()
    {
        m_CubeSize = CubePrefab.transform.localScale.x;

        Invoke("RegisterAsReceiver", 1f);

        if (!isServer) return;

        SpawnCubes();
    }

    private void RegisterAsReceiver()
    {
        GameObject.FindGameObjectsWithTag("Player").First(cp => cp.GetComponent<CommandProcessor>().enabled).GetComponent<CommandProcessor>().RegisterReceiver(gameObject);
    }

    protected virtual void SpawnCubes()
    {
        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = new Vector3(m_CubeSize * size.x, m_CubeSize * size.y, m_CubeSize * size.z);

        //we want the cube of cubes to be centered on this game object
        var centerOffset = cubeSize * (SideLength / 2f);

        for (var k = 0; k < SideLength; k++)
        {
            for (var j = 0; j < SideLength; j++)
            {
                for (var i = 0; i < SideLength; i++)
                {
                    var pos = (new Vector3(cubeSize.x * i, cubeSize.y * j, cubeSize.z * k) * GapFactor) + transform.position;
                    var cube = (GameObject)Instantiate(CubePrefab, pos - centerOffset, Quaternion.identity);
                    cube.transform.localScale = new Vector3(m_CubeSize, m_CubeSize, m_CubeSize);
                    cube.transform.parent = transform;

                    PreCubeSpawned(cube);

                    Cubes.Add(cube);
                    NetworkServer.Spawn(cube);

                    RpcSetAsChild(cube);

                    PostCubeSpawned(cube);
                }
            }
        }
    }

    protected virtual void PreCubeSpawned(GameObject cube) { }
    protected virtual void PostCubeSpawned(GameObject cube) { }

    [Command]
    protected abstract void CmdActivateBehaviour();
    [Command]
    protected abstract void CmdDeactivateBehaviour();
    [ClientRpc]
    protected abstract void RpcActivateBehaviour();
    [ClientRpc]
    protected abstract void RpcDeactivateBehaviour();
    protected abstract void ActivateBehaviour();
    protected abstract void DeactivateBehaviour();


    protected void OnTriggerEnter(Collider other)
    {
        if (!enabled || !isServer) return;

        if (other.gameObject.tag == TriggeringTag)
        {
            TriggeredCount++;

            if (TriggeredCount == 1)
            {
                ActivateBehaviour();
            }
        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (!enabled || !isServer) return;

        if (other.gameObject.tag == TriggeringTag)
        {
            TriggeredCount--;

            if (TriggeredCount <= 0)
            {
                TriggeredCount = 0;
                DeactivateBehaviour();
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
            CmdActivateBehaviour();
        }
        else if (TriggeredCount == 0)
        {
            CmdDeactivateBehaviour();
        }
    }

    protected virtual void RepositionCubes()
    {
        var size = CubePrefab.GetComponent<BoxCollider>().size;
        var cubeSize = new Vector3(m_CubeSize * size.x, m_CubeSize * size.y, m_CubeSize * size.z);

        //we want the cube of cubes to be centered on this game object
        var centerOffset = cubeSize * (SideLength / 2f);

        for (var k = 0; k < SideLength; k++)
        {
            for (var j = 0; j < SideLength; j++)
            {
                for (var i = 0; i < SideLength; i++)
                {
                    //3d index into a flattened list
                    var cube = Cubes[i + (j * SideLength) + (k * SideLength * SideLength)];
                    cube.transform.position = (new Vector3(cubeSize.x * i, cubeSize.y * j, cubeSize.z * k) * GapFactor)
                                                + transform.position - centerOffset;
                    cube.transform.localScale = new Vector3(m_CubeSize, m_CubeSize, m_CubeSize);
                    RpcSetCubeTransformData(cube, cube.transform.position, cube.transform.localScale);
                }
            }
        }
    }

    [ClientRpc]
    private void RpcSetAsChild(GameObject cube)
    {
        cube.transform.parent = transform;
    }

    [ClientRpc]
    private void RpcSetCubeTransformData(GameObject cube, Vector3 position, Vector3 scale)
    {
        cube.transform.position = position;
        cube.transform.localScale = scale;
    }

    [Command]
    protected virtual void CmdOnGapFactorChanged()
    {
        RepositionCubes();
    }

    [Command]
    protected virtual void CmdOnSideLengthChanged()
    {
        var newSize = SideLength * SideLength * SideLength;

        //Shrinking
        if (newSize < Cubes.Count)
        {
            for (var i = newSize; i < Cubes.Count; i++)
            {
                NetworkServer.Destroy(Cubes[i]);
                Destroy(Cubes[i]);
            }
            Cubes = Cubes.GetRange(0, newSize);
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

                PreCubeSpawned(cube);

                Cubes.Add(cube);
                NetworkServer.Spawn(cube);
            }

            RepositionCubes();
        }
    }

    [Command]
    protected virtual void CmdOnCubeSizeChanged(float newSize)
    {
        m_CubeSize = newSize;

        RepositionCubes();
    }

    [Command]
    protected virtual void CmdChangeColor(float r, float g, float b)
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

    public abstract bool IsCommandRelevant(string cmd);

    public virtual string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        try
        {
            switch (tokens[1])
            {
                case "GapFactor":
                    var oldGap = GapFactor;
                    GapFactor = float.Parse(tokens[2]);
                    if (GapFactor != oldGap) CmdOnGapFactorChanged();
                    return tokens[0] + " GapFactor changed to " + GapFactor;
                case "SideLength":
                    var oldLength = SideLength;
                    SideLength = int.Parse(tokens[2]);
                    if (SideLength < 0)
                    {
                        var badLength = SideLength;
                        SideLength = oldLength;
                        return "Cannot change SideLength to " + badLength;
                    }
                    if (SideLength != oldLength) CmdOnSideLengthChanged();
                    return tokens[0] + " SideLength changed to " + SideLength;
                case "Color":
                    var oldColor = Cubes[0].GetComponent<Renderer>().material.color;
                    var r = Mathf.Clamp(float.Parse(tokens[2]), 0f, 1f);
                    var g = Mathf.Clamp(float.Parse(tokens[3]), 0f, 1f);
                    var b = Mathf.Clamp(float.Parse(tokens[4]), 0f, 1f);

                    var newColor = new Color(r, g, b);
                    if (newColor != oldColor)
                    {
                        CmdChangeColor(r, g, b);
                    }
                    return tokens[0] + " CubeColor changed to " + "{" + r + ", " + g + ", " + b + "}";
                case "CubeSize":
                    var newSize = float.Parse(tokens[2]);
                    if (newSize <= 0)
                    {
                        return "Cannot change CubeSize to " + newSize;
                    }
                    if (newSize != m_CubeSize) CmdOnCubeSizeChanged(newSize);
                    return tokens[0] + " CubeSize changed to " + newSize;
                default:
                    return "Not a valid command";
            }
        }
        catch
        {
            return "Not a valid command";
        }
    }
}

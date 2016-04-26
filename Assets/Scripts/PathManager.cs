using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PathManager : NetworkBehaviour, ICommandReceiver
{
    private enum State
    {
        None,
        Saving,
        NextPoint
    }

    private State m_State = State.None;
    private string m_LastPathName = string.Empty;
    private int m_LastPathPoint;
    private FileInfo m_TargetFile;

    private readonly Dictionary<string, GameObject> m_Conveyors = new Dictionary<string, GameObject>(); 
    private readonly Dictionary<string, GameObject> m_PathRenderers = new Dictionary<string, GameObject>(); 

    public Dictionary<string, List<Vector3>> Paths = new Dictionary<string, List<Vector3>>();

    public GameObject PathRendererPrefab;
    public GameObject ConveyancePrefab;

    private void Start()
    {
        if (!isServer)
            return;

        m_TargetFile = new FileInfo("Data/paths.txt");

        if (m_TargetFile.Exists)
        {
            Paths = LoadPaths(m_TargetFile);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Invoke("RegisterAsReceiver", 1f);
        //RegisterAsReceiver();
    }

    private void RegisterAsReceiver()
    {
        GameObject.FindGameObjectsWithTag("Player").First(cp => cp.GetComponent<NetworkIdentity>().isLocalPlayer).GetComponent<CommandProcessor>().RegisterReceiver(gameObject);
    }

    public bool IsCommandRelevant(string cmd)
    {
        return cmd.StartsWith("path") || (m_State != State.None && (cmd.StartsWith("Y") || cmd.StartsWith("N")));
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        var tokens = cmd.Split(' ');

        if (tokens.Length > 2)
        {
            switch (tokens[2])
            {
                case "show":
                    return DoShowCommand(tokens);
                case "hide":
                    return DoHideCommand(tokens);
                case "remove":
                    return DoRemoveCommand(tokens);
                case "spawn":
                    return DoSpawnCommand(tokens[1]);
            }
        }

        if (tokens.Length > 1 && tokens[1] == "save-all")
        {
            SavePaths(m_TargetFile, Paths);
            return "Saved all paths";
        }

        switch (m_State)
        {
            case State.Saving:
                if (tokens[0] == "Y")
                {
                    //Save path
                    SavePaths(m_TargetFile, Paths);

                    if (m_Conveyors.ContainsKey(m_LastPathName))
                        m_Conveyors[m_LastPathName].GetComponent<ConveyanceController>().RefreshPath();

                    m_State = State.NextPoint;
                    return "Path " + m_LastPathName + " saved. Path " + m_LastPathName + " log location " +
                           (m_LastPathPoint + 1) + " Y/N?";
                }

                //Remove path
                //TODO: Should have a working copy, not erasing automatically
                Paths.Remove(m_LastPathName);
                m_State = State.None;
                return "";
            case State.NextPoint:
                if (tokens[0] == "Y")
                {
                    //Fake a tokens array to re-use the logic
                    return DoLogCommand(new[]
                    {
                        "path", m_LastPathName, "log", "location", (m_LastPathPoint + 1).ToString()
                    }, sender);
                }

                m_State = State.None;
                return "";
            case State.None:
                m_LastPathName = tokens[1];

                if (!Paths.ContainsKey(tokens[1]))
                {
                    Paths.Add(tokens[1], new List<Vector3>());
                }

                if (tokens[2] == "log")
                {
                    return DoLogCommand(tokens, sender);
                }

                return "";
        }

        return "";
    }

    private string DoSpawnCommand(string pathName)
    {
        var con = (GameObject) Instantiate(ConveyancePrefab, Paths[pathName][0], Quaternion.identity);
        m_Conveyors.Add(pathName, con);
        var controller = con.GetComponent<ConveyanceController>();
        controller.Manager = this;
        controller.SetPath(pathName);
        NetworkServer.Spawn(con);

        return "Spawned a conveyor on path " + pathName;
    }

    private string DoShowCommand(string[] tokens)
    {
        try
        {
            if (m_PathRenderers.ContainsKey(tokens[1]))
                return "Path " + tokens[1] + " is already being shown";

            var path = Paths[tokens[1]];
            var obj = (GameObject)Instantiate(PathRendererPrefab, Vector3.zero, Quaternion.identity);
            NetworkServer.Spawn(obj);

            m_PathRenderers.Add(tokens[1], obj);
            var renderer = obj.GetComponent<LineRenderer>();
            renderer.material.color = Color.green;
            renderer.SetVertexCount(path.Count);
            renderer.SetPositions(path.ToArray());
            renderer.SetColors(Color.green, Color.green);

            RpcAddPathRenderer(tokens[1], obj, path.ToArray(), Color.green);

            return "Showing path " + tokens[1];
        }
        catch(Exception e)
        {
            return e.Message;
        }
    }

    private string DoHideCommand(string[] tokens)
    {
        try
        {
            if (!m_PathRenderers.ContainsKey(tokens[1]))
                return "Path " + tokens[1] + " is not being shown";

            RpcRemovePathRenderer(tokens[1]);
            NetworkServer.Destroy(m_PathRenderers[tokens[1]]);
            m_PathRenderers.Remove(tokens[1]);

            return "Hiding path " + tokens[1];
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    private string DoRemoveCommand(string[] tokens)
    {
        try
        {
            if (tokens.GetLength(0) == 3)
            {
                Paths.Remove(tokens[1]);

                if (m_Conveyors.ContainsKey(tokens[1]))
                {
                    NetworkServer.Destroy(m_Conveyors[tokens[1]]);
                    m_Conveyors.Remove(tokens[1]);
                }
                
                return "Removed path " + tokens[1];
            }
            
            if (tokens[3] != "location")
                return "Not a valid command segment: " + tokens[3];

            var point = int.Parse(tokens[4]);

            if (Paths[tokens[1]].Count < point)
                return "The path " + tokens[1] + " is not that long";

            Paths[tokens[1]].RemoveAt(point - 1);

            if (m_Conveyors.ContainsKey(tokens[1]))
                m_Conveyors[tokens[1]].GetComponent<ConveyanceController>().RefreshPath();

            return "Removed point " + point + " from path " + tokens[1];
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    [ClientRpc]
    private void RpcAddPathRenderer(string pathName, GameObject obj, Vector3[] path, Color color)
    {
        m_PathRenderers.Add(pathName, obj);
        var renderer = obj.GetComponent<LineRenderer>();
        renderer.material.color = color;
        renderer.SetVertexCount(path.GetLength(0));
        renderer.SetPositions(path);
        renderer.SetColors(color, color);
    }

    [ClientRpc]
    private void RpcRemovePathRenderer(string pathName)
    {
        m_PathRenderers.Remove(pathName);
    }

    private string DoLogCommand(string[] tokens, GameObject sender)
    {
        if (tokens[3] != "location") return "Not a valid command";

        var locationNumber = int.Parse(tokens[4]);

        var playerPos = sender.transform.position;

        if (Paths[tokens[1]].Count == locationNumber - 1)
            Paths[tokens[1]].Add(playerPos);
        else
            Paths[tokens[1]][locationNumber - 1] = playerPos;

        m_LastPathPoint = locationNumber;
        m_State = State.Saving;
        return "path " + tokens[1] + ", location " + tokens[4] + " logged. Save Path " + tokens[1] + ", Y/N?";
    }

    private void OnApplicationQuit()
    {
        SavePaths(m_TargetFile, Paths);
    }

    private static void SavePaths(FileInfo file, Dictionary<string, List<Vector3>> paths)
    {
        var sr = new StringBuilder();
        foreach (var path in paths)
        {
            sr.AppendLine(path.Key);

            foreach (var pos in path.Value)
            {
                sr.Append(pos.x + " ");
                sr.Append(pos.y + " ");
                sr.AppendLine(pos.z.ToString());
            }

            sr.AppendLine();
        }

        File.WriteAllText(file.FullName, sr.ToString());
    }

    private static Dictionary<string, List<Vector3>> LoadPaths(FileInfo file)
    {
        var paths = new Dictionary<string, List<Vector3>>();

        using (var reader = new StreamReader(file.FullName))
        {
            string line;
            string pathName = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    pathName = null;
                    continue;
                }

                if (pathName == null)
                {
                    pathName = line;
                    paths.Add(pathName, new List<Vector3>());
                    continue;
                }

                var components = line.Split(' ');
                var x = float.Parse(components[0]);
                var y = float.Parse(components[1]);
                var z = float.Parse(components[2]);
                var pos = new Vector3(x, y, z);

                paths[pathName].Add(pos);
            }
        }

        return paths;
    }
}

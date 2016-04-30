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
    private FileInfo m_PathFile;
    private FileInfo m_BeaconFile;

    private readonly List<ConveyanceController> m_FinishedConveyances = new List<ConveyanceController>(); 
    private readonly Dictionary<string, List<ConveyanceController>> m_Conveyors = new Dictionary<string, List<ConveyanceController>>();
    private readonly Dictionary<string, GameObject> m_PathRenderers = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject[]> m_PathObjects = new Dictionary<string, GameObject[]>();
    private readonly Dictionary<ConveyanceController, PlayerInteractionController> m_PlayersAttachedToConveyances = new Dictionary<ConveyanceController, PlayerInteractionController>(); 
    private Dictionary<string, GameObject> m_Beacons = new Dictionary<string, GameObject>();

    public Dictionary<string, List<Vector3>> Paths = new Dictionary<string, List<Vector3>>();

    public GameObject PathRendererPrefab;
    public GameObject ConveyancePrefab;
    public GameObject PathObjectPrefab;
    public GameObject BeaconPrefab;

    private void Start()
    {
        if (!isServer)
            return;

        m_BeaconFile = new FileInfo("Data/beacons.txt");
        m_PathFile = new FileInfo("Data/paths.txt");

        if (m_PathFile.Exists && m_BeaconFile.Exists)
        {
            Paths = LoadPaths(m_PathFile);
            m_Beacons = LoadBeacons(m_BeaconFile);

            foreach (var beacon in m_Beacons.Values)
            {
                //beacon.transform.parent = transform;
                NetworkServer.Spawn(beacon);
            }

            CreatePathObjects();
            AssignPathObjectsToBeacons();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Invoke("RegisterAsReceiver", 3f);
        //RegisterAsReceiver();
    }

    [Server]
    public GameObject SpawnConveyance(string pathName, bool reversePath = false)
    {
        GameObject obj;
        DoSpawnConveyanceCommand(pathName, out obj, reversePath);
        return obj;
    }

    [Server]
    public void NotifyOfConveyanceFinished(ConveyanceController conveyance)
    {
        if (m_PlayersAttachedToConveyances.ContainsKey(conveyance))
        {
            m_PlayersAttachedToConveyances[conveyance].DetachClientsFromConveyance();
            m_PlayersAttachedToConveyances.Remove(conveyance);
        }

        m_Conveyors[conveyance.CurrentPath].Remove(conveyance);
        m_FinishedConveyances.Add(conveyance);

        //Give clients a chance to detach from the conveyances
        Invoke("DestroyConveyance", 1f);
    }

    [Server]
    private void DestroyConveyance()
    {
        if (!m_FinishedConveyances.Any()) return;

        for (var i = 0; i < m_FinishedConveyances.Count; i++)
        {
            if (m_FinishedConveyances[i].AttachedCount != 0) continue;

            NetworkServer.Destroy(m_FinishedConveyances[i].gameObject);
            m_FinishedConveyances.RemoveAt(i);
            i--;
        }
        
        //If we have conveyances that have not been cleared of players
        if (m_FinishedConveyances.Count != 0)
            Invoke("DestroyConveyance", .5f);
    }

    [Server]
    public void NotifyOfAttachToConveyance(PlayerInteractionController player, ConveyanceController conveyance)
    {
        conveyance.AttachObject(player.gameObject);
        conveyance.ServerStartRunning();
        m_PlayersAttachedToConveyances[conveyance] = player;
    }

    [Server]
    public void RemovePlayerFromConveyance(PlayerInteractionController player, ConveyanceController conveyance)
    {
        conveyance.DetachObject(player.gameObject);
        m_PlayersAttachedToConveyances.Remove(conveyance);
    }

    private void RegisterAsReceiver()
    {
        GameObject.FindGameObjectsWithTag("Player").First(cp => cp.GetComponent<NetworkIdentity>() && cp.GetComponent<NetworkIdentity>().isLocalPlayer).GetComponent<CommandProcessor>().RegisterReceiver(gameObject);
    }

    private void CreatePathObjects()
    {
        foreach (var path in Paths.Where(p => p.Value.Count > 1))
        {
            PlacePathEndPoints(path.Key);
        }
    }

    private void AssignPathObjectsToBeacons()
    {
        var validPathObjects = new HashSet<GameObject>();

        foreach (var goArr in m_PathObjects.Values)
        {
            validPathObjects.Add(goArr[0]);
            validPathObjects.Add(goArr[1]);
        }

        foreach (var beacon in m_Beacons.Values.Select(b => b.GetComponent<BeaconController>()))
        {
            beacon.DetectPaths(validPathObjects);
        }
    }

    private void PlacePathEndPoints(string pathName)
    {
        var path = Paths[pathName];

        if (path.Count < 2) return;

        if (m_PathObjects.ContainsKey(pathName))
        {
            var endPoints = m_PathObjects[pathName];
            endPoints[0].transform.position = path[0];
            endPoints[1].transform.position = path[path.Count - 1];
            return;
        }

        var pathStartObj = (GameObject)Instantiate(PathObjectPrefab, path[0], Quaternion.identity);
        var pathEndObj = (GameObject)Instantiate(PathObjectPrefab, path[path.Count - 1], Quaternion.identity);

        NetworkServer.Spawn(pathStartObj);
        NetworkServer.Spawn(pathEndObj);

        pathStartObj.name = pathName + " Start";
        pathEndObj.name = pathName + " End";

        pathStartObj.transform.parent = transform;
        pathEndObj.transform.parent = transform;

        m_PathObjects.Add(pathName, new[] { pathStartObj, pathEndObj });
    }

    public bool IsCommandRelevant(string cmd)
    {
        return cmd.StartsWith("path") || cmd.StartsWith("beacon") || m_State != State.None && (cmd.StartsWith("Y") || cmd.StartsWith("N"));
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        var tokens = cmd.Split(' ');

        if (tokens[0] == "beacon")
        {
            return DoBeaconCommand(tokens, sender);
        }

        if (tokens.Length > 2)
        {
            switch (tokens[2])
            {
                case "show":
                    return DoShowPathCommand(tokens);
                case "hide":
                    return DoHidePathCommand(tokens);
                case "remove":
                    return DoRemovePathCommand(tokens);
                case "insert":
                    return DoInsertOnPathCommand(tokens, sender);
                case "spawn":
                    GameObject obj;
                    return DoSpawnConveyanceCommand(tokens[1], out obj);
            }
        }

        if (tokens.Length > 1 && tokens[1] == "save-all")
        {
            SavePaths(m_PathFile, Paths);
            return "Saved all paths";
        }

        switch (m_State)
        {
            case State.Saving:
                if (tokens[0] == "Y")
                {
                    //Save path
                    SavePaths(m_PathFile, Paths);
                    PlacePathEndPoints(m_LastPathName);
                    AssignPathObjectsToBeacons();

                    //TODO: re-evaluate if this is still relevant
                    if (m_Conveyors.ContainsKey(m_LastPathName))
                    {
                        foreach (var cc in m_Conveyors[m_LastPathName])
                        {
                            cc.RefreshPath();
                        }
                    }

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

    private string DoBeaconCommand(string[] tokens, GameObject sender)
    {
        if (tokens[1] == "save-all" && tokens.GetLength(0) == 2)
        {
            SaveBeacons(m_BeaconFile);
            return "Saved all beacons";
        }

        if (tokens[1] == "connect")
        {
            if (tokens.GetLength(0) != 5)
                return "Expected 3 arguments to connect, received " + (tokens.GetLength(0) - 2);
            return DoConnectCommand(tokens[2], tokens[3], tokens[4], sender);
        }

        var beaconName = tokens[1];

        if (tokens[2] == "create")
        {
            return DoCreateBeaconCommand(beaconName, sender);
        }

        if (!m_Beacons.ContainsKey(beaconName))
            return "Unrecognized Beacon Name: " + beaconName;

        if (tokens[2] == "delete")
        {
            return DoDeleteBeaconCommand(beaconName, sender);
        }

        return "Unrecognized beacon command: " + tokens[2];
    }

    private string DoConnectCommand(string beaconOne, string beaconTwo, string pathName, GameObject sender)
    {
        if (!m_Beacons.ContainsKey(beaconOne))
        {
            return "The beacon " + beaconOne + " does not exist";
        }
        if (!m_Beacons.ContainsKey(beaconTwo))
        {
            return "The beacon " + beaconTwo + " does not exist";
        }
        if (Paths.ContainsKey(pathName))
        {
            return "The path " + pathName + " already exists. Remove it, or choose a different name.";
        }

        var start = m_Beacons[beaconOne].transform.position;
        var end = m_Beacons[beaconTwo].transform.position;

        Paths[pathName] = new List<Vector3>
        {
            start, end
        };

        SavePaths(m_PathFile, Paths);
        PlacePathEndPoints(pathName);
        AssignPathObjectsToBeacons();

        return "Created a path connecting " + beaconOne + " and " + beaconTwo + " named " + pathName;
    }

    private string DoDeleteBeaconCommand(string beaconName, GameObject sender)
    {
        NetworkServer.Destroy(m_Beacons[beaconName]);
        m_Beacons.Remove(beaconName);
        SaveBeacons(m_BeaconFile);

        return "Removed beacon " + beaconName;
    }

    private string DoCreateBeaconCommand(string beaconName, GameObject sender)
    {
        var beacon = (GameObject) Instantiate(BeaconPrefab, sender.transform.position, Quaternion.identity);
        NetworkServer.Spawn(beacon);
        beacon.name = beaconName;
        beacon.GetComponent<BeaconController>().Name = beaconName;
        beacon.GetComponent<BeaconController>().DetectPaths();
        m_Beacons.Add(beaconName, beacon);
        SaveBeacons(m_BeaconFile);

        return "Spawned Beacon " + beaconName;
    }

    private string DoSpawnConveyanceCommand(string pathName, out GameObject obj, bool reversePath = false)
    {
        var spawnPoint = reversePath ? Paths[pathName].Last() : Paths[pathName][0];

        var con = (GameObject) Instantiate(ConveyancePrefab, spawnPoint, Quaternion.identity);
        var controller = con.GetComponent<ConveyanceController>();

        obj = con;

        if (!m_Conveyors.ContainsKey(pathName))
        {
            m_Conveyors.Add(pathName, new List<ConveyanceController>());
        }

        m_Conveyors[pathName].Add(controller);
        controller.Manager = this;
        controller.SetPath(pathName, reversePath);
        NetworkServer.Spawn(con);

        return "Spawned a conveyor on path " + pathName;
    }

    private string DoShowPathCommand(string[] tokens)
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

    private string DoHidePathCommand(string[] tokens)
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

    private string DoRemovePathCommand(string[] tokens)
    {
        try
        {
            var pathName = tokens[1];

            if (tokens.GetLength(0) == 3)
            {
                Paths.Remove(pathName);

                //TODO: re-evaluate if this is still relevant
                if (m_Conveyors.ContainsKey(pathName))
                {
                    foreach (var cc in m_Conveyors[pathName])
                    {
                        NetworkServer.Destroy(cc.gameObject);
                    }
                    m_Conveyors.Remove(pathName);
                }
                if (m_PathObjects.ContainsKey(pathName))
                {
                    NetworkServer.Destroy(m_PathObjects[pathName][0]);
                    NetworkServer.Destroy(m_PathObjects[pathName][1]);
                    m_PathObjects.Remove(pathName);
                    AssignPathObjectsToBeacons();
                }

                return "Removed path " + pathName;
            }
            
            if (tokens[3] != "location")
                return "Not a valid command segment: " + tokens[3];

            var point = int.Parse(tokens[4]);

            if (Paths[pathName].Count < point)
                return "The path " + pathName + " is not that long";

            Paths[pathName].RemoveAt(point - 1);

            SavePaths(m_PathFile, Paths);
            PlacePathEndPoints(pathName);
            AssignPathObjectsToBeacons();

            if (m_Conveyors.ContainsKey(m_LastPathName))
            {
                foreach (var cc in m_Conveyors[m_LastPathName])
                {
                    cc.RefreshPath();
                }
            }

            return "Removed point " + point + " from path " + pathName;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    private string DoInsertOnPathCommand(string[] tokens, GameObject sender)
    {
        if (tokens.GetLength(0) != 4)
            return "Expected 1 argument to insert, received " + (tokens.GetLength(0) - 3);

        var pathName = tokens[1];

        if (!Paths.ContainsKey(pathName))
            return "Unrecognized path name: " + pathName;

        int index;
        if (!int.TryParse(tokens[3], out index))
            return "Failed to parse the point on the path: " + tokens[3];

        Paths[pathName].Insert(index - 1, sender.transform.position);
        SavePaths(m_PathFile, Paths);
        PlacePathEndPoints(pathName);
        AssignPathObjectsToBeacons();

        return "Inserted your current position as point " + index + " on path " + pathName;
    }

    [ClientRpc]
    private void RpcAddPathRenderer(string pathName, GameObject obj, Vector3[] path, Color color)
    {
        if (m_PathRenderers.ContainsKey(pathName))
            return;

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
        if (!isServer) return;
        SavePaths(m_PathFile, Paths);
        SaveBeacons(m_BeaconFile);
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

    private void SaveBeacons(FileInfo beaconFile)
    {
        var sr = new StringBuilder();
        foreach (var path in m_Beacons)
        {
            sr.AppendLine(path.Key);

            var pos = path.Value.transform.position;

            sr.Append(pos.x + " ");
            sr.Append(pos.y + " ");
            sr.AppendLine(pos.z.ToString());

            sr.AppendLine();
        }

        File.WriteAllText(beaconFile.FullName, sr.ToString());
    }

    private Dictionary<string, GameObject> LoadBeacons(FileInfo beaconFile)
    {
        var beacons = new Dictionary<string, GameObject>();

        using (var reader = new StreamReader(beaconFile.FullName))
        {
            string line;
            string name = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;

                if (name == null)
                {
                    name = line;
                    continue;
                }

                var components = line.Split(' ');
                var x = float.Parse(components[0]);
                var y = float.Parse(components[1]);
                var z = float.Parse(components[2]);
                var pos = new Vector3(x, y, z);

                var beacon = (GameObject) Instantiate(BeaconPrefab, pos, Quaternion.identity);
                beacons.Add(name, beacon);
                beacon.GetComponent<BeaconController>().Name = name;
                beacon.name = name;

                name = null;
            }
        }

        return beacons;
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

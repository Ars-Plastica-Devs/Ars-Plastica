using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PathManager : NetworkBehaviour, ICommandReceiver
{
    private FileInfo m_PathFile;
    private FileInfo m_BeaconFile;

    private readonly List<ConveyanceController> m_FinishedConveyances = new List<ConveyanceController>();
    private readonly HashSet<ConveyanceController> m_Conveyors = new HashSet<ConveyanceController>();
    private readonly Dictionary<ConveyanceController, PlayerInteractionController> m_PlayersAttachedToConveyances = new Dictionary<ConveyanceController, PlayerInteractionController>();

    private readonly Dictionary<string, PathRenderController> m_PathRenderers = new Dictionary<string, PathRenderController>();
    private readonly Dictionary<string, GameObject[]> m_PathEndObjects = new Dictionary<string, GameObject[]>();
    private Dictionary<string, BeaconController> m_Beacons = new Dictionary<string, BeaconController>();
    private readonly Dictionary<string, BeaconController[]> m_BeaconsOnPath = new Dictionary<string, BeaconController[]>();

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
            m_Beacons = LoadBeacons(m_BeaconFile);
            Paths = LoadPaths(m_PathFile);

            foreach (var beacon in m_Beacons.Values)
            {
                //beacon.transform.parent = transform;
                NetworkServer.Spawn(beacon.gameObject);
            }

            CreatePathObjects();
            AssignPathObjectsToBeacons();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        CommandProcessor.PendingReceivers.Add(gameObject);
    }

    [Server]
    public GameObject SpawnConveyance(string startBeaconName, string endBeaconName)
    {
        GameObject obj;
        DoSpawnConveyanceCommand(startBeaconName, endBeaconName, out obj);
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

        m_Conveyors.Remove(conveyance);
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

    public List<Vector3> GetPathFromBeacons(string startBeacon, string endBeacon)
    {
        foreach (var pathToBeacons in m_BeaconsOnPath)
        {
            var firstBeaconName = pathToBeacons.Value[0].GetComponent<BeaconController>().Name;
            var secondBeaconName = pathToBeacons.Value[1].GetComponent<BeaconController>().Name;

            if (firstBeaconName == startBeacon && secondBeaconName == endBeacon)
            {
                return Paths[pathToBeacons.Key];
            }
            if (firstBeaconName == endBeacon && secondBeaconName == startBeacon)
            {
                var copy = Paths[pathToBeacons.Key].ToList();
                copy.Reverse();
                return copy;
            }
        }

        return null;
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
        foreach (var beacon in m_Beacons.Values)
        {
            beacon.DetectPaths(m_BeaconsOnPath);
        }
    }

    private void PlacePathEndPoints(string pathName)
    {
        var path = Paths[pathName];

        if (path.Count < 2) return;

        //If these end points have already been created
        if (m_PathEndObjects.ContainsKey(pathName))
        {
            var endPoints = m_PathEndObjects[pathName];
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

        m_PathEndObjects.Add(pathName, new[] { pathStartObj, pathEndObj });
    }

    public bool IsCommandRelevant(string cmd, GameObject sender = null)
    {
        return cmd.StartsWith("path") || cmd.StartsWith("beacon")/* || m_State != State.None && (cmd.StartsWith("Y") || cmd.StartsWith("N"))*/;
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        var tokens = cmd.Split(' ');

        if (tokens[0] == "beacon")
        {
            return DoBeaconCommand(tokens, sender);
        }

        if (tokens[0] == "path")
        {
            switch (tokens[1])
            {
                case "show-all":
                    return DoShowAllCommand();
                case "hide-all":
                    return DoHideAllCommand();
            }
            switch (tokens[2])
            {
                case "show":
                    return DoShowPathCommand(tokens);
                case "hide":
                    return DoHidePathCommand(tokens);
                case "delete":
                    return DoRemovePathCommand(tokens);
                case "insert":
                    return DoInsertOnPathCommand(tokens, sender);
            }
        }

        /*if (tokens.Length > 1 && tokens[1] == "save-all")
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
        }*/

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
            if (tokens.GetLength(0) != 4)
                return "Expected 2 arguments to connect, received " + (tokens.GetLength(0) - 2);
            return DoConnectCommand(tokens[2], tokens[3]);
        }

        var beaconName = tokens[1];

        if (tokens.Length == 2)
        {
            return DoTeleportPlayerToBeacon(beaconName, sender);
        }

        if (tokens[2] == "create")
        {
            return DoCreateBeaconCommand(beaconName, sender);
        }

        if (!m_Beacons.ContainsKey(beaconName))
            return "Unrecognized Beacon Name: " + beaconName;

        if (tokens[2] == "delete")
        {
            return DoDeleteBeaconCommand(beaconName);
        }

        return "Unrecognized beacon command: " + tokens[2];
    }

    private string DoTeleportPlayerToBeacon(string beaconName, GameObject sender)
    {
        if (!m_Beacons.ContainsKey(beaconName))
            return "The given beacon does not exist: " + beaconName;

        var beacon = m_Beacons[beaconName];
        var pos = beacon.gameObject.transform.position;
        var cp = sender.GetComponent<CommandProcessor>();

        if (cp == null)
            return "Could not teleport player to beacon " + beaconName;

        cp.RunCommand("set-loc " + pos.x + " " + pos.y + " " + pos.z, sender);

        return "Teleported to beacon " + beaconName;
    }

    private string DoConnectCommand(string beaconOne, string beaconTwo)
    {
        var pathName = beaconOne + "-" + beaconTwo;

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

        m_Beacons[beaconOne].AddPath(pathName);
        m_Beacons[beaconTwo].AddPath(pathName);

        m_BeaconsOnPath.Add(pathName, new[] { m_Beacons[beaconOne], m_Beacons[beaconTwo] });
        PlacePathEndPoints(pathName);
        AssignPathObjectsToBeacons();
        SavePaths(m_PathFile, Paths);

        return "Created a path connecting " + beaconOne + " and " + beaconTwo + " named " + pathName;
    }

    private string DoDeleteBeaconCommand(string beaconName)
    {
        var connectedPaths = m_BeaconsOnPath.Where(kvp => kvp.Value.Contains(m_Beacons[beaconName]))
                                            .Select(bc => bc.Key).ToList();
        foreach (var connectedPath in connectedPaths)
        {
            DoRemovePathCommand(new[] {"path", connectedPath, "remove"});
        }

        NetworkServer.Destroy(m_Beacons[beaconName].gameObject);
        m_Beacons.Remove(beaconName);

        SaveBeacons(m_BeaconFile);

        return "Removed beacon " + beaconName;
    }

    private string DoCreateBeaconCommand(string beaconName, GameObject sender)
    {
        var beacon = (GameObject)Instantiate(BeaconPrefab, sender.transform.position, Quaternion.identity);
        NetworkServer.Spawn(beacon);
        beacon.name = beaconName;
        var controller = beacon.GetComponent<BeaconController>();
        controller.Name = beaconName;
        controller.DetectPaths(m_BeaconsOnPath);
        m_Beacons.Add(beaconName, controller);
        SaveBeacons(m_BeaconFile);

        return "Spawned Beacon " + beaconName;
    }

    private string DoSpawnConveyanceCommand(string startBeaconName, string endBeaconName, out GameObject obj)
    {
        var startBeacon = m_Beacons[startBeaconName];
        var spawnPoint = startBeacon.transform.position;

        var con = (GameObject)Instantiate(ConveyancePrefab, spawnPoint, Quaternion.identity);
        var controller = con.GetComponent<ConveyanceController>();

        obj = con;

        m_Conveyors.Add(controller);
        controller.Manager = this;
        controller.SetPath(startBeaconName, endBeaconName);
        NetworkServer.Spawn(con);

        return "Spawned a conveyor from " + startBeaconName + " to " + endBeaconName;
    }

    private string DoShowAllCommand()
    {
        foreach (var path in Paths)
        {
            DoShowPathCommand(new[] { "path", path.Key, "show" });
        }

        return "Showing all paths";
    }

    private string DoHideAllCommand()
    {
        foreach (var path in Paths)
        {
            DoHidePathCommand(new[] { "path", path.Key, "hide" });
        }

        return "Hiding all paths";
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

            var controller = obj.GetComponent<PathRenderController>();
            m_PathRenderers.Add(tokens[1], controller);

            controller.ServerSetPoints(path);
            controller.ServerSetName(tokens[1]);

            return "Showing path " + tokens[1];
        }
        catch (Exception e)
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

            //RpcRemovePathRenderer(tokens[1]);
            NetworkServer.Destroy(m_PathRenderers[tokens[1]].gameObject);
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

                if (m_PathRenderers.ContainsKey(pathName))
                {
                    DoHidePathCommand(new[] { "path", pathName, "hide" });
                }

                if (m_BeaconsOnPath.ContainsKey(pathName))
                {
                    m_BeaconsOnPath[pathName][0].RemovePath(pathName);
                    m_BeaconsOnPath[pathName][1].RemovePath(pathName);
                    m_BeaconsOnPath.Remove(pathName);
                }

                if (m_PathEndObjects.ContainsKey(pathName))
                {
                    NetworkServer.Destroy(m_PathEndObjects[pathName][0]);
                    NetworkServer.Destroy(m_PathEndObjects[pathName][1]);
                    m_PathEndObjects.Remove(pathName);

                    AssignPathObjectsToBeacons();
                }

                return "Removed path " + pathName;
            }

            if (tokens[3] != "location")
                return "Not a valid command segment: " + tokens[3];

            var index = int.Parse(tokens[4]);

            //Don't let users manipulate the end-points on the beacons
            if (index == 0 || index >= Paths[pathName].Count - 1)
                return "The intermediate point " + index + " does not exist on path " + pathName;

            Paths[pathName].RemoveAt(index);

            if (m_PathRenderers.ContainsKey(pathName))
            {
                var controller = m_PathRenderers[pathName];
                controller.ServerSetPoints(Paths[pathName]);
            }

            SavePaths(m_PathFile, Paths);

            return "Removed point " + index + " from path " + pathName;
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

        if (index == 0 || index == Paths[pathName].Count)
            return "Cannot insert at index " + index + " when it has " + (Paths[pathName].Count - 2) +
                   " intermediate elements";

        Paths[pathName].Insert(index, sender.transform.position);

        if (m_PathRenderers.ContainsKey(pathName))
        {
            var controller = m_PathRenderers[pathName];
            controller.ServerSetPoints(Paths[pathName]);
        }

        SavePaths(m_PathFile, Paths);

        return "Inserted your current position as point " + index + " on path " + pathName;
    }

    private void OnApplicationQuit()
    {
        if (!isServer) return;
        SavePaths(m_PathFile, Paths);
        SaveBeacons(m_BeaconFile);
    }

    private void SavePaths(FileSystemInfo file, Dictionary<string, List<Vector3>> paths)
    {
        var sr = new StringBuilder();
        foreach (var path in paths)
        {
            var beacons = m_BeaconsOnPath[path.Key];
            var startBeacon = beacons[0].GetComponent<BeaconController>().Name;
            var endBeacon = beacons[1].GetComponent<BeaconController>().Name;

            sr.AppendLine(path.Key);
            sr.AppendLine(startBeacon);

            for (var i = 1; i < path.Value.Count - 1; i++)
            {
                var pos = path.Value[i];
                sr.Append(pos.x + " ");
                sr.Append(pos.y + " ");
                sr.AppendLine(pos.z.ToString());
            }

            sr.AppendLine(endBeacon);

            sr.AppendLine();
        }

        File.WriteAllText(file.FullName, sr.ToString());
    }

    private void SaveBeacons(FileSystemInfo beaconFile)
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

    private Dictionary<string, BeaconController> LoadBeacons(FileSystemInfo beaconFile)
    {
        var beacons = new Dictionary<string, BeaconController>();

        using (var reader = new StreamReader(beaconFile.FullName))
        {
            string line;
            string beaconName = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;

                if (beaconName == null)
                {
                    beaconName = line;
                    continue;
                }

                var components = line.Split(' ');
                var x = float.Parse(components[0]);
                var y = float.Parse(components[1]);
                var z = float.Parse(components[2]);
                var pos = new Vector3(x, y, z);

                var beacon = (GameObject)Instantiate(BeaconPrefab, pos, Quaternion.identity);
                beacons.Add(beaconName, beacon.GetComponent<BeaconController>());
                beacon.GetComponent<BeaconController>().Name = beaconName;
                beacon.name = beaconName;

                beaconName = null;
            }
        }

        return beacons;
    }

    private Dictionary<string, List<Vector3>> LoadPaths(FileSystemInfo file)
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

                if (components.Length == 1)
                {
                    //Get the beacon by name, add it's location to the current path
                    var beacon = m_Beacons[components[0]];
                    paths[pathName].Add(beacon.transform.position);

                    if (!m_BeaconsOnPath.ContainsKey(pathName))
                    {
                        m_BeaconsOnPath[pathName] = new BeaconController[2];
                        m_BeaconsOnPath[pathName][0] = beacon;
                    }
                    else
                    {
                        m_BeaconsOnPath[pathName][1] = beacon;
                    }

                    beacon.GetComponent<BeaconController>().AddPath(pathName);
                }
                else
                {
                    var x = float.Parse(components[0]);
                    var y = float.Parse(components[1]);
                    var z = float.Parse(components[2]);
                    var pos = new Vector3(x, y, z);

                    paths[pathName].Add(pos);
                }
            }
        }

        return paths;
    }
}

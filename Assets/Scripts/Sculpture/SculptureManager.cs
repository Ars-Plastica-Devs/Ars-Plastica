using UnityEngine;
using UnityEngine.Networking;

public class SculptureManager : NetworkBehaviour, ICommandReceiver
{
    public static SculptureManager Instance;

    public GameObject AvoidCubesPrefab;
    public GameObject ShrinkCubesPrefab;
    public GameObject TransparentCubesPrefab;
    public GameObject PixelBoardPrefab;
    public GameObject TextureZoomScollPrefab;
    public GameObject PrimitiveSwarmPrefab;

    private void Start()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        CommandProcessor.PendingReceivers.Add(gameObject);
    }

    public bool IsCommandRelevant(string cmd, GameObject sender = null)
    {
        return cmd.StartsWith("sculpture");
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        if (tokens.Length == 2)
        {
            return DoSpawnSculptureCommand(tokens[1], sender);
        }

        return "Invalid Command";
    }

    public void SpawnSculpture(string sculptureName, Vector3 pos)
    {
        var prefabToSpawn = GetPrefabToSpawn(sculptureName);

        if (prefabToSpawn == null)
        {
            Debug.Log("Invalid Sculpture Name: " + sculptureName);
            return;
        }
        
        var sculp = Instantiate(prefabToSpawn, pos, Quaternion.identity);
        NetworkServer.Spawn(sculp);
    }

    private string DoSpawnSculptureCommand(string sculptureName, GameObject sender)
    {
        var prefabToSpawn = GetPrefabToSpawn(sculptureName);

        if (prefabToSpawn == null)
            return "Invalid Sculpture Name: " + sculptureName;

        var radius = prefabToSpawn.GetComponent<Sculpture>().GetBoundingSphereRadius();
        var pos = sender.transform.position + (sender.transform.forward * radius * 2f);

        var sculp = Instantiate(prefabToSpawn, pos, Quaternion.identity);
        NetworkServer.Spawn(sculp);

        return "";
    }

    private GameObject GetPrefabToSpawn(string sculptureName)
    {
        switch (sculptureName)
        {
            case "avoid-cubes":
                return AvoidCubesPrefab;
            case "shrink-cubes":
                return ShrinkCubesPrefab;
            case "transparent-cubes":
                return TransparentCubesPrefab;
            case "pixel-board":
                return PixelBoardPrefab;
            case "texture-zoom-scroll":
                return TextureZoomScollPrefab;
            case "primitive-swarm":
                return PrimitiveSwarmPrefab;
        }

        return null;
    }
}

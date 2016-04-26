using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class AIEcosystem : NetworkBehaviour, ICommandReceiver
{
    private readonly HashSet<GameObject> m_Herbivores = new HashSet<GameObject>();
    private readonly HashSet<GameObject> m_Carnivores = new HashSet<GameObject>();
    private readonly HashSet<GameObject> m_Nodules = new HashSet<GameObject>();
    private readonly HashSet<GameObject> m_Plants = new HashSet<GameObject>();

    [SyncVar] private int m_SyncedNoduleCount;
    [SyncVar] private int m_SyncedPlantCount;
    [SyncVar] private int m_SyncedHerbivoreCount;
    [SyncVar] private int m_SyncedCarnivoreCount;
    private const float SYNC_COUNTS_RATE = 1f;

    [SyncVar] public int InitialPlantCount = 400;
    [SyncVar] public int InitialHerbivoreCount = 300;
    [SyncVar] public int InitialCarnivoreCount = 100;

    [SyncVar] public int NoduleLimit = 800;
    [SyncVar] public int PlantLimit = 400;
    [SyncVar] public int HerbivoreLimit = 300;
    [SyncVar] public int CarnivoreLimit = 100;

    [SyncVar] public int HerbivoreMinimum = 50;
    [SyncVar] public int CarnivoreMinimum = 25;

    public float CreatureSpawnExtent = 1500f;

    public int CurrentNoduleCount
    {
        get
        {
            return isServer ? m_Nodules.Count : m_SyncedNoduleCount;
        }
    }

    public int CurrentPlantCount
    {
        get
        {
            return isServer ? m_Plants.Count : m_SyncedPlantCount;
        }
    }
    public int CurrentHerbivoreCount
    {
        get
        {
            return isServer ? m_Herbivores.Count : m_SyncedHerbivoreCount;
        }
    }
    public int CurrentCarnivoreCount
    {
        get
        {
            return isServer ? m_Carnivores.Count : m_SyncedCarnivoreCount;
        }
    }

    public Transform[] Plants;
    public Transform[] Herbivores;
    public Transform[] Carnivores;

    private void Start()
    {
        NetworkServer.SpawnObjects();
        if (!NetworkManager.singleton || !(DayClock) FindObjectOfType(typeof (DayClock)))
            return;
        if (!isServer)
            return;

        NetworkServer.Spawn(gameObject);

        HerbivoreMinimum = DataStore.GetInt("HerbivoreMinimum");
        CarnivoreMinimum = DataStore.GetInt("CarnivoreMinimum");

        InitialPlantCount = DataStore.GetInt("InitialPlantCount");
        InitialHerbivoreCount = DataStore.GetInt("InitialHerbivoreCount");
        InitialCarnivoreCount = DataStore.GetInt("InitialCarnivoreCount");

        NoduleLimit = DataStore.GetInt("NoduleLimit");
        PlantLimit = DataStore.GetInt("PlantLimit");
        HerbivoreLimit = DataStore.GetInt("HerbivoreLimit");
        CarnivoreLimit = DataStore.GetInt("CarnivoreLimit");

        CreatureSpawnExtent = DataStore.GetFloat("CreatureSpawnExtent");

        if (Plants.Length > 0)
        {
            for (var i = 0; i < InitialPlantCount; i++)
            {
                var pos = Random.insideUnitSphere * CreatureSpawnExtent;
                SpawnPlant(pos);
            }
        }

        if (Herbivores.Length > 0)
        {
            for (var i = 0; i < InitialHerbivoreCount; i++)
            {
                var pos = Random.insideUnitSphere * CreatureSpawnExtent;
                SpawnHerbivore(pos);
            }
        }

        if (Carnivores.Length > 0)
        {
            for (var i = 0; i < InitialCarnivoreCount; i++)
            {
                var pos = Random.insideUnitSphere * CreatureSpawnExtent;
                SpawnCarnivore(pos);
            }
        }

        InvokeRepeating("SyncCounts", SYNC_COUNTS_RATE, SYNC_COUNTS_RATE);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Invoke("RegisterAsReceiver", 1f);
        //RegisterAsReceiver();
    }

    private void RegisterAsReceiver()
    {
        GameObject.FindGameObjectsWithTag("Player")
            .First(cp => cp.GetComponent<NetworkIdentity>().isLocalPlayer)
            .GetComponent<CommandProcessor>()
            .RegisterReceiver(gameObject);
    }

    /// <summary>
    /// Sync counts across the network so that clients can display certain
    /// UI elements with the correct values
    /// </summary>
    private void SyncCounts()
    {
        m_SyncedNoduleCount = m_Nodules.Count;
        m_SyncedPlantCount = m_Plants.Count;
        m_SyncedHerbivoreCount = m_Herbivores.Count;
        m_SyncedCarnivoreCount = m_Carnivores.Count;
    }

    public bool CanAddNodule()
    {
        return CurrentNoduleCount < NoduleLimit;
    }

    public bool AddNodule(GameObject nod)
    {
        if (!CanAddNodule())
            return false;

        m_Nodules.Add(nod);
        return true;
    }


    public bool RemoveNodule(GameObject nod)
    {
        if (m_Nodules.Count == 0)
            return false;

        m_Nodules.Remove(nod);
        return true;
    }

    public bool CanAddHerbivore()
    {
        return CurrentHerbivoreCount < HerbivoreLimit;
    }

    public bool CanAddCarnivore()
    {
        return CurrentCarnivoreCount < CarnivoreLimit;
    }

    public bool CanAddPlant()
    {
        return CurrentPlantCount < PlantLimit;
    }

    public void SpawnPlant(Vector3 pos)
    {
        var plant = (Transform) Instantiate(Plants[0], pos, Quaternion.identity);
        plant.GetComponent<PlantAI>().Ecosystem = this;
        plant.SetParent(transform);
        NetworkServer.Spawn(plant.gameObject);
        m_Plants.Add(plant.gameObject);
    }

    public void SpawnHerbivore(Vector3 pos)
    {
        var herb = (Transform) Instantiate(Herbivores[0], pos, Quaternion.identity);
        herb.GetComponent<HerbivoreAI>().Ecosystem = this;
        herb.SetParent(transform);
        NetworkServer.Spawn(herb.gameObject);
        m_Herbivores.Add(herb.gameObject);
    }

    public void SpawnCarnivore(Vector3 pos)
    {
        var carn = (Transform) Instantiate(Carnivores[0], pos, Quaternion.identity);
        carn.GetComponent<CarnivoreAI>().Ecosystem = this;
        carn.SetParent(transform);
        NetworkServer.Spawn(carn.gameObject);
        m_Carnivores.Add(carn.gameObject);
    }

    public void KillPlant(GameObject plant)
    {
        m_Plants.Remove(plant);
        NetworkServer.Destroy(plant);
    }

    public void KillHerbivore(GameObject herb)
    {
        m_Herbivores.Remove(herb);
        NetworkServer.Destroy(herb);

        if (CurrentHerbivoreCount < HerbivoreMinimum)
        {
            OnHerbivoreExtinction();
        }
    }

    public void KillCarnivore(GameObject carn)
    {
        m_Carnivores.Remove(carn);
        NetworkServer.Destroy(carn);

        if (CurrentCarnivoreCount < CarnivoreMinimum)
        {
            OnCarnivoreExtinction();
        }
    }

    private void SetHerbivoreMin(int min)
    {
        HerbivoreMinimum = min;
        DataStore.Set("HerbivoreMinimum", HerbivoreMinimum);

        if (CurrentHerbivoreCount < HerbivoreMinimum)
        {
            OnHerbivoreExtinction();
        }
    }

    private void SetCarnivoreMin(int min)
    {
        CarnivoreMinimum = min;
        DataStore.Set("CarnivoreMinimum", CarnivoreMinimum);

        if (CurrentCarnivoreCount < CarnivoreMinimum)
        {
            OnCarnivoreExtinction();
        }
    }

    private void SetPlantCount(int count)
    {
        while (count < CurrentPlantCount)
        {
            KillPlant(m_Plants.First());
        }

        while (count > CurrentPlantCount)
        {
            SpawnPlant(Random.insideUnitSphere * CreatureSpawnExtent);
        }
    }

    private void SetHerbivoreCount(int count)
    {
        while (count < CurrentHerbivoreCount)
        {
            KillHerbivore(m_Herbivores.First());
        }

        while (count > CurrentHerbivoreCount)
        {
            SpawnHerbivore(Random.insideUnitSphere * CreatureSpawnExtent);
        }
    }

    private void SetCarnivoreCount(int count)
    {
        while (count < CurrentCarnivoreCount)
        {
            KillCarnivore(m_Carnivores.First());
        }

        while (count > CurrentCarnivoreCount)
        {
            SpawnCarnivore(Random.insideUnitSphere * CreatureSpawnExtent);
        }
    }

    private void SetNoduleLimit(int limit)
    {
        NoduleLimit = limit;
        DataStore.Set("NoduleLimit", NoduleLimit);

        while (m_Nodules.Count > NoduleLimit)
        {
            RemoveNodule(m_Carnivores.First());
        }
    }

    private void SetPlantLimit(int limit)
    {
        PlantLimit = limit;
        DataStore.Set("PlantLimit", PlantLimit);

        while (m_Plants.Count > PlantLimit)
        {
            KillPlant(m_Plants.First());
        }
    }

    private void SetHerbivoreLimit(int limit)
    {
        HerbivoreLimit = limit;
        DataStore.Set("HerbivoreLimit", HerbivoreLimit);

        while (m_Herbivores.Count > HerbivoreLimit)
        {
            KillHerbivore(m_Herbivores.First());
        }
    }

    private void SetCarnivoreLimit(int limit)
    {
        CarnivoreLimit = limit;
        DataStore.Set("CarnivoreLimit", CarnivoreLimit);

        while (m_Carnivores.Count > CarnivoreLimit)
        {
            KillCarnivore(m_Carnivores.First());
        }
    }

    private void OnHerbivoreExtinction()
    {
        while (m_Herbivores.Count < HerbivoreMinimum)
        {
            var pos = Random.insideUnitSphere * CreatureSpawnExtent;
            SpawnHerbivore(pos);
        }
    }

    private void OnCarnivoreExtinction()
    {
        while (m_Carnivores.Count < CarnivoreMinimum)
        {
            var pos = Random.insideUnitSphere * CreatureSpawnExtent;
            SpawnCarnivore(pos);
        }
    }

    public bool IsCommandRelevant(string cmd)
    {
        return cmd.StartsWith("world") || cmd.StartsWith("spawn");
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        try
        {
            if (tokens[0] == "world")
            {
                switch (tokens[1])
                {
                    case "predation":
                        switch (tokens[2])
                        {
                            case "on":
                                Animal.PredationAllowed = true;
                                return "Predation turned on";
                            case "off":
                                Animal.PredationAllowed = false;
                                return "Predation turned off";
                        }
                        return "Not a valid command segment: " + tokens[2];
                    case "reproduction":
                        switch (tokens[2])
                        {
                            case "on":
                                Animal.ReproductionAllowed = true;
                                return "Reproduction turned on";
                            case "off":
                                Animal.ReproductionAllowed = false;
                                return "Reproduction turned off";
                        }
                        return "Not a valid command segment: " + tokens[2];
                    case "limit":
                        var limit = int.Parse(tokens[3]);
                        switch (tokens[2])
                        {
                            case "plants":
                                SetPlantLimit(limit);
                                return "Set plant limit to " + limit;
                            case "herbivores":
                                SetHerbivoreLimit(limit);
                                return "Set herbivore limit to " + limit;
                            case "carnivores":
                                SetCarnivoreLimit(limit);
                                return "Set carnivore limit to " + limit;
                            case "nodules":
                                SetNoduleLimit(limit);
                                return "Set nodule limit to " + limit;
                        }
                        return "Not a valid command segment: " + tokens[2];
                    case "minimum":
                        var min = int.Parse(tokens[3]);
                        switch (tokens[2])
                        {
                            case "herbivores":
                                SetHerbivoreMin(min);
                                return "Set herbivore minimum to " + min;
                            case "carnivores":
                                SetCarnivoreMin(min);
                                return "Set carnivore minimum to " + min;
                        }
                        return "Not a valid command segment: " + tokens[2];
                    case "set-count":
                        var count = int.Parse(tokens[3]);
                        switch (tokens[2])
                        {
                            case "plants":
                                SetPlantCount(count);
                                return "Set plant count to " + count;
                            case "herbivores":
                                SetHerbivoreCount(count);
                                return "Set herbivore count to " + count;
                            case "carnivores":
                                SetCarnivoreCount(count);
                                return "Set carnivore count to " + count;
                        }
                        return "Not a valid command segment: " + tokens[2];
                    default:
                        return "Not a valid command segment: " + tokens[1];
                }
            }
            if (tokens[0] == "spawn")
            {
                switch (tokens[1])
                {
                    case "plant":
                        SpawnPlant(sender.transform.position + sender.transform.forward * 5);
                        return "Spawned a plant";
                    case "herbivore":
                        SpawnHerbivore(sender.transform.position + sender.transform.forward * 5);
                        return "Spawned a herbivore";
                    case "carnivore":
                        SpawnCarnivore(sender.transform.position + sender.transform.forward * 5);
                        return "Spawned a carnivore";
                    default:
                        return "Not a valid command segment: " + tokens[1];
                }
            }
        }
        catch (Exception e)
        {
            return e.Message;
        }

        return "Not a valid command";
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        if (HerbivoreMinimum != DataStore.GetInt("HerbivoreMinimum"))
        {
            DataStore.Set("HerbivoreMinimum", HerbivoreMinimum);
        }
        if (CarnivoreMinimum != DataStore.GetInt("CarnivoreMinimum"))
        {
            DataStore.Set("CarnivoreMinimum", CarnivoreMinimum);
        }
        if (InitialPlantCount != DataStore.GetInt("InitialPlantCount"))
        {
            DataStore.Set("InitialPlantCount", InitialPlantCount);
        }
        if (InitialHerbivoreCount != DataStore.GetInt("InitialHerbivoreCount"))
        {
            DataStore.Set("InitialHerbivoreCount", InitialHerbivoreCount);
        }
        if (InitialCarnivoreCount != DataStore.GetInt("InitialCarnivoreCount"))
        {
            DataStore.Set("InitialCarnivoreCount", InitialCarnivoreCount);
        }
        if (NoduleLimit != DataStore.GetInt("NoduleLimit"))
        {
            DataStore.Set("NoduleLimit", NoduleLimit);
        }
        if (PlantLimit != DataStore.GetInt("PlantLimit"))
        {
            DataStore.Set("PlantLimit", PlantLimit);
        }
        if (HerbivoreLimit != DataStore.GetInt("HerbivoreLimit"))
        {
            DataStore.Set("HerbivoreLimit", HerbivoreLimit);
        }
        if (CarnivoreLimit != DataStore.GetInt("CarnivoreLimit"))
        {
            DataStore.Set("CarnivoreLimit", InitialCarnivoreCount);
        }
        if (CreatureSpawnExtent != DataStore.GetFloat("CreatureSpawnExtent"))
        {
            DataStore.Set("CreatureSpawnExtent", CreatureSpawnExtent);
        }
    }
}


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

    public int MaxNumberPlantsToSpawn = 70;
    public int MaxNumberHerbivoresToSpawn = 40;
    public int MaxNumberCarnivoresToSpawn = 20;

    public int NoduleLimit = 200;
    public int PlantLimit = 100;
    public int HerbivoreLimit = 50;
    public int CarnivoreLimit = 25;

    public int HerbivoresToSpawnOnExtinction = 8;
    public int CarnivoresToSpawnOnExtinction = 3;

    public float CreatureSpawnExtent = 2500f;

    [SerializeField] private int m_TotalNodules;
    [SerializeField] private int m_TotalPlants;
    [SerializeField] private int m_TotalHerbivores;
    [SerializeField] private int m_TotalCarnivores;

    public Transform[] Plants;
    public Transform[] Herbivores;
    public Transform[] Carnivores;

    // Use this for initialization
    private void Start()
    {
        NetworkServer.SpawnObjects();
        if (!NetworkManager.singleton || !(DayClock) FindObjectOfType(typeof (DayClock)))
            return;
        if (!isServer)
            return;

        NetworkServer.Spawn(gameObject);

        if (Plants.Length > 0)
        {
            for (var i = 0; i < MaxNumberPlantsToSpawn; i++)
            {
                var pos = Random.insideUnitSphere * CreatureSpawnExtent;
                SpawnPlant(pos);
            }
        }

        if (Herbivores.Length > 0)
        {
            for (var i = 0; i < MaxNumberHerbivoresToSpawn; i++)
            {
                var pos = Random.insideUnitSphere * CreatureSpawnExtent;
                SpawnHerbivore(pos);
            }
        }

        if (Carnivores.Length > 0)
        {
            for (var i = 0; i < MaxNumberCarnivoresToSpawn; i++)
            {
                var pos = Random.insideUnitSphere * CreatureSpawnExtent;
                SpawnCarnivore(pos);
            }
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
        GameObject.FindGameObjectsWithTag("Player")
            .First(cp => cp.GetComponent<NetworkIdentity>().isLocalPlayer)
            .GetComponent<CommandProcessor>()
            .RegisterReceiver(gameObject);
    }

    public bool CanAddNodule()
    {
        return m_TotalNodules < NoduleLimit;
    }

    public bool AddNodule(GameObject nod)
    {
        m_Nodules.Add(nod);

        if (m_TotalNodules + 1 > NoduleLimit)
            return false;

        m_TotalNodules++;
        return true;
    }


    public bool RemoveNodule(GameObject nod)
    {
        m_Nodules.Remove(nod);

        if (m_TotalNodules - 1 < 0)
            return false;

        m_TotalNodules--;
        return true;
    }

    public bool CanAddHerbivore()
    {
        return m_TotalHerbivores < HerbivoreLimit;
    }

    public bool CanAddCarnivore()
    {
        return m_TotalCarnivores < CarnivoreLimit;
    }

    public bool CanAddPlant()
    {
        return m_TotalPlants < PlantLimit;
    }

    public void SpawnPlant(Vector3 pos)
    {
        var plant = (Transform) Instantiate(Plants[0], pos, Quaternion.identity);
        plant.GetComponent<PlantAI>().Ecosystem = this;
        plant.SetParent(transform);
        NetworkServer.Spawn(plant.gameObject);
        m_Plants.Add(plant.gameObject);
        m_TotalPlants++;
    }

    public void SpawnHerbivore(Vector3 pos)
    {
        var herb = (Transform) Instantiate(Herbivores[0], pos, Quaternion.identity);
        herb.GetComponent<HerbivoreAI>().Ecosystem = this;
        herb.SetParent(transform);
        NetworkServer.Spawn(herb.gameObject);
        m_Herbivores.Add(herb.gameObject);
        m_TotalHerbivores++;
    }

    public void SpawnCarnivore(Vector3 pos)
    {
        var carn = (Transform) Instantiate(Carnivores[0], pos, Quaternion.identity);
        carn.GetComponent<CarnivoreAI>().Ecosystem = this;
        carn.SetParent(transform);
        NetworkServer.Spawn(carn.gameObject);
        m_Carnivores.Add(carn.gameObject);
        m_TotalCarnivores++;
    }

    public void KillPlant(GameObject plant)
    {
        m_TotalPlants--;

        m_Plants.Remove(plant);
        NetworkServer.Destroy(plant);

        if (m_TotalPlants <= 0)
        {
            m_TotalPlants = 0;
        }
    }

    public void KillHerbivore(GameObject herb)
    {
        m_TotalHerbivores--;

        m_Herbivores.Remove(herb);
        NetworkServer.Destroy(herb);

        if (m_TotalHerbivores <= 0)
        {
            m_TotalHerbivores = 0;
            OnHerbivoreExtinction();
        }
    }

    public void KillCarnivore(GameObject carn)
    {
        m_TotalCarnivores--;

        m_Carnivores.Remove(carn);
        NetworkServer.Destroy(carn);

        if (m_TotalCarnivores <= 0)
        {
            m_TotalCarnivores = 0;
            OnCarnivoreExtinction();
        }
    }

    private void SetPlantLimit(int limit)
    {
        PlantLimit = limit;

        while (m_Plants.Count > PlantLimit)
        {
            KillPlant(m_Plants.First());
        }
    }

    private void SetHerbivoreLimit(int limit)
    {
        HerbivoreLimit = limit;

        while (m_Herbivores.Count > HerbivoreLimit)
        {
            KillHerbivore(m_Herbivores.First());
        }
    }

    private void SetCarnivoreLimit(int limit)
    {
        CarnivoreLimit = limit;

        while (m_Carnivores.Count > CarnivoreLimit)
        {
            KillCarnivore(m_Carnivores.First());
        }
    }

    private void SetNoduleLimit(int limit)
    {
        NoduleLimit = limit;

        while (m_Nodules.Count > NoduleLimit)
        {
            RemoveNodule(m_Carnivores.First());
        }
    }

    private void OnHerbivoreExtinction()
    {
        for (var i = 0; i < HerbivoresToSpawnOnExtinction; i++)
        {
            var pos = Random.insideUnitSphere * CreatureSpawnExtent;
            SpawnHerbivore(pos);
        }
    }

    private void OnCarnivoreExtinction()
    {
        for (var i = 0; i < CarnivoresToSpawnOnExtinction; i++)
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
}


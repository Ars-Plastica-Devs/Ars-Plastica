using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Octree;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

//TODO: Extract some responsibility from this class - it's doing a lot of things right now
public class Ecosystem : NetworkBehaviour, ICommandReceiver
{
    private static readonly Dictionary<string, Enum> Spawnables = new Dictionary<string, Enum>();
    private static readonly Dictionary<string, SpawnablePlacer> SpawnablePlacers = new Dictionary<string, SpawnablePlacer>();

    private class DefaultSpawnablePlacer : SpawnablePlacer
    {
        public override Vector3 GetSpawnPosition(GameObject sender)
        {
            return sender.transform.position + sender.transform.forward * 5;
        }

        public override Quaternion GetSpawnRotation(GameObject sender)
        {
            return Quaternion.identity;
        }
    }


    static Ecosystem()
    {
        var spawnableCreatureAtts = ReflectionHelper.GetAllInstancesOfAttribute<SpawnableCreatureAttribute>(false);

        //We give every spawnable a default placer so that
        //we never have to check if it has one or not at runtime
        var defaultPlacer = new DefaultSpawnablePlacer();

        foreach (var att in spawnableCreatureAtts)
        {
            Spawnables.Add(att.Name, att.Type);
            SpawnablePlacers[att.Name] = (att.PlacerType != null)
                ? (SpawnablePlacer) Activator.CreateInstance(att.PlacerType)
                : defaultPlacer;
        }
    }

    public static Ecosystem Singleton;

    private readonly Dictionary<HerbivoreType, HashSet<HerbivoreBase>> m_Herbivores = new Dictionary<HerbivoreType, HashSet<HerbivoreBase>>();
    private readonly Dictionary<CarnivoreType, HashSet<CarnivoreBase>> m_Carnivores = new Dictionary<CarnivoreType, HashSet<CarnivoreBase>>();
    private readonly Dictionary<PlantType, HashSet<PlantBase>> m_PlantMap = new Dictionary<PlantType, HashSet<PlantBase>>();

    private readonly NoduleSet m_Nodules = new NoduleSet();

    [SyncVar]
    private int m_SyncedNoduleCount;
    [SyncVar]
    private int m_SyncedPlantCount;
    [SyncVar]
    private int m_SyncedHerbivoreCount;
    [SyncVar]
    private int m_SyncedCarnivoreCount;
    private const float SYNC_COUNTS_RATE = 1f;

    [SyncVar]
    public int InitialFGLColonycount = 3;
    [SyncVar]
    public int InitialHerbivoreCount = 300;
    [SyncVar]
    public int InitialCarnivoreCount = 100;

    [SyncVar]
    public int NoduleLimit = 800;
    [SyncVar]
    public int HerbivoreLimit = 300;
    [SyncVar]
    public int CarnivoreLimit = 100;

    [SyncVar]
    public int HerbivoreMinimum = 50;
    [SyncVar]
    public int CarnivoreMinimum = 25;

    [SyncVar]
    public float CreatureSpawnExtent = 1500f;
    [SyncVar]
    public float WorldExtent = 2500f;

    [SyncVar]
    public float PlantSpawnExtentBuffer = 500f;

    public Dictionary<Data, string> DataMap = new Dictionary<Data, string>();

    public delegate void DataChangeDelegate(Data key, string value);
    public static event DataChangeDelegate OnDataChangeClientside;

    public delegate void PredationEventDelegate(Creature animalKilled);
    public event PredationEventDelegate OnPredationEvent;

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
            return isServer ? m_PlantMap.Sum(kvp => kvp.Value.Count) : m_SyncedPlantCount;
        }
    }
    public int CurrentHerbivoreCount
    {
        get
        {
            return isServer ? m_Herbivores.Sum(kvp => kvp.Value.Count) : m_SyncedHerbivoreCount;
        }
    }
    public int CurrentCarnivoreCount
    {
        get
        {
            return isServer ? m_Carnivores.Sum(kvp => kvp.Value.Count) : m_SyncedCarnivoreCount;
        }
    }

    private void Awake()
    {
        if (Singleton == null)
            Singleton = this;
    }

    private void Start()
    {
        NetworkServer.SpawnObjects();
        if (!NetworkManager.singleton || !(DayClock)FindObjectOfType(typeof(DayClock)))
            return;
        if (!isServer)
        {
            return;
        }

        Player_ID.OnPlayerSetupComplete += OnPlayerJoined;
        NetworkServer.Spawn(gameObject);

        m_Nodules.SpawnNoduleController = (p, r, t) =>
        {
            var nod = NoduleFactory.InstantiateNodule(p, r, t);
            NetworkServer.Spawn(nod.gameObject);
            return nod;
        };

        DataStore.OnDataChange += OnDataChange;

        var ssDataMap = DataStore.GetAllData();

        DataMap =
            ssDataMap.Select(kvp => new KeyValuePair<Data, string>((Data)Enum.Parse(typeof(Data), kvp.Key), kvp.Value))
                .ToDictionary(x => x.Key, x => x.Value);

        HerbivoreMinimum = DataStore.GetInt(Data.EcoHerbivoreMinimum);
        CarnivoreMinimum = DataStore.GetInt(Data.EcoCarnivoreMinimum);

        InitialFGLColonycount = DataStore.GetInt(Data.EcoInitialFGLColonyCount);
        InitialHerbivoreCount = DataStore.GetInt(Data.EcoInitialHerbivoreCount);
        InitialCarnivoreCount = DataStore.GetInt(Data.EcoInitialCarnivoreCount);

        NoduleLimit = DataStore.GetInt(Data.EcoNoduleLimit);
        HerbivoreLimit = DataStore.GetInt(Data.EcoHerbivoreLimit);
        CarnivoreLimit = DataStore.GetInt(Data.EcoCarnivoreLimit);

        CreatureSpawnExtent = DataStore.GetFloat(Data.EcoCreatureSpawnExtent);
        WorldExtent = DataStore.GetFloat(Data.EcoWorldExtent);
        PlantSpawnExtentBuffer = DataStore.GetFloat(Data.EcoPlantSpawnExtentBuffer);

        Creature.PredationAllowed = DataStore.Get(Data.EcoPredation) == "on";

        DiscoverChildren();

        for (var i = 0; i < InitialFGLColonycount; i++)
        {
            var pos = Random.insideUnitSphere * (WorldExtent - PlantSpawnExtentBuffer);
            Debug.Log("Note that plant spawning is deactivated");
            //SpawnPlant(pos, Quaternion.identity, PlantType.FloatGrassLargeColony);
        }

        for (var i = 0; i < InitialHerbivoreCount; i++)
        {
            var pos = Random.insideUnitSphere * CreatureSpawnExtent;
            SpawnHerbivore(pos, Quaternion.identity, HerbivoreFactory.GetRandomSpawnableIndividualType());
        }

        for (var i = 0; i < InitialCarnivoreCount; i++)
        {
            var pos = Random.insideUnitSphere * CreatureSpawnExtent;
            SpawnCarnivore(pos, Quaternion.identity, EnumHelper.GetRandomEnum<CarnivoreType>());
        }

        InvokeRepeating("SyncCounts", SYNC_COUNTS_RATE, SYNC_COUNTS_RATE);
    }

    private void OnDestroy()
    {
        Player_ID.OnPlayerSetupComplete -= OnPlayerJoined;
    }

    /*private void InitializeClientSidePlayerOctree()
    {
        OctreeManager.AddOctree(OctreeType.Player, new Octree(new Bounds(Vector3.zero, new Vector3(WorldExtent, WorldExtent, WorldExtent) * 2f)));
    }*/

    public override void OnStartClient()
    {
        base.OnStartClient();
        CommandProcessor.PendingReceivers.Add(gameObject);
    }

    /// <summary>
    /// Adds all child plants to the relevant lists
    /// </summary>
    private void DiscoverChildren()
    {
        var childrenPlants = transform.GetComponentsInChildren<PlantBase>();

        foreach (var plant in childrenPlants)
        {
            var t = plant.Type;
            if (!m_PlantMap.ContainsKey(t))
                m_PlantMap[t] = new HashSet<PlantBase>();

            m_PlantMap[t].Add(plant);
        }
    }

    /// <summary>
    /// Sync counts across the network so that clients can display certain
    /// UI elements with the correct values
    /// </summary>
    [Server]
    private void SyncCounts()
    {
        m_SyncedNoduleCount = m_Nodules.Count;
        m_SyncedPlantCount = CurrentPlantCount;
        m_SyncedHerbivoreCount = CurrentHerbivoreCount;
        m_SyncedCarnivoreCount = CurrentCarnivoreCount;
    }

    public bool CanAddNodule()
    {
        return CurrentNoduleCount < NoduleLimit;
    }

    public void RemoveNodule(GameObject nod)
    {
        RemoveNodule(nod.GetComponent<Nodule>());
    }

    public void RemoveNodule(Nodule con)
    {
        if (m_Nodules.Count != 0 && con != null)
        {
            m_Nodules.DisableNodule(con);
            OctreeManager.Get(OctreeType.Nodule).Remove(con.transform);
        }
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
        return true;
    }

    public Nodule SpawnNodule(Vector3 pos, Quaternion rot, NoduleType type)
    {
        var nod = m_Nodules.SpawnNodule(pos, rot, type);
        OctreeManager.Get(OctreeType.Nodule).Add(nod.transform);
        return nod;
    }

    public GameObject SpawnPlant(Vector3 pos, Quaternion rot, PlantType type)
    {
        var plant = PlantFactory.InstantiatePlant(pos, rot, type);
        plant.transform.SetParent(transform);
        NetworkServer.Spawn(plant.gameObject);

        if (!m_PlantMap.ContainsKey(plant.Type))
            m_PlantMap.Add(plant.Type, new HashSet<PlantBase>());

        m_PlantMap[plant.Type].Add(plant);

        return plant.gameObject;
    }

    public GameObject SpawnHerbivore(Vector3 pos, Quaternion rot, HerbivoreType type)
    {
        var herb = HerbivoreFactory.InstantiateHerbivore(pos, rot, type);
        herb.transform.SetParent(transform);
        NetworkServer.Spawn(herb.gameObject);

        if (!m_Herbivores.ContainsKey(type))
            m_Herbivores.Add(type, new HashSet<HerbivoreBase>());

        m_Herbivores[type].Add(herb);
        OctreeManager.Get(OctreeType.Herbivore).Add(herb.transform);
        return herb.gameObject;
    }

    public GameObject SpawnCarnivore(Vector3 pos, Quaternion rot, CarnivoreType type)
    {
        var carn = CarnivoreFactory.InstantiateCarnivore(pos, rot, type);
        carn.transform.SetParent(transform);
        NetworkServer.Spawn(carn.gameObject);

        if (!m_Carnivores.ContainsKey(type))
            m_Carnivores.Add(type, new HashSet<CarnivoreBase>());

        m_Carnivores[type].Add(carn);
        OctreeManager.Get(OctreeType.Carnivore).Add(carn.transform);
        return carn.gameObject;
    }

    public void KillPlant(PlantBase plant, bool predation = false)
    {
        if (plant == null || plant.gameObject == null)
            return;

        if (m_PlantMap.ContainsKey(plant.Type))
            m_PlantMap[plant.Type].Remove(plant);

        if (predation && OnPredationEvent != null)
            OnPredationEvent(plant);

        NetworkServer.Destroy(plant.gameObject);
    }

    public void KillHerbivore(HerbivoreBase herb, bool predation = false)
    {
        if (herb == null || herb.gameObject == null)
            return;

        if (m_Herbivores.ContainsKey(herb.Type))
            m_Herbivores[herb.Type].Remove(herb);

        OctreeManager.Get(OctreeType.Herbivore).Remove(herb.transform);

        if (predation && OnPredationEvent != null)
            OnPredationEvent(herb);

        NetworkServer.Destroy(herb.gameObject);

        ValidateHerbivoreCount();
    }

    public void KillCarnivore(CarnivoreBase carn, bool predation = false)
    {
        if (carn == null || carn.gameObject == null)
            return;

        if (m_Carnivores.ContainsKey(carn.Type))
            m_Carnivores[carn.Type].Remove(carn);

        OctreeManager.Get(OctreeType.Carnivore).Remove(carn.transform);

        if (predation && OnPredationEvent != null)
            OnPredationEvent(carn);

        NetworkServer.Destroy(carn.gameObject);

        ValidateCarnivoreCount();
    }

    private void SetHerbivoreMin(int min)
    {
        if (min > HerbivoreLimit)
            min = HerbivoreLimit;

        HerbivoreMinimum = min;
        DataStore.Set(Data.EcoHerbivoreMinimum, HerbivoreMinimum);

        ValidateHerbivoreCount();
    }

    private void SetCarnivoreMin(int min)
    {
        if (min > CarnivoreLimit)
            min = CarnivoreLimit;

        CarnivoreMinimum = min;
        DataStore.Set(Data.EcoCarnivoreMinimum, CarnivoreMinimum);

        ValidateCarnivoreCount();
    }

    private void SetPlantCount(int count)
    {
        while (count > CurrentPlantCount)
        {
            var t = PlantFactory.GetRandomSpawnableIndividualType();

            SpawnPlant(Random.insideUnitSphere * CreatureSpawnExtent, Quaternion.identity, t);
            Debug.Log("Spawned a " + t);
        }

        while (count < CurrentPlantCount && m_PlantMap.Count > 0)
        {
            var t = EnumHelper.GetRandomEnum<PlantType>();
            if (m_PlantMap.ContainsKey(t))
                KillPlant(m_PlantMap[t].FirstOrDefault());

            foreach (var kvp in m_PlantMap)
            {
                kvp.Value.RemoveWhere(p => p == null || p.gameObject == null);
            }
        }
    }

    private void SetHerbivoreCount(int count)
    {
        while (count < CurrentHerbivoreCount)
        {
            var t = EnumHelper.GetRandomEnum<HerbivoreType>();
            if (m_Herbivores.ContainsKey(t))
                KillHerbivore(m_Herbivores[t].FirstOrDefault());

            foreach (var kvp in m_Herbivores)
            {
                kvp.Value.RemoveWhere(h => h == null || h.gameObject == null);
            }
        }

        while (count > CurrentHerbivoreCount)
        {
            SpawnHerbivore(Random.insideUnitSphere * CreatureSpawnExtent, Quaternion.identity, 
                            EnumHelper.GetRandomEnum<HerbivoreType>());
        }
    }

    private void SetCarnivoreCount(int count)
    {
        while (count < CurrentCarnivoreCount)
        {
            var t = EnumHelper.GetRandomEnum<CarnivoreType>();
            if (m_Carnivores.ContainsKey(t))
                KillCarnivore(m_Carnivores[t].FirstOrDefault());

            foreach (var kvp in m_Carnivores)
            {
                kvp.Value.RemoveWhere(c => c == null || c.gameObject == null);
            }
        }

        while (count > CurrentCarnivoreCount)
        {
            SpawnCarnivore(Random.insideUnitSphere * CreatureSpawnExtent, Quaternion.identity,
                            EnumHelper.GetRandomEnum<CarnivoreType>());
        }
    }

    private void SetNoduleLimit(int limit)
    {
        NoduleLimit = limit;
        DataStore.Set(Data.EcoNoduleLimit, NoduleLimit);

        while (m_Nodules.Count > NoduleLimit)
        {
            var floatNod = m_Nodules.FirstOrDefault(n => n.Type == NoduleType.Floating);
            if (floatNod == null)
                break;
            RemoveNodule(floatNod);
        }
    }

    private void SetHerbivoreLimit(int limit)
    {
        if (limit < HerbivoreMinimum)
            limit = HerbivoreMinimum;

        HerbivoreLimit = limit;
        DataStore.Set(Data.EcoHerbivoreLimit, HerbivoreLimit);

        ValidateHerbivoreCount();
    }

    private void SetCarnivoreLimit(int limit)
    {
        if (limit < CarnivoreMinimum)
            limit = CarnivoreMinimum;

        CarnivoreLimit = limit;
        DataStore.Set(Data.EcoCarnivoreLimit, CarnivoreLimit);

        ValidateCarnivoreCount();
    }

    private void ValidateHerbivoreCount()
    {
        Debug.Assert(HerbivoreMinimum <= HerbivoreLimit);

        while (CurrentHerbivoreCount > HerbivoreLimit)
        {
            KillHerbivore(m_Herbivores[EnumHelper.GetRandomEnum<HerbivoreType>()].First());
        }
        while (CurrentHerbivoreCount < HerbivoreMinimum)
        {
            var pos = Random.insideUnitSphere * CreatureSpawnExtent;
            SpawnHerbivore(pos, Quaternion.identity, HerbivoreFactory.GetRandomSpawnableIndividualType());
        }
    }

    private void ValidateCarnivoreCount()
    {
        Debug.Assert(CarnivoreMinimum <= CarnivoreLimit);

        while (CurrentCarnivoreCount > CarnivoreLimit)
        {
            KillCarnivore(m_Carnivores[EnumHelper.GetRandomEnum<CarnivoreType>()].First());
        }
        while (CurrentCarnivoreCount < CarnivoreMinimum)
        {
            var pos = Random.insideUnitSphere * CreatureSpawnExtent;
            SpawnCarnivore(pos, Quaternion.identity, EnumHelper.GetRandomEnum<CarnivoreType>());
        }
    }

    public bool IsCommandRelevant(string cmd, GameObject sender = null)
    {
        return cmd.StartsWith("world") || cmd.StartsWith("spawn") || cmd.StartsWith("kill") || cmd.StartsWith("exterminate");
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        if (!isServer) return "";

        var tokens = cmd.Split(' ');

        //try
        {
            if (tokens[0] == "world")
            {
                switch (tokens[1])
                {
                    case "reproduction":
                        switch (tokens[2])
                        {
                            case "on":
                                Creature.ReproductionAllowed = true;
                                return "Reproduction turned on";
                            case "off":
                                Creature.ReproductionAllowed = false;
                                return "Reproduction turned off";
                        }
                        return "Not a valid command segment: " + tokens[2];
                    case "limit":
                        var limit = int.Parse(tokens[3]);
                        switch (tokens[2])
                        {
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
                Enum spawnAbleEnumType;
                if (Spawnables.TryGetValue(tokens[1], out spawnAbleEnumType))
                {
                    if (spawnAbleEnumType.GetType() == typeof (PlantType))
                    {
                        if (!CanAddPlant()) return "Plant max reached - cannot spawn more";
                        SpawnPlant(SpawnablePlacers[tokens[1]].GetSpawnPosition(sender), 
                                    SpawnablePlacers[tokens[1]].GetSpawnRotation(sender), 
                                    (PlantType)spawnAbleEnumType);
                        return "Spawned a " + tokens[1];
                    }
                    if (spawnAbleEnumType.GetType() == typeof(HerbivoreType))
                    {
                        if (!CanAddHerbivore()) return "Herbivore max reached - cannot spawn more";
                        SpawnHerbivore(SpawnablePlacers[tokens[1]].GetSpawnPosition(sender), 
                                    SpawnablePlacers[tokens[1]].GetSpawnRotation(sender), 
                                    (HerbivoreType)spawnAbleEnumType);
                        return "Spawned a " + tokens[1];
                    }
                    if (spawnAbleEnumType.GetType() == typeof(CarnivoreType))
                    {
                        if (!CanAddCarnivore()) return "Carnivore max reached - cannot spawn more";
                        SpawnCarnivore(SpawnablePlacers[tokens[1]].GetSpawnPosition(sender), 
                                    SpawnablePlacers[tokens[1]].GetSpawnRotation(sender), 
                                    (CarnivoreType)spawnAbleEnumType);
                        return "Spawned a " + tokens[1];
                    }
                }
                return "Not a valid command segment: " + tokens[1];
            }
            else if (tokens[0] == "kill")
            {
                switch (tokens[1])
                {
                    case "floatgs-colony":
                        if (!m_PlantMap.ContainsKey(PlantType.FloatGrassSmallColony))
                            return "No Float Grass Small colonies exist";
                        KillPlant(m_PlantMap[PlantType.FloatGrassSmallColony].First());
                        return "Killed a Float Grass Small colony";
                    case "floatgl-colony":
                        if (!m_PlantMap.ContainsKey(PlantType.FloatGrassLargeColony))
                            return "No Float Grass Large colonies exist";
                        KillPlant(m_PlantMap[PlantType.FloatGrassLargeColony].First());
                        return "Killed a Float Grass Large colony";
                }
            }
            else if (tokens[0] == "exterminate")
            {
                Exterminate(Spawnables[tokens[1]]);
                return "exterminated all " + tokens[1];
            }
        }
        // (Exception e)
        {
            //return e.Message;
        }

        return "Not a valid command";
    }

    private void Exterminate(Enum type)
    {
        if (type.GetType() == typeof(PlantType))
            Exterminate((PlantType)type);
        if (type.GetType() == typeof(CarnivoreType))
            Exterminate((CarnivoreType)type);
        if (type.GetType() == typeof(HerbivoreType))
            Exterminate((HerbivoreType)type);
    }

    private void Exterminate(PlantType plantType)
    {
        while (m_PlantMap.ContainsKey(plantType) && m_PlantMap[plantType].Count > 0)
        {
            KillPlant(m_PlantMap[plantType].First());
        }
    }

    private void Exterminate(HerbivoreType herbType)
    {
        while (m_Herbivores.ContainsKey(herbType) && m_Herbivores[herbType].Count > 0)
        {
            KillHerbivore(m_Herbivores[herbType].First());
        }
    }

    private void Exterminate(CarnivoreType carnType)
    {
        while (m_Carnivores.ContainsKey(carnType) && m_Carnivores[carnType].Count > 0)
        {
            KillCarnivore(m_Carnivores[carnType].First());
        }
    }

    [Server]
    private void OnDataChange(Data key, string value)
    {
        DataMap[key] = value;
        RpcOnDataChange(key, value);

        var strKey = key.FastToString();
        if (strKey.StartsWith("Eco", true, CultureInfo.InvariantCulture))
        {
            OnEcoDataChanged(key, value);
        }
        else if (strKey.StartsWith("BrushHead", true, CultureInfo.InvariantCulture))
        {
            BrushHead.ChangeBrushHeadData(key, value, GetHerbivores<BrushHead>(HerbivoreType.BrushHead));
        }
        else if (strKey.StartsWith("TriHorse", true, CultureInfo.InvariantCulture))
        {
            TriHorse.ChangeTriHorseData(key, value, GetHerbivores<TriHorse>(HerbivoreType.TriHorse));
        }
        else if (strKey.StartsWith("Jabarkie", true, CultureInfo.InvariantCulture))
        {
            Jabarkie.ChangeJabarkieData(key, value, GetCarnivores<Jabarkie>(CarnivoreType.Jabarkie));
        }
        else if (strKey.StartsWith("Gnomehatz", true, CultureInfo.InvariantCulture))
        {
            Gnomehatz.ChangeGnomehatzData(key, value, GetCarnivores<Gnomehatz>(CarnivoreType.Gnomehatz));
        }
        else if (strKey.StartsWith("Plant", true, CultureInfo.InvariantCulture))
        {
            PlantAI.ChangePlantData(key, value, GetPlants<PlantAI>(PlantType.Generic));
        }
        else if (strKey.StartsWith("Snatcher", true, CultureInfo.InvariantCulture))
        {
            IEnumerable<SnatcherPlant> snatchers;

            if (m_PlantMap.ContainsKey(PlantType.EmbeddedSnatcher))
            {
                snatchers = GetPlants<SnatcherPlant>(PlantType.EmbeddedSnatcher);
                if (m_PlantMap.ContainsKey(PlantType.FloatingSnatcher))
                    snatchers = snatchers.Concat(GetPlants<SnatcherPlant>(PlantType.FloatingSnatcher));
            }
            else if (m_PlantMap.ContainsKey(PlantType.FloatingSnatcher))
                snatchers = GetPlants<SnatcherPlant>(PlantType.FloatingSnatcher);
            else
                return;

            SnatcherPlant.ChangeSnatcherData(key, value, snatchers);
        }
        else if (strKey.StartsWith("NoduleFloating", true, CultureInfo.InvariantCulture))
        {
            FloatingNodule.ChangeNoduleData(key, value, m_Nodules.Where(n => n.Type == NoduleType.Floating).Select(n => n.gameObject));
        }
        else if (strKey.StartsWith("FloatGLColony", true, CultureInfo.InvariantCulture))
        {
            FloatGrassLargeColony.ChangeFloatGrassLargeColonyData(key, value, GetPlants<FloatGrassLargeColony>(PlantType.FloatGrassLargeColony));
        }
        else if (strKey.StartsWith("FloatGLCluster", true, CultureInfo.InvariantCulture))
        {
            FloatGrassLargeCluster.ChangeFloatGrassClusterData(key, value, GetPlants<FloatGrassLargeCluster>(PlantType.FloatGrassLargeCluster));
        }
        else if (strKey.StartsWith("FloatGLBlade", true, CultureInfo.InvariantCulture))
        {
            FloatGrassLargeBlade.ChangeFloatGrassLargeBladeData(key, value, GetPlants<FloatGrassLargeBlade>(PlantType.FloatGrassLargeBlade));
        }
        else if (strKey.StartsWith("FloatGSColony", true, CultureInfo.InvariantCulture))
        {
            FloatGrassSmallColony.ChangeFloatGrassSmallColonyData(key, value, GetPlants<FloatGrassSmallColony>(PlantType.FloatGrassSmallColony));
        }
        else if (strKey.StartsWith("FloatGSCluster", true, CultureInfo.InvariantCulture))
        {
            FloatGrassSmallCluster.ChangeFloatGrassClusterData(key, value, GetPlants<FloatGrassSmallCluster>(PlantType.FloatGrassSmallCluster));
        }
        else if (strKey.StartsWith("FloatGSBlade", true, CultureInfo.InvariantCulture))
        {
            FloatGrassSmallBlade.ChangeFloatGrassSmallBladeData(key, value, GetPlants<FloatGrassSmallBlade>(PlantType.FloatGrassSmallBlade));
        }
        else if (strKey.StartsWith("SporeGun", true, CultureInfo.InvariantCulture))
        {
            SporeGun.ChangeSporeGunData(key, value, GetPlants<SporeGun>(PlantType.SporeGun));
        }
        else if (strKey.StartsWith("FungiB", true, CultureInfo.InvariantCulture))
        {
            FungiB.ChangeFungiBData(key, value, GetPlants<FungiB>(PlantType.FungiB));
        }
        else if (strKey.StartsWith("Herbistar", true, CultureInfo.InvariantCulture))
        {
            Herbistar.ChangeHerbistarData(key, value, GetHerbivores<Herbistar>(HerbivoreType.Herbistar));
        }
        else if (strKey.StartsWith("DownDown", true, CultureInfo.InvariantCulture))
        {
            DownDown.ChangeDownDownData(key, value, GetHerbivores<DownDown>(HerbivoreType.DownDown));
        }
    }

    private IEnumerable<TCarn> GetCarnivores<TCarn>(CarnivoreType type)
        where TCarn : CarnivoreBase
    {
        return !m_Carnivores.ContainsKey(type)
                    ? Enumerable.Empty<TCarn>()
                    : m_Carnivores[type].Select(cb => (TCarn)cb);
    }

    private IEnumerable<THerb> GetHerbivores<THerb>(HerbivoreType type)
        where THerb : HerbivoreBase
    {
        return !m_Herbivores.ContainsKey(type)
                    ? Enumerable.Empty<THerb>()
                    : m_Herbivores[type].Select(hb => (THerb)hb);
    }

    private IEnumerable<TPlant> GetPlants<TPlant>(PlantType type) 
        where TPlant : PlantBase
    {
        return !m_PlantMap.ContainsKey(type) 
                    ? Enumerable.Empty<TPlant>() 
                    : m_PlantMap[type].Select(pb => (TPlant)pb);
    }

    [ClientRpc]
    private void RpcOnDataChange(Data key, string value)
    {
        //We need to run this check because we may be a host.
        //If we are, we don't want to reset the data
        if (!isServer)
        {
            DataStore.Set(key, value, true, true);
            DataMap[key] = value;
        }

        if (OnDataChangeClientside != null)
            OnDataChangeClientside(key, value);
    }

    [Server]
    private void OnPlayerJoined(GameObject player)
    {
        SendAllDataToClients();
    }

    [Server]
    public void SendAllDataToClients()
    {
        foreach (var data in DataMap)
        {
            RpcOnDataChange(data.Key, data.Value);
        }
    }

    private void OnEcoDataChanged(Data key, string value)
    {
        switch (key)
        {
            case Data.EcoPredation:
                var state = value == "on";
                Creature.PredationAllowed = state;
                break;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.EcoHerbivoreMinimum, HerbivoreMinimum);
        DataStore.SetIfDifferent(Data.EcoCarnivoreMinimum, CarnivoreMinimum);
        DataStore.SetIfDifferent(Data.EcoInitialFGLColonyCount, InitialFGLColonycount);
        DataStore.SetIfDifferent(Data.EcoInitialHerbivoreCount, InitialHerbivoreCount);
        DataStore.SetIfDifferent(Data.EcoInitialCarnivoreCount, InitialCarnivoreCount);
        DataStore.SetIfDifferent(Data.EcoNoduleLimit, NoduleLimit);
        DataStore.SetIfDifferent(Data.EcoHerbivoreLimit, HerbivoreLimit);
        DataStore.SetIfDifferent(Data.EcoCarnivoreLimit, InitialCarnivoreCount);
        DataStore.SetIfDifferent(Data.EcoCreatureSpawnExtent, CreatureSpawnExtent);
        DataStore.SetIfDifferent(Data.EcoWorldExtent, WorldExtent);
        DataStore.SetIfDifferent(Data.EcoPlantSpawnExtentBuffer, PlantSpawnExtentBuffer);
    }
}


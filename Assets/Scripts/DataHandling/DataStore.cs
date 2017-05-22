using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

//Current Count: 211
//These enum members should not have their value changed - it will break all references to that member in the inspector
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum Data
{
    ServerAddress = 65,

    EcoHerbivoreMinimum = 0,
    EcoCarnivoreMinimum = 1,

    EcoInitialPlantCount = 2,
    EcoInitialFGLColonyCount = 3,
    EcoInitialHerbivoreCount = 4,
    EcoInitialCarnivoreCount = 5,

    EcoNoduleLimit = 6,
    EcoPlantLimit = 7,
    EcoHerbivoreLimit = 8,
    EcoCarnivoreLimit = 9,

    EcoCreatureSpawnExtent = 10,
    EcoWorldExtent = 11,

    EcoPlantSpawnExtentBuffer = 12,
    EcoPlantSpawnHeightMin = 13,
    EcoPlantSpawnHeightMax = 14,
    EcoPlantSpawnLayerPercentage = 15,

    EcoPredation = 16,
    //Nodule Options
    NoduleFloatingFloatSpeed = 17,
    NoduleFloatingSendRate = 18,

    //Plant Options
    PlantLifeSpan = 19,
    PlantDaysBeforeRebirth = 20,
    PlantDaysToDoubleSize = 21,
    PlantDaysToDieInShade = 22,
    PlantFullSizeMin = 23,
    PlantFullSizeMax = 24,
    PlantNodulesPerDay = 25,
    PlantNoduleDispersalRange = 26,

    //Snatcher Options
    SnatcherTendrilLength = 27,
    SnatcherNoduleDistance = 28,
    SnatcherTendrilSpeed = 29,
    SnatcherFloatSpeed = 30,
    SnatcherRotationSpeed = 31,
    SnatcherNoduleRespawnDelay = 32,
    SnatcherChildDelay = 33,
    SnatcherDaysToGrown = 34,
    SnatcherInitialScale = 35,
    SnatcherFullScaleMin = 36,
    SnatcherFullScaleMax = 37,
    SnatcherDaysBeforeVulnerable = 38,

    //Float Grass Large Options
    FloatGLBladeInitialYScale = 39,
    FloatGLBladeFinalYScaleMin = 40,
    FloatGLBladeFinalYScaleMax = 41,
    FloatGLBladeDaysToGrown = 42,
    FloatGLBladeNodulesPerDay = 43,
    FloatGLBladeNoduleDispersalRange = 44,
    FloatGLClusterMinBladeCount = 45,
    FloatGLClusterMaxBladeCount = 46,
    FloatGLClusterSpawnHorizontalRange = 47,
    FloatGLClusterSpawnVerticalRange = 48,
    FloatGLBladeFloatSpeed = 49,
    FloatGLBladeFloatRange = 50,
    FloatGLBladePlayerCheckRate = 51,
    FloatGLBladePlayerCheckRange = 52,
    FloatGLBladeColorVariation = 53,
    FloatGLClusterFloatSpeed = 54,
    FloatGLClusterFloatRange = 55,
    FloatGLColonyMinClusterCount = 56,
    FloatGLColonyMaxClusterCount = 57,
    FloatGLColonySpawnHorizontalRange = 58,
    FloatGLColonySpawnVerticalRange = 59,

    //Float Grass Small Options
    FloatGSBladeInitialYScale = 104,
    FloatGSBladeFinalYScaleMin = 105,
    FloatGSBladeFinalYScaleMax = 106,
    FloatGSBladeDaysToGrown = 107,
    FloatGSBladeNodulesPerNight = 108,
    FloatGSBladeNoduleDispersalRange = 109,
    FloatGSClusterMinBladeCount = 110,
    FloatGSClusterMaxBladeCount = 111,
    FloatGSClusterSpawnHorizontalRange = 112,
    FloatGSClusterSpawnVerticalRange = 113,
    FloatGSBladeFloatSpeed = 114,
    FloatGSBladeFloatRange = 115,
    FloatGSBladeColorVariation = 116,
    FloatGSClusterFloatSpeed = 117,
    FloatGSClusterFloatRange = 118,
    FloatGSColonyMinClusterCount = 119,
    FloatGSColonyMaxClusterCount = 120,
    FloatGSColonySpawnHorizontalRange = 121,
    FloatGSColonySpawnVerticalRange = 122,

    //BrushHead Options
    BrushHeadInitialScale = 60,
    BrushHeadFinalScaleMin = 61,
    BrushHeadFinalScaleMax = 62, 
    BrushHeadDaysToGrown = 64,
    BrushHeadLifeSpan = 66,
    BrushHeadBaseSpeed = 67,
    BrushHeadDaysBeforeReproducing = 68,
    BrushHeadDaysBetweenReproductions = 69,
    BrushHeadStarvingDamageAmount = 70,
    BrushHeadStructureCollisionDamageAmount = 71,
    BrushHeadMinFlockDispersion = 72,
    BrushHeadMaxFlockDispersion = 73,
    BrushHeadSensingRadius = 74,
    BrushHeadWanderRadius = 75,
    BrushHeadWanderDistance = 76,
    BrushHeadWanderJitter = 77,
    BrushHeadWanderWeight = 78,
    BrushHeadAlignWeight = 79,

    //TriHorse Options
    TriHorseInitialScale = 194,
    TriHorseFinalScaleMin = 195,
    TriHorseFinalScaleMax = 196,
    TriHorseDaysToGrown = 197,
    TriHorseLifeSpan = 198,
    TriHorseBaseSpeed = 199,
    TriHorseDaysBeforeReproducing = 200,
    TriHorseDaysBetweenReproductions = 201,
    TriHorseStarvingDamageAmount = 202,
    TriHorseStructureCollisionDamageAmount = 203,
    TriHorseMinFlockDispersion = 204,
    TriHorseMaxFlockDispersion = 205,
    TriHorseSensingRadius = 206,
    TriHorseWanderRadius = 207,
    TriHorseWanderDistance = 208,
    TriHorseWanderJitter = 209,
    TriHorseWanderWeight = 210,
    TriHorseAlignWeight = 211,

    //TriHorse Options
    DownDownInitialScale = 212,
    DownDownFinalScaleMin = 213,
    DownDownFinalScaleMax = 214,
    DownDownDaysToGrown = 215,
    DownDownLifeSpan = 216,
    DownDownBaseSpeed = 217,
    DownDownDaysBeforeReproducing = 218,
    DownDownDaysBetweenReproductions = 219,
    DownDownStarvingDamageAmount = 220,
    DownDownStructureCollisionDamageAmount = 221,
    DownDownMinFlockDispersion = 222,
    DownDownMaxFlockDispersion = 223,
    DownDownSensingRadius = 224,
    DownDownWanderRadius = 225,
    DownDownWanderDistance = 226,
    DownDownWanderJitter = 227,
    DownDownWanderWeight = 228,
    DownDownAlignWeight = 229,

    //Carnivore Options
    CarnYoungSize = 80,
    CarnTeenSize = 81,
    CarnAdultSizeMin = 82,
    CarnAdultSizeMax = 83,
    CarnDaysAsYoung = 84,
    CarnDaysAsTeen = 85,
    CarnLifeSpan = 86,
    CarnBaseSpeed = 87,
    CarnHuntingPeriodSpeed = 88,
    CarnDaysBetweenReproductions = 89,
    CarnStarvingDamageAmount = 90,
    CarnStructureCollisionDamageAmount = 91,
    CarnMaximumHerdSizeToAttack = 92,
    CarnHerdApproachDistance = 93,
    CarnSensingRadius = 94,

    //Jabarkie Options
    JabarkieInitialScale = 123,
    JabarkieFinalScaleMin = 124,
    JabarkieFinalScaleMax = 125,
    JabarkieDaysToGrown = 126,
    JabarkieLifeSpan = 127,
    JabarkieBaseSpeed = 128,
    JabarkieHuntingPeriodSpeed = 129,
    JabarkieDaysBetweenReproductions = 130,
    JabarkieStarvingDamageAmount = 131,
    JabarkieStructureCollisionDamageAmount = 132,
    JabarkieMaximumHerdSizeToAttack = 133,
    JabarkieHerdApproachDistance = 134,
    JabarkieSensingRadius = 135,

    //Tortilla Options
    TortillaInitialScale = 136,
    TortillaFinalScaleMin = 137,
    TortillaFinalScaleMax = 138,
    TortillaDaysToGrown = 139,
    TortillaLifeSpan = 140,
    TortillaBaseSpeed = 141,
    TortillaDaysBeforeReproducing = 142,
    TortillaDaysBetweenReproductions = 143,
    TortillaStarvingDamageAmount = 144,
    TortillaStructureCollisionDamageAmount = 145,
    TortillaMinFlockDispersion = 146,
    TortillaMaxFlockDispersion = 147,
    TortillaSensingRadius = 148,
    TortillaWanderRadius = 149,
    TortillaWanderDistance = 150,
    TortillaWanderJitter = 151,
    TortillaWanderWeight = 152,
    TortillaAlignWeight = 153,

    //Gnomehatz Options
    GnomehatzInitialScale = 154,
    GnomehatzFinalScaleMin = 155,
    GnomehatzFinalScaleMax = 156,
    GnomehatzDaysToGrown = 157,
    GnomehatzLifeSpan = 158,
    GnomehatzBaseSpeed = 159,
    GnomehatzHuntingPeriodSpeed = 160,
    GnomehatzDaysBetweenReproductions = 161,
    GnomehatzStarvingDamageAmount = 162,
    GnomehatzStructureCollisionDamageAmount = 163,
    GnomehatzMaximumHerdSizeToAttack = 164,
    GnomehatzHerdApproachDistance = 165,
    GnomehatzSensingRadius = 166,

    //FellyJish Options
    FellyJishInitialScale = 167,
    FellyJishFinalScaleMin = 168,
    FellyJishFinalScaleMax = 169,
    FellyJishDaysToGrown = 170,
    FellyJishLifeSpan = 171,
    FellyJishBaseSpeed = 172,
    FellyJishHuntingPeriodSpeed = 173,
    FellyJishDaysBetweenReproductions = 174,
    FellyJishStarvingDamageAmount = 175,
    FellyJishStructureCollisionDamageAmount = 176,
    FellyJishMaximumHerdSizeToAttack = 177,
    FellyJishHerdApproachDistance = 178,
    FellyJishSensingRadius = 179,

    //SporeGun Options
    SporeGunLifeSpan = 96,
    SporeGunDaysToGrown = 97,
    SporeGunInitialScale = 98,
    SporeGunFinalScaleMin = 99,
    SporeGunFinalScaleMax = 100,
    SporeGunNodulesPerDay = 102,
    SporeGunNoduleFiringSpread = 103,

    //SporeGun Nodule Options
    SporeGunNoduleSpeed = 101,

    //FungiB Options
    FungiBScaleFactor = 63,

    //AirPlant Options
    AirPlantInitialScale = 180,
    AirPlantFinalScaleMin = 181,
    AirPlantFinalScaleMax = 182,
    AirPlantDaysToGrown = 183,
    AirPlantLifeSpan = 184,

    //Herbistar Options
    HerbistarInitialScale = 185,
    HerbistarFinalScaleMin = 186,
    HerbistarFinalScaleMax = 187,
    HerbistarDaysToGrown = 188,
    HerbistarLifeSpan = 189,
    HerbistarBaseSpeed = 190,
    HerbistarWanderRadius = 191,
    HerbistarWanderDistance = 192,
    HerbistarWanderJitter = 193,

    DayLength = 95
}

static class Extensions
{
    private static class EnumStrings<T>
    {
        private static readonly string[] Strings;

        static EnumStrings()
        {
            if (typeof(T).IsEnum)
            {
                Strings = new string[Enum.GetValues(typeof(T)).Length];

                foreach (Enum value in Enum.GetValues(typeof(T)))
                {
                    Strings[((IConvertible)value).ToInt32(CultureInfo.InvariantCulture)] = value.ToString();
                }
            }
            else
            {
                throw new Exception("Generic type must be an enumeration");
            }
        }

        public static string GetEnumString(int enumValue)
        {
            return Strings[enumValue];
        }
    }

    public static string FastToString(this Data data)
    {
        return EnumStrings<Data>.GetEnumString((int)data);
    }

}

public static class DataStore
{
    private static readonly FileInfo DataFile;
    private static readonly Dictionary<string, string> Data = new Dictionary<string, string>();
    private static readonly Dictionary<string, int> DataLocations = new Dictionary<string, int>(); 

    public const string DataPath = "Data/data.txt";
    public delegate void DataChangeDelegate(Data key, string value);
    public static event DataChangeDelegate OnDataChange;

    static DataStore()
    {
        DataFile = new FileInfo(DataPath);

        if (!DataFile.Exists) return;

        LoadData();
    }

    public static Dictionary<string, string> GetAllData()
    {
        return Data;
    }

    public static string Get(Data key)
    {
        return Get(key.FastToString());
    }

    public static string Get(string key)
    {
        if (Application.isPlaying)
        {
            return Data.ContainsKey(key)
                ? Data[key]
                : null;
        }

        var arrLine = File.ReadAllLines(DataPath);
        return arrLine.Where(line => line.StartsWith(key + ' '))
            .Select(line => line.Substring(key.Length + 1))
            .FirstOrDefault();
    }

    public static int GetInt(Data key)
    {
        return GetInt(key.FastToString());
    }

    public static int GetInt(string key)
    {
        if (Application.isPlaying)
        {
            return Data.ContainsKey(key)
                ? int.Parse(Data[key])
                : 0;
        }

        var arrLine = File.ReadAllLines(DataPath);
        return arrLine.Where(line => line.StartsWith(key + ' '))
            .Select(line => int.Parse(line.Substring(key.Length + 1)))
            .FirstOrDefault();
    }

    public static float GetFloat(Data key)
    {
        return GetFloat(key.FastToString());
    }

    public static float GetFloat(string key)
    {
        if (Application.isPlaying)
        {
            return Data.ContainsKey(key)
                ? float.Parse(Data[key])
                : 0;
        }

        var arrLine = File.ReadAllLines(DataPath);
        return arrLine.Where(line => line.StartsWith(key + ' '))
            .Select(line => float.Parse(line.Substring(key.Length + 1)))
            .FirstOrDefault();
    }

    public static void Set(string key, string value)
    {
        Set(key, value, false, false);
    }

    public static void Set(string key, object o)
    {
        Set(key, o.ToString(), false, false);
    }

    public static void Set(Data key, string value)
    {
        Set(key.FastToString(), value, false, false);
    }

    public static void Set(Data key, object o)
    {
        Set(key.FastToString(), o.ToString(), false, false);
    }

    public static void Set(Data key, string value, bool supressChangeEvent, bool suppressSaving)
    {
        Set(key.FastToString(), value, supressChangeEvent, suppressSaving);
    }

    public static void Set(string key, string value, bool supressChangeEvent, bool suppressSaving)
    {
        if (!Application.isPlaying)
        {
            SetEditor(key, value);
            return;
        }

        Data[key] = value;

        //TODO: Fix this crap
        if (OnDataChange != null && !supressChangeEvent)
            OnDataChange((Data)Enum.Parse(typeof(Data), key), value);

        if (suppressSaving)
            return;

        var arrLine = File.ReadAllLines(DataPath);
        arrLine[DataLocations[key]] = key + ' ' + value;
        File.WriteAllLines(DataPath, arrLine);
    }

    public static void SetEditor(string key, string value)
    {
        var arrLine = File.ReadAllLines(DataPath);

        for (var i = 0; i < arrLine.Length; i++)
        {
            if (!arrLine[i].StartsWith(key + ' ')) continue;

            arrLine[i] = key + ' ' + value;
            break;
        }

        File.WriteAllLines(DataPath, arrLine);
    }

    public static bool SetIfDifferent(Data key, string value)
    {
        var old = Get(key);
        if (old == value)
            return false;

        Set(key, value);
        return true;
    }

    public static bool SetIfDifferent(Data key, object o)
    {
        return SetIfDifferent(key, o.ToString());
    }

    private static void LoadData()
    {
        using (var sr = new StreamReader(DataFile.FullName))
        {
            string line;
            var lineNumber = 0;

            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line) || line[0] == '/')
                {
                    lineNumber++;
                    continue;
                }

                var firstSpace = line.IndexOf(' ');
                var key = line.Substring(0, firstSpace);
                var value = line.Substring(firstSpace + 1, line.Length - (key.Length + 1));

                Data.Add(key, value);
                DataLocations.Add(key, lineNumber);

                lineNumber++;
            }
        }
    }
}
  
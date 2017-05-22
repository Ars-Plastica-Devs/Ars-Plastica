using System;
using UnityEngine;

public enum PlantType
{
    Generic = 0,
    EmbeddedSnatcher = 1,
    FloatingSnatcher = 2,

    FloatGrassLargeColony = 3,
    FloatGrassLargeCluster = 4,
    FloatGrassLargeBlade = 5,

    FloatGrassSmallColony = 6,
    FloatGrassSmallCluster = 7,
    FloatGrassSmallBlade = 8,

    FungiB = 9,
    SporeGun = 10,
    AirPlant = 11
}

/// <summary>
/// Wraps the Instantiation of plants so that users only need to 
/// supply a PlantType and transform info
/// </summary>
public class PlantFactory : MonoBehaviour
{
    private static PlantFactory m_Singleton;
    public static PlantFactory Singleton {
        get { return m_Singleton ?? (m_Singleton = new PlantFactory()); }
    }

    public GameObject GenericPlantPrefab;
    public GameObject EmbeddedSnatcherPrefab;
    public GameObject FloatingSnatcherPrefab;
    public GameObject FloatGrassLargeClusterPrefab;
    public GameObject FloatGrassSmallClusterPrefab;
    public GameObject FloatGrassLargeBladePrefab;
    public GameObject FloatGrassSmallBladePrefab;
    public GameObject FloatGrassLargeColonyPrefab;
    public GameObject FloatGrassSmallColonyPrefab;
    public GameObject FungiBPrefab;
    public GameObject SporeGunPrefab;
    public GameObject AirPlantPrefab;

    public static PlantBase InstantiatePlant(Vector3 pos, Quaternion rot, PlantType type)
    {
        return Singleton.InternalInstantiatePlant(pos, rot, type);
    }

    private void Awake()
    {
        m_Singleton = this;
    }

    private PlantBase InternalInstantiatePlant(Vector3 pos, Quaternion rot, PlantType type)
    {
        switch (type)
        {
            case PlantType.Generic:
                return ((GameObject)Instantiate(GenericPlantPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.EmbeddedSnatcher:
                return ((GameObject)Instantiate(EmbeddedSnatcherPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FloatingSnatcher:
                return ((GameObject)Instantiate(FloatingSnatcherPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FloatGrassLargeCluster:
                return ((GameObject)Instantiate(FloatGrassLargeClusterPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FloatGrassSmallCluster:
                return ((GameObject)Instantiate(FloatGrassSmallClusterPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FloatGrassLargeBlade:
                return ((GameObject)Instantiate(FloatGrassLargeBladePrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FloatGrassSmallBlade:
                return ((GameObject)Instantiate(FloatGrassSmallBladePrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FloatGrassLargeColony:
                return ((GameObject)Instantiate(FloatGrassLargeColonyPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FloatGrassSmallColony:
                return ((GameObject)Instantiate(FloatGrassSmallColonyPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.FungiB:
                return ((GameObject)Instantiate(FungiBPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.SporeGun:
                return ((GameObject)Instantiate(SporeGunPrefab, pos, rot)).GetComponent<PlantBase>();
            case PlantType.AirPlant:
                return ((GameObject)Instantiate(AirPlantPrefab, pos, rot)).GetComponent<PlantBase>();
        }

        throw new ArgumentException("Unrecognized type: " + type);
    }

    /// <summary>
    /// Returns a random PlantType that is not composed of other PlantTypes
    /// </summary>
    /// <returns></returns>
    public static PlantType GetRandomSpawnableIndividualType()
    {
        var t = PlantType.FloatGrassLargeColony;
        while (t == PlantType.FloatGrassLargeColony || t == PlantType.FloatGrassLargeCluster
               || t == PlantType.FloatGrassSmallColony || t == PlantType.FloatGrassSmallCluster || t == PlantType.FungiB)
        {
            t = EnumHelper.GetRandomEnum<PlantType>();
        }
        return t;
    }
}

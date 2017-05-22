using System;
using System.Collections.Generic;
using UnityEngine;

public enum HerbivoreType
{
    BrushHead = 0,
    Tortilla = 1,
    Herbistar = 2,
    TriHorse = 3,
    DownDown = 4
}

public class HerbivoreFactory : MonoBehaviour
{
    private static readonly HashSet<HerbivoreType> m_DisabledTypes = new HashSet<HerbivoreType>
    {
        //HerbivoreType.BrushHead,
        //HerbivoreType.DownDown,
        HerbivoreType.Herbistar,
        //HerbivoreType.Tortilla,
        //HerbivoreType.TriHorse
    };

    private static HerbivoreFactory m_Singleton;
    public static HerbivoreFactory Singleton
    {
        get { return m_Singleton ?? (m_Singleton = new HerbivoreFactory()); }
    }

    public GameObject BrushHeadPrefab;
    public GameObject TortillaPerfab;
    public GameObject HerbistarPrefab;
    public GameObject TriHorsePrefab;
    public GameObject DownDownPrefab;

    public static HerbivoreBase InstantiateHerbivore(Vector3 pos, Quaternion rot, HerbivoreType type)
    {
        return Singleton.InternalInstantiateHerbivore(pos, rot, type);
    }

    private void Awake()
    {
        m_Singleton = this;

        foreach (var herbivoreType in m_DisabledTypes)
        {
            Debug.Log("Note that " + herbivoreType + " is disabled.", this);
        }
    }

    private HerbivoreBase InternalInstantiateHerbivore(Vector3 pos, Quaternion rot, HerbivoreType type)
    {
        switch (type)
        {
            case HerbivoreType.BrushHead:
                return Instantiate(BrushHeadPrefab, pos, rot).GetComponent<HerbivoreBase>();
            case HerbivoreType.Tortilla:
                return Instantiate(TortillaPerfab, pos, rot).GetComponent<HerbivoreBase>();
            case HerbivoreType.Herbistar:
                return Instantiate(HerbistarPrefab, pos, rot).GetComponent<HerbivoreBase>();
            case HerbivoreType.TriHorse:
                return Instantiate(TriHorsePrefab, pos, rot).GetComponent<HerbivoreBase>();
            case HerbivoreType.DownDown:
                return Instantiate(DownDownPrefab, pos, rot).GetComponent<HerbivoreBase>();
        }

        throw new ArgumentException("Unrecognized type: " + type);
    }

    /// <summary>
    /// Returns a random HerbivoreType
    /// </summary>
    /// <returns></returns>
    public static HerbivoreType GetRandomSpawnableIndividualType()
    {
        var t = EnumHelper.GetRandomEnum<HerbivoreType>();
        while (m_DisabledTypes.Contains(t))
            t = EnumHelper.GetRandomEnum<HerbivoreType>();

        return t;
    }
}

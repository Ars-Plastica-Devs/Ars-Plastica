using System;
using System.Collections.Generic;
using UnityEngine;

public enum CarnivoreType
{
    Jabarkie = 0,
    Gnomehatz = 1,
    FellyJish = 2
}

public class CarnivoreFactory : MonoBehaviour
{
    private static readonly HashSet<CarnivoreType> m_DisabledTypes = new HashSet<CarnivoreType>
    {
        //CarnivoreType.Jabarkie,
        //CarnivoreType.Gnomehatz,
        //CarnivoreType.FellyJish
    };

    private static CarnivoreFactory m_Singleton;
    public static CarnivoreFactory Singleton
    {
        get { return m_Singleton ?? (m_Singleton = new CarnivoreFactory()); }
    }

    public GameObject JabarkiePrefab;
    public GameObject GnomehatzPrefab;
    public GameObject FellyJishPrefab;

    public static CarnivoreBase InstantiateCarnivore(Vector3 pos, Quaternion rot, CarnivoreType type)
    {
        return m_Singleton.InternalInstantiateCarnivore(pos, rot, type);
    }

    private void Awake()
    {
        m_Singleton = this;

        foreach (var carnivoreType in m_DisabledTypes)
        {
            Debug.Log("Note that " + carnivoreType + " is disabled.", this);
        }
    }

    private CarnivoreBase InternalInstantiateCarnivore(Vector3 pos, Quaternion rot, CarnivoreType type)
    {
        switch (type)
        {
            case CarnivoreType.Jabarkie:
                return Instantiate(JabarkiePrefab, pos, rot).GetComponent<CarnivoreBase>();
            case CarnivoreType.Gnomehatz:
                return Instantiate(GnomehatzPrefab, pos, rot).GetComponent<CarnivoreBase>();
            case CarnivoreType.FellyJish:
                return Instantiate(FellyJishPrefab, pos, rot).GetComponent<CarnivoreBase>();
        }

        throw new ArgumentException("Unrecognized type: " + type);
    }

    /// <summary>
    /// Returns a random CarnivoreType
    /// </summary>
    /// <returns></returns>
    public static CarnivoreType GetRandomSpawnableIndividualType()
    {
        var t = EnumHelper.GetRandomEnum<CarnivoreType>();
        while (m_DisabledTypes.Contains(t))
            t = EnumHelper.GetRandomEnum<CarnivoreType>();

        return t;
    }
}

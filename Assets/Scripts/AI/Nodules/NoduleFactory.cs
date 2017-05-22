using System;
using UnityEngine;

public enum NoduleType
{
    Floating,
    Snatcher,
    SporeGun
}

public class NoduleFactory : MonoBehaviour
{
    private static NoduleFactory m_Singleton;
    public static NoduleFactory Singleton
    {
        get { return m_Singleton ?? (m_Singleton = new NoduleFactory()); }
    }

    public GameObject FloatingNodulePrefab;
    public GameObject SnatcherNodulePrefab;
    public GameObject SporeGunNodulePrefab;

    public static Nodule InstantiateNodule(Vector3 pos, Quaternion rot, NoduleType type)
    {
        return Singleton.InternalInstantiateNodule(pos, rot, type);
    }

    private void Awake()
    {
        m_Singleton = this;
    }

    private Nodule InternalInstantiateNodule(Vector3 pos, Quaternion rot, NoduleType type)
    {
        switch (type)
        {
            case NoduleType.Floating:
                return ((GameObject)Instantiate(FloatingNodulePrefab, pos, rot)).GetComponent<Nodule>();
            case NoduleType.Snatcher:
                return ((GameObject)Instantiate(SnatcherNodulePrefab, pos, rot)).GetComponent<Nodule>();
            case NoduleType.SporeGun:
                return ((GameObject)Instantiate(SporeGunNodulePrefab, pos, rot)).GetComponent<Nodule>();
        }

        throw new ArgumentException("Unrecognized type: " + type);
    }
}

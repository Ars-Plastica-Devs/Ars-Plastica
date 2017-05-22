using System.Collections.Generic;
using System.Linq;
using Assets.Octree;
using UnityEngine;

public class OctreeSensor<T> : IProximitySensor<T> where T : Component
{
    private int m_NumToDetect;
    private float m_LastTimeUpdated;
    private float m_RefreshCounter;

    /// <summary>
    ///     The closest object to this Sensors owner at the last sensor update
    /// </summary>
    public T Closest { get; private set; }
    /// <summary>
    ///     The set of NumToDetect objects closest to this Sensors owner at the last sensor update.
    ///     Will be null if NumToDetect is less than 2.
    /// </summary>
    public HashSet<T> KClosest { get; private set; }

    /// <summary>
    ///     The range to detect objects in
    /// </summary>
    public float Range { get; set; }
    /// <summary>
    ///     The rate at which to detect objects, in seconds
    /// </summary>
    public float RefreshRate { get; set; }
    /// <summary>
    ///     The owner of this OctreeSensor
    /// </summary>
    public Transform Owner { get; set; }
    /// <summary>
    ///     The Octree to use for proximity detection
    /// </summary>
    public Octree Octree { get; set; }

    /// <summary>
    ///     The number objects to detect using the Octree.
    /// </summary>
    public int NumToDetect
    {
        get { return m_NumToDetect; }
        set
        {
            m_NumToDetect = value;
            if (m_NumToDetect > 1 && KClosest == null)
                KClosest = new HashSet<T>();
            else if (m_NumToDetect < 2)
                KClosest = null; //Let it be garbage collected
        }
    }

    public OctreeSensor(Transform owner, float range, Octree octree)
        : this (owner, range, 1, octree)
    {
    }

    public OctreeSensor(Transform owner, float range, int numToDetect, Octree octree)
    {
        RefreshRate = 1f;

        Owner = owner;
        Range = range;
        NumToDetect = numToDetect;

        Octree = octree;
    }

    /// <summary>
    ///     Performs an update to the sensor. Respects the sensors update rate.
    /// </summary>
    public void SensorUpdate()
    {
        var dt = Time.time - m_LastTimeUpdated;
        m_LastTimeUpdated = Time.time;

        m_RefreshCounter += dt;

        if (m_RefreshCounter < RefreshRate) return;

        m_RefreshCounter = 0;

        if (m_NumToDetect == 1)
            SetClosestInRange();
        else
            SetKClosestInRange();
    }

    /// <summary>
    ///     Performs an update to the sensor. Ignores the sensors update rate.
    /// </summary>
    public void ForceUpdate()
    {
        m_RefreshCounter = 0f;

        if (m_NumToDetect == 1)
            SetClosestInRange();
        else
            SetKClosestInRange();
    }

    private void SetClosestInRange()
    {
        if (Octree == null)
            Debug.Log("h");
        var closest = Octree.GetClosestObject(Owner.position, Range);

        if (closest == null || closest.gameObject == null || !closest.gameObject.activeInHierarchy)
            Closest = null;
        else
            Closest = closest.gameObject.GetComponent<T>();
    }

    private void SetKClosestInRange()
    {
        var closest = Octree.GetKClosestObjects(Owner.position, m_NumToDetect, Range);

        if (closest.Length == 0)
        {
            Closest = null;
            KClosest.Clear();
            return;
        }

        var firstOrDefault = closest.FirstOrDefault(t => t.GetComponent<T>() != null);
        Closest = firstOrDefault != null ? firstOrDefault.GetComponent<T>() : null;

        KClosest.Clear();
        KClosest.UnionWith(closest.Where(t => t.GetComponent<T>() != null).Select(t => t.GetComponent<T>()));
    }
}

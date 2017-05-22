using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NoduleSet : ICollection<Nodule>
{
    private readonly Dictionary<short, Nodule> m_Nodules = new Dictionary<short, Nodule>();
    private readonly HashSet<short> m_DisabledIDs = new HashSet<short>();

    public short NextID { get; private set; }
    public Func<Vector3, Quaternion, NoduleType, Nodule> SpawnNoduleController;

    public IEnumerator<Nodule> GetEnumerator()
    {
        return m_Nodules.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Nodule First(Func<Nodule, bool> predicate)
    {
        return m_Nodules.First(n => !m_DisabledIDs.Contains(n.Key) && predicate(n.Value)).Value;
    }

    public Nodule FirstOrDefault(Func<Nodule, bool> predicate)
    {
        var nod = m_Nodules.FirstOrDefault(n => !m_DisabledIDs.Contains(n.Key) && predicate(n.Value));
        return nod.Equals(default(KeyValuePair<short, Nodule>)) ? null : nod.Value;
    }

    public Nodule SpawnNodule(Vector3 pos, Quaternion rot, NoduleType type)
    {
        //Check for a disabled nodule of the right type
        foreach (var id in m_DisabledIDs.ToList())
        {
            if (m_Nodules[id].Type == type)
            {
                return EnableNodule(pos, rot, id);
            }
        }

        var nod = SpawnNoduleController(pos, rot, type);
        Add(nod);
        return nod;
    }

    private Nodule EnableNodule(Vector3 pos, Quaternion rot, short id)
    {
        m_DisabledIDs.Remove(id);
        m_Nodules[id].ServerEnable(pos, rot);
        return m_Nodules[id];
    }

    public void DisableNodule(Nodule nod)
    {
        Debug.Assert(!m_DisabledIDs.Contains(nod.ID));
        m_DisabledIDs.Add(nod.ID);
        nod.ServerDisable();
    }

    public void Add(GameObject item)
    {
        var nodCon = item.GetComponent<Nodule>();

        if (nodCon == null)
            throw new ArgumentException();

        Add(nodCon);
    }

    public void Add(Nodule item)
    {
        item.ID = NextID;
        m_Nodules.Add(NextID, item);

        if (m_Nodules.Count == (short.MaxValue * 2) - 1)
        {
            throw new Exception("Overloaded Nodule Set");
        }

        while (m_Nodules.ContainsKey(NextID) && m_Nodules[NextID].enabled)
        {
            NextID++;

            if (NextID == short.MaxValue)
                NextID = short.MinValue;
        }
    }

    public void Clear()
    {
        m_Nodules.Clear();
    }

    public bool Contains(Nodule item)
    {
        return m_Nodules.ContainsKey(item.ID);
    }

    public void CopyTo(Nodule[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove(GameObject item)
    {
        var nodCon = item.GetComponent<Nodule>();

        if (nodCon == null)
            throw new ArgumentException();

        return Remove(nodCon);
    }

    public bool Remove(Nodule item)
    {
        return m_Nodules.Remove(item.ID);
    }

    public int Count {
        get { return m_Nodules.Count - m_DisabledIDs.Count; }
    }
    public bool IsReadOnly {
        get { return false; }
    }
}

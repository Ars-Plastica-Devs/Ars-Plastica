using System.Collections.Generic;
using UnityEngine;

namespace Assets.Octree
{
    internal class ResultList
    {
        private struct Record
        {
            public float Distance;
            public Transform Object;
        }

        private class RecordComparer : IComparer<Record>
        {
            public int Compare(Record x, Record y)
            {
                return x.Distance.CompareTo(y.Distance);
            }
        }

        private readonly RecordComparer m_Comparer = new RecordComparer();
        private readonly List<Record> m_Records;

        public int Count
        {
            get { return m_Records.Count; }
        }

        public ResultList(int initialCapacity = 10)
        {
            m_Records = new List<Record>(initialCapacity);
        }

        public void Clear()
        {
            m_Records.Clear();
        }

        public void Add(float d, Transform o)
        {
            var r = new Record { Distance = d, Object = o };
            var index = m_Records.BinarySearch(r, m_Comparer);
            if (index < 0) index = ~index;
            m_Records.Insert(index, r);
        }

        public void RemoveAt(int i)
        {
            m_Records.RemoveAt(i);
        }

        public float GetDistance(int i)
        {
            return m_Records[i].Distance;
        }

        public Transform GetObject(int i)
        {
            return m_Records[i].Object;
        }

        public Transform[] GetObjects()
        {
            var ret = new Transform[m_Records.Count];
            for (var i = 0; i < m_Records.Count; i++)
            {
                ret[i] = m_Records[i].Object;
            }
            return ret;
        }
    }
}

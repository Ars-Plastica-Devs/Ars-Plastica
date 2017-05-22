using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Octree
{
    public class Octree : IOctree
    {
        //To avoid memory allocation, we define static collections to be re-used for scratch work
        //Making these static means we are not thread safe - but Unity game logic is meant to be
        //single threaded anyways.
        private static readonly ResultList CachedResultList = new ResultList();
        private static readonly HashSet<Transform> CachedHashSet = new HashSet<Transform>();
        private static readonly List<Transform> CachedList = new List<Transform>();

        private readonly Queue<Transform> m_PendingInsertion;
        private readonly Queue<Transform> m_PendingRemoval;
        private bool m_TreeBuilt;

        private Bounds m_Region;
        private readonly Octree m_Parent;
        private readonly List<Transform> m_Objects;
        private readonly Octree[] m_ChildNodes = new Octree[8];

        private byte m_ActiveNodes;
        private const int MIN_SIZE = 2;

        /// <summary>
        /// Creates an octree that is a child of another octree.
        /// By knowing we are a child, we can save some memory allocation
        /// </summary>
        private Octree(Bounds region, List<Transform> objects, Octree parent)
        {
            if (objects == null)
                throw new NullReferenceException("Cannot have a null list of objects");

            m_Region = region;
            m_Objects = objects;
            m_Parent = parent;

            //If we are a child, we wont need these allocations
            if (parent == null)
            {
                m_PendingInsertion = new Queue<Transform>();
                m_PendingRemoval = new Queue<Transform>();
            }
        }

        public Octree(Bounds region, List<Transform> objects)
            : this(region, objects, null)
        {
        }

        public Octree(Bounds region)
            : this(region, new List<Transform>(), null)
        {
        }

        public void Update()
        {
            //Remove null references
            for (var i = 0; i < m_Objects.Count; i++)
            {
                if (m_Objects[i] == null)
                {
                    m_Objects.RemoveAt(i);
                    i--;
                }
            }

            //Update active branches
            for (var i = 0; i < 8; i++)
            {
                if ((m_ActiveNodes & (1 << i)) != 0)
                {
                    m_ChildNodes[i].Update();
                }
            }

            CachedHashSet.Clear(); //Used as scratch to track items already re-inserted
                                   //Move children up to parent if they have moved out of our bounds
            for (var i = 0; i < m_Objects.Count; i++)
            {
                var obj = m_Objects[i];

                //If an object hasn't moved, then it is still in the correct octree
                //CachedHashSet contains items that we already re-inserted so we won't
                //continually remove and re-add them to a tree
                if (!obj || CachedHashSet.Contains(obj))
                    continue;

                //TODO: rework this to work with objects that have bounds
                //Find the parent region that contains this object
                var current = this;
                while (!current.m_Region.Contains(obj.position))
                {
                    if (current.m_Parent != null) current = current.m_Parent;
                    else break;
                }


                m_Objects.RemoveAt(i);
                i--;
                current.Insert(obj);

                CachedHashSet.Add(obj);
            }

            //If a branch is dead, remove it
            for (int flags = m_ActiveNodes, index = 0; flags > 0; flags >>= 1, index++)
            {
                if ((flags & 1) == 1 && m_ChildNodes[index].m_Objects.Count == 0 &&
                    m_ChildNodes[index].m_ActiveNodes == 0)
                {
                    m_ChildNodes[index] = null;
                    m_ActiveNodes ^= (byte)(1 << index);       //remove the node from the active nodes flag list
                }

                /*if ((flags & 1) == 1 && m_ChildNodes[index].m_CurrentLife == 0 && m_ChildNodes[index].m_Objects.Count == 0)
                {
                    m_ChildNodes[index] = null;
                    m_ActiveNodes ^= (byte)(1 << index);       //remove the node from the active nodes flag list
                }*/
            }
        }

        public void Add(Transform t)
        {
            if (t == null)
                return;

            m_PendingInsertion.Enqueue(t);
        }

        public void Remove(Transform t)
        {
            if (t == null)
                return;

            m_PendingRemoval.Enqueue(t);
        }

        #region Queeries
        public Transform GetClosestObject(Vector3 pos, float maxDistance = float.MaxValue)
        {
            UpdateTree();
            var r = maxDistance * maxDistance;
            return NearestNeighborSearch(pos, ref r);
        }

        public Transform[] GetKClosestObjects(Vector3 pos, int k, float range = float.MaxValue)
        {
            UpdateTree();

            CachedResultList.Clear();
            var r = range * range;
            KNearestNeighborSearch(pos, k, ref r, CachedResultList);
            return CachedResultList.GetObjects();
        }

        public Transform[] GetObjectsInRange(Vector3 pos, float range = float.MaxValue)
        {
            UpdateTree();

            CachedList.Clear();
            AllNearestNeighborsSearch(pos, range * range, CachedList);
            return CachedList.ToArray();
        }

        public void GetObjectsInRange(Vector3 pos, List<Transform> objectsInRange, float range = float.MaxValue)
        {
            UpdateTree();

            AllNearestNeighborsSearch(pos, range * range, objectsInRange);
        }

        public Transform[] GetObjectsInBounds(Bounds b)
        {
            UpdateTree();

            CachedList.Clear();
            ObjectsInBoundsSearch(b, CachedList);
            return CachedList.ToArray();
        }

        public void GetObjectsInBounds(Bounds b, List<Transform> results)
        {
            UpdateTree();

            ObjectsInBoundsSearch(b, results);
        }

        #endregion

        #region Internal Queeries
        private Transform NearestNeighborSearch(Vector3 pos, ref float distanceSquared)
        {
            Transform closest = null;

            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

                    var ds = (pos - obj.position).sqrMagnitude;

                    if (!(ds < distanceSquared))
                        continue;

                    distanceSquared = ds;
                    closest = obj;
                }
                return closest;
            }

            for (var i = 0; i < 8; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0 ||
                    (m_ChildNodes[i].m_Objects.Count == 0 && m_ChildNodes[i].m_ActiveNodes == 0))
                    continue;

                //If a border is closer than the closest distance so far, it might have a closer object
                var distToChildBorder = m_ChildNodes[i].m_Region.SqrDistance(pos);

                if (!(distToChildBorder < distanceSquared))
                    continue;

                var testObject = m_ChildNodes[i].NearestNeighborSearch(pos, ref distanceSquared);
                if (testObject != null)
                {
                    closest = testObject;
                    distanceSquared = (pos - closest.position).sqrMagnitude;
                }
            }

            return closest;
        }

        private void KNearestNeighborSearch(Vector3 pos, int k, ref float rangeSquared, ResultList results)
        {
            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

                    var ds = (pos - obj.position).sqrMagnitude;

                    if (ds > rangeSquared)
                        continue;

                    //If results list has empty elements
                    if (results.Count < k)
                    {
                        results.Add(ds, obj);
                        continue;
                    }

                    if (ds < results.GetDistance(results.Count - 1))
                    {
                        results.RemoveAt(results.Count - 1);
                        results.Add(ds, obj);
                        rangeSquared = results.GetDistance(results.Count - 1);
                    }
                }
                return;
            }

            //Check if we should check children
            for (var i = 0; i < 8; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0 ||
                    (m_ChildNodes[i].m_Objects.Count == 0 && m_ChildNodes[i].m_ActiveNodes == 0))
                    continue;

                //If a border is closer than the farthest distance so far, it might have a closer object
                var distToChildBorder = m_ChildNodes[i].m_Region.SqrDistance(pos);

                if (distToChildBorder > rangeSquared)
                    continue;

                m_ChildNodes[i].KNearestNeighborSearch(pos, k, ref rangeSquared, results);
            }
        }

        private void AllNearestNeighborsSearch(Vector3 pos, float rangeSquared, ICollection<Transform> results)
        {
            //We have no children, check objects in this node
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

                    var ds = (pos - obj.position).sqrMagnitude;

                    if (ds > rangeSquared)
                        continue;

                    results.Add(obj);
                }
                return;
            }

            //Check if we should check children
            for (var i = 0; i < 8; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0)
                    continue;

                //If a border is closer than the farthest distance so far, it might have a closer object
                var distToChildBorder = m_ChildNodes[i].m_Region.SqrDistance(pos);

                if ((distToChildBorder > rangeSquared))
                    continue;

                m_ChildNodes[i].AllNearestNeighborsSearch(pos, rangeSquared, results);
            }
        }

        private void ObjectsInBoundsSearch(Bounds b, ICollection<Transform> results)
        {
            if (m_ActiveNodes == 0)
            {
                for (var i = 0; i < m_Objects.Count; i++)
                {
                    var obj = m_Objects[i];
                    if (obj == null)
                        continue;

                    if (!b.Contains(obj.position))
                        return;

                    results.Add(obj);
                }
            }

            //Check if we should check children
            for (var i = 0; i < 8; i++)
            {
                if ((m_ActiveNodes & (1 << i)) == 0)
                    continue;

                if (m_ChildNodes[i].m_Region.Intersects(b))
                    m_ChildNodes[i].ObjectsInBoundsSearch(b, results);
            }
        }

        #endregion Internal Queeries

        #region Internal Operations
        private void UpdateTree()
        {
            while (m_PendingInsertion.Count != 0)
            {
                Insert(m_PendingInsertion.Dequeue());
            }

            while (m_PendingRemoval.Count != 0)
            {
                Delete(m_PendingRemoval.Dequeue());
            }

            for (var i = 0; i < m_Objects.Count; i++)
            {
                if (m_Objects[i] == null)
                {
                    m_Objects.RemoveAt(i);
                    i--;
                }
            }

            if (!m_TreeBuilt)
                BuildTree();
        }

        private void BuildTree()
        {
            if (m_Objects.Count <= 1)
            {
                m_TreeBuilt = true;
                return; //We are a leaf node - we are done
            }


            var dimensions = m_Region.max - m_Region.min;

            //Smallest we can get, no more subdividing
            //For an octree, all the bounds are cubes, so we only 
            //need to check one axis
            if (dimensions.x <= MIN_SIZE)
            {
                m_TreeBuilt = true;
                return;
            }

            var half = dimensions / 2f;
            var quarLen = half.x / 2f; //a quarter of the length of a side of this region
            var center = m_Region.center;

            //Create child bounds
            var octants = new Bounds[8];
            octants[0] = new Bounds(center + new Vector3(-quarLen, quarLen, quarLen), half);
            octants[1] = new Bounds(center + new Vector3(quarLen, quarLen, quarLen), half);
            octants[2] = new Bounds(center + new Vector3(quarLen, quarLen, -quarLen), half);
            octants[3] = new Bounds(center + new Vector3(-quarLen, quarLen, -quarLen), half);
            octants[4] = new Bounds(center + new Vector3(-quarLen, -quarLen, quarLen), half);
            octants[5] = new Bounds(center + new Vector3(quarLen, -quarLen, quarLen), half);
            octants[6] = new Bounds(center + new Vector3(quarLen, -quarLen, -quarLen), half);
            octants[7] = new Bounds(center + new Vector3(-quarLen, -quarLen, -quarLen), half);

            //Objects that go in each octant
            //Since these lists will be used by the octants
            //there is no reason to cache them
            var octList = new List<Transform>[8];
            for (var i = 0; i < 8; i++) octList[i] = new List<Transform>();

            CachedList.Clear();
            //list of objects moved into children
            var delist = CachedList;

            //Move objects into children
            for (var index = 0; index < m_Objects.Count; index++)
            {
                var obj = m_Objects[index];

                if (obj == null)
                {
                    //Get rid of the null object
                    m_Objects.RemoveAt(index);
                    index--;
                    continue;
                }

                for (var i = 0; i < 8; i++)
                {
                    //TODO: Expand this to deal with objects with bounds, not just points
                    if (octants[i].Contains(obj.position))
                    {
                        octList[i].Add(obj);
                        delist.Add(obj);
                        break;
                    }
                }
            }

            //Delist objects that were moved into the children
            for (var i = 0; i < delist.Count; i++)
            {
                m_Objects.Remove(delist[i]);
            }

            //Create child nodes
            for (var i = 0; i < 8; i++)
            {
                if (octList[i].Count == 0)
                    continue;

                m_ChildNodes[i] = CreateChildNode(octants[i], octList[i]);
                m_ActiveNodes |= (byte)(1 << i);
                m_ChildNodes[i].BuildTree();
            }

            m_TreeBuilt = true;
        }

        private void Insert(Transform obj)
        {
            if (obj == null)
                return;

            if (m_Objects.Count <= 1 && m_ActiveNodes == 0)
            {
                m_Objects.Add(obj);
                return;
            }

            var dimensions = m_Region.max - m_Region.min;

            //Smallest we can get, no more subdividing
            //For an octree, all the bounds are cubes, so we only 
            //need to check one axis it seems
            if (dimensions.x <= MIN_SIZE
                /*&& dimensions.y <= MIN_SIZE
                && dimensions.z <= MIN_SIZE*/)
            {
                m_Objects.Add(obj);
                return;
            }

            var half = dimensions / 2f;
            var quarLen = half.x / 2f; //a quarter of the length of a side of this region
            var center = m_Region.center;

            //Create child bounds, using existing values if possible
            var octants = new Bounds[8];
            octants[0] = (m_ChildNodes[0] != null)
                            ? m_ChildNodes[0].m_Region
                            : new Bounds(center + new Vector3(-quarLen, quarLen, quarLen), half);
            octants[1] = (m_ChildNodes[1] != null)
                            ? m_ChildNodes[1].m_Region
                            : new Bounds(center + new Vector3(quarLen, quarLen, quarLen), half);
            octants[2] = (m_ChildNodes[2] != null)
                            ? m_ChildNodes[2].m_Region
                            : new Bounds(center + new Vector3(quarLen, quarLen, -quarLen), half);
            octants[3] = (m_ChildNodes[3] != null)
                            ? m_ChildNodes[3].m_Region
                            : new Bounds(center + new Vector3(-quarLen, quarLen, -quarLen), half);
            octants[4] = (m_ChildNodes[4] != null)
                            ? m_ChildNodes[4].m_Region
                            : new Bounds(center + new Vector3(-quarLen, -quarLen, quarLen), half);
            octants[5] = (m_ChildNodes[5] != null)
                            ? m_ChildNodes[5].m_Region
                            : new Bounds(center + new Vector3(quarLen, -quarLen, quarLen), half);
            octants[6] = (m_ChildNodes[6] != null)
                            ? m_ChildNodes[6].m_Region
                            : new Bounds(center + new Vector3(quarLen, -quarLen, -quarLen), half);
            octants[7] = (m_ChildNodes[7] != null)
                            ? m_ChildNodes[7].m_Region
                            : new Bounds(center + new Vector3(-quarLen, -quarLen, -quarLen), half);

            var found = false;
            for (var i = 0; i < 8 && !found; i++)
            {
                //TODO: Expand this to deal with objects with bounds, not just points
                if (octants[i].Contains(obj.position))
                {
                    //Insert if the child exists
                    if (m_ChildNodes[i] != null)
                    {
                        m_ChildNodes[i].Insert(obj);
                    }
                    //Create and build if the child does not exist
                    else
                    {
                        m_ChildNodes[i] = CreateChildNode(octants[i], new List<Transform> { obj });
                        m_ActiveNodes |= (byte)(1 << i);
                        m_ChildNodes[i].BuildTree();
                    }
                    found = true;
                }
            }
            if (!found)
                m_Objects.Add(obj);
        }

        private bool Delete(Transform t)
        {
            if (m_Objects.Count > 0 && m_Objects.Remove(t))
                return true;

            //For each active node, try to delete from that node
            for (int flags = m_ActiveNodes, index = 0; flags > 0; flags >>= 1, index++)
            {
                if ((flags & 1) == 1 && m_ChildNodes[index].Delete(t))
                {
                    return true;
                }
            }

            return false;
        }

        private Octree CreateChildNode(Bounds region, List<Transform> objects)
        {
            return objects.Count == 0
                ? null
                : new Octree(region, objects, this);
        }
        #endregion

        public void DrawBounds()
        {
            Gizmos.DrawWireCube(m_Region.center, m_Region.size);

            for (var i = 0; i < 8; i++)
            {
                if (m_ChildNodes[i] != null)
                    m_ChildNodes[i].DrawBounds();
            }
        }
    }
}

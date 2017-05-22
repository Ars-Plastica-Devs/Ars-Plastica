using System.Collections.Generic;
using UnityEngine;

namespace Assets.Octree
{
    public class OctreeComponent : MonoBehaviour, IOctree
    {
        private bool m_UpdateRequired;
        private Octree m_Tree;

        public OctreeType Type;

        [Tooltip("The desired distance from the center of the Octree to it's farthest edge.")]
        public float Extent;

        [Tooltip("Controls whether or not to draw the Octree boundaries in the Editor's Scene View")]
        public bool Draw;

        [Tooltip("How often to perform an update on the Octree. Set to 0 to update every frame.")]
        public float UpdateRate;
        private float m_UpdateCounter;

        private void Awake()
        {
            if (OctreeManager.Contains(Type))
                OctreeManager.Remove(Type);

            m_Tree = new Octree(new Bounds(transform.position, new Vector3(Extent, Extent, Extent) * 2f));
            OctreeManager.AddOctree(Type, m_Tree);
        }

        private void Update()
        {
            m_UpdateCounter += Time.deltaTime;
            if (m_UpdateCounter > UpdateRate || m_UpdateRequired)
            {
                m_UpdateRequired = false;
                m_UpdateCounter = 0f;
                m_Tree.Update();
            }
        }

        private void OnDrawGizmos()
        {
            if (Draw && Application.isPlaying)
            {
                m_Tree.DrawBounds();
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(transform.position, Vector3.one);
            }
            
        }

        /// <summary>
        /// Forces an update on the Octree regardless of update timer progress
        /// </summary>
        public void ForceUpdate()
        {
            m_UpdateCounter = 0f;
            m_UpdateRequired = false;
            m_Tree.Update();
        }

        /// <summary>
        /// Adds the given object to the Octree.
        /// </summary>
        public void Add(Transform obj)
        {
            m_Tree.Add(obj);

            m_UpdateRequired = true;
        }

        /// <summary>
        /// Removes the given object from the Octree.
        /// </summary>
        public void Remove(Transform obj)
        {
            m_Tree.Remove(obj);

            m_UpdateRequired = true;
        }

        /// <summary>
        /// Returns the object that is closest to the given position. 
        /// If no object exists within the given range, returns null
        /// </summary>
        public Transform GetClosestObject(Vector3 pos, float range)
        {
            return m_Tree.GetClosestObject(pos, range);
        }

        /// <summary>
        /// Returns the K closest objects to the given position within the given range.
        /// If less than K objects exist within the given range, less than K objects will
        /// be returned.
        /// </summary>
        public Transform[] GetKClosestObjects(Vector3 pos, int k, float range)
        {
            return m_Tree.GetKClosestObjects(pos, k, range);
        }

        /// <summary>
        /// Returns all objects within the given range of the given position.
        /// </summary>
        public Transform[] GetObjectsInRange(Vector3 pos, float range)
        {
            return m_Tree.GetObjectsInRange(pos, range);
        }

        /// <summary>
        /// Adds all objects within the given range of the given position
        /// to the objectsInRange List
        /// </summary>
        public void GetObjectsInRange(Vector3 pos, List<Transform> objectsInRange, float range)
        {
            m_Tree.GetObjectsInRange(pos, objectsInRange, range);
        }

        /// <summary>
        /// Returns all objects within the given Bounds
        /// </summary>
        public Transform[] GetObjectsInBounds(Bounds b)
        {
            return m_Tree.GetObjectsInBounds(b);
        }

        /// <summary>
        /// Adds all objects within the given Bounds to the given List
        /// </summary>
        public void GetObjectsInBounds(Bounds b, List<Transform> results)
        {
            m_Tree.GetObjectsInBounds(b, results);
        }
    }
}

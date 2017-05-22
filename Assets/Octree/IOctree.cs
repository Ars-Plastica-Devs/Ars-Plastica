using System.Collections.Generic;
using UnityEngine;

namespace Assets.Octree
{
    interface IOctree
    {
        /// <summary>
        /// Adds the given object to the Octree.
        /// </summary>
        void Add(Transform obj);

        /// <summary>
        /// Removes the given object from the Octree.
        /// </summary>
        void Remove(Transform obj);

        /// <summary>
        /// Returns the object that is closest to the given position. 
        /// If no object exists within the given range, returns null
        /// </summary>
        Transform GetClosestObject(Vector3 pos, float range);

        /// <summary>
        /// Returns the K closest objects to the given position within the given range.
        /// If less than K objects exist within the given range, less than K objects will
        /// be returned.
        /// </summary>
        Transform[] GetKClosestObjects(Vector3 pos, int k, float range);

        /// <summary>
        /// Returns all objects within the given range of the given position.
        /// </summary>
        Transform[] GetObjectsInRange(Vector3 pos, float range);

        /// <summary>
        /// Adds all objects within the given range of the given position
        /// to the objectsInRange List
        /// </summary>
        void GetObjectsInRange(Vector3 pos, List<Transform> objectsInRange, float range);

        /// <summary>
        /// Returns all objects within the given Bounds
        /// </summary>
        Transform[] GetObjectsInBounds(Bounds b);

        /// <summary>
        /// Adds all objects within the given Bounds to the given List
        /// </summary>
        void GetObjectsInBounds(Bounds b, List<Transform> results);
    }
}

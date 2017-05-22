using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Octree
{
    /// <summary>
    /// Keeps track of existing Octrees and provides an easy way for any
    /// object to get an Octree of a certain type
    /// </summary>
    public static class OctreeManager
    {
        private static readonly Dictionary<OctreeType, Octree> Trees = new Dictionary<OctreeType, Octree>();

        public static Octree Get(OctreeType key)
        {
            if (Trees.ContainsKey(key))
                return Trees[key];

            //Debug.Log("Tried to get an Octree for an unknown type " + key);
            return null;
        }

        public static void AddOctree(OctreeType key, Octree tree)
        {
            if (Trees.ContainsKey(key))
                throw new ArgumentException("The OctreeManager already contains an Octree for " + key);

            Trees[key] = tree;
        }

        public static bool Contains(OctreeType key)
        {
            return Trees.ContainsKey(key);
        }

        public static bool Remove(OctreeType key)
        {
            return Trees.Remove(key);
        }
    }
}

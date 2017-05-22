using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Debug
{
    /// <summary>
    /// Wraps a MonoBehaviour so that we can keep track of all debugging related components
    /// </summary>
    public abstract class DebugBehaviour : MonoBehaviour
    {
        public static readonly HashSet<DebugBehaviour> All = new HashSet<DebugBehaviour>();

        public delegate void OnNewDebugBehaviourDelegate(DebugBehaviour db);
        public static event OnNewDebugBehaviourDelegate OnNewDebugBehaviour;

        protected virtual void Awake()
        {
            All.Add(this);

            if (OnNewDebugBehaviour != null)
                OnNewDebugBehaviour(this);
        }

        protected virtual void OnDestroy()
        {
            All.Remove(this);
        }
    }
}

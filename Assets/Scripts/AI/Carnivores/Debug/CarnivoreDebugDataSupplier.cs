using System;
using Assets.Scripts.AI.Debug;
using UnityEngine;

namespace Assets.Scripts.AI.Carnivores.Debug
{
    /// <summary>
    /// Provides string output of some helpful debugging data related to a specific carnivore.
    /// </summary>
    public class CarnivoreDebugDataSupplier : MonoBehaviour, IAIDebugDataSupplier
    {
        private CarnivoreBase m_Owner;

        private void Start()
        {
            //We allow this MonoBehvaiour to be placed on a child object.
            //If it is placed on a MonoBehaviour with a CarnivoreBase component,
            //GetComponentInParent will still find it.
            m_Owner = GetComponentInParent<CarnivoreBase>();

            if (m_Owner == null)
                throw new Exception(name + " does not have a CarnivoreBase component");
        }

        public string GetDebugDataString()
        {
            var behaviour = "AI State: " + m_Owner.CurrentBehaviourState;
            var age = "Age: " + m_Owner.AgeData.DaysOld.ToString("0.00") + " days";
            var health = "Health: " + m_Owner.Health;

            return behaviour + "\n" + age + "\n" + health;
        }
    }
}

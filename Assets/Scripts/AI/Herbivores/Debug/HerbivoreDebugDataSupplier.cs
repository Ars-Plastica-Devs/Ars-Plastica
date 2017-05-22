using System;
using Assets.Scripts.AI.Debug;
using UnityEngine;

namespace Assets.Scripts.AI.Herbivores.Debug
{
    /// <summary>
    /// Provides string output of some helpful debugging data related to a specific herbivore.
    /// </summary>
    public class HerbivoreDebugDataSupplier : MonoBehaviour, IAIDebugDataSupplier
    {
        private HerbivoreBase m_Owner;

        private void Start()
        {
            //We allow this MonoBehvaiour to be placed on a child object.
            //If it is placed on a MonoBehaviour with a HerbivoreBase component,
            //GetComponentInParent will still find it.
            m_Owner = GetComponentInParent<HerbivoreBase>();

            if (m_Owner == null)
                throw new Exception(name + " does not have a HerbivoreBase component");
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

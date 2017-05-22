using Assets.Octree;
using UnityEngine;

namespace Assets.Scripts.Utility
{
    /// <summary>
    /// Activates a certain set of MonoBehaviour's when a player is within a certain range.
    /// </summary>
    public class ActivateOnPlayerProximity : MonoBehaviour
    {
        private IProximitySensor<Transform> m_Sensor;

        public float Range = 200f;
        public MonoBehaviour[] ToActivate;

        private void Start()
        {
            m_Sensor = new OctreeSensor<Transform>(transform, Range, 1, OctreeManager.Get(OctreeType.Player));
        }

        private void Update()
        {
            m_Sensor.SensorUpdate();

            foreach (var mb in ToActivate)
            {
                if (mb == null)
                    continue;
                mb.enabled = (m_Sensor.Closest != null);
            }
        }
    }
}

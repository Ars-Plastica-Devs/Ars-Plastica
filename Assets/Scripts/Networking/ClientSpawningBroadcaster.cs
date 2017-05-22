using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking
{
    /// <summary>
    /// Allows IClientSpawningListener's to register and listen for when a certain NetworkInstanceId 
    /// is spawned on the client, and then receive a callback
    /// </summary>
    public class ClientSpawningBroadcaster : MonoBehaviour
    {
        public static ClientSpawningBroadcaster Singleton;

        private void Awake()
        {
            Singleton = this;
        }

        private readonly Dictionary<NetworkInstanceId, HashSet<IClientSpawningListener>> m_Listeners = new Dictionary<NetworkInstanceId, HashSet<IClientSpawningListener>>(); 

        public void RegisterListener(NetworkInstanceId id, IClientSpawningListener listener)
        {
            var o = ClientScene.FindLocalObject(id);
            if (o != null)
            {
                listener.OnObjectSpawned(o);
                return;
            }

            if (!m_Listeners.ContainsKey(id))
                m_Listeners[id] = new HashSet<IClientSpawningListener>();

            m_Listeners[id].Add(listener);
        }

        public void RemoveListener(NetworkInstanceId id, IClientSpawningListener listener)
        {
            if (m_Listeners.ContainsKey(id))
                m_Listeners[id].Remove(listener);
        }

        public void OnNetIDSpawned(NetworkInstanceId id)
        {
            if (!m_Listeners.ContainsKey(id))
                return;

            var o = ClientScene.FindLocalObject(id);
            if (o != null)
            {
                foreach (var listener in m_Listeners[id])
                {
                    listener.OnObjectSpawned(o);
                }
            }

            m_Listeners.Remove(id);
        }
    }

    public interface IClientSpawningListener
    {
        void OnObjectSpawned(GameObject obj);
    }
}

using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking
{
    public class NetworkedChild : NetworkBehaviour, IClientSpawningListener
    {
        [SyncVar(hook="OnParentNetIDChange")]
        private NetworkInstanceId m_ParentNetID;
        public NetworkInstanceId ParentNetID
        {
            get { return m_ParentNetID; }
            set { m_ParentNetID = value; }
        }

        public override void OnStartClient()
        {
            if (transform.parent == null)
                ClientSpawningBroadcaster.Singleton.RegisterListener(m_ParentNetID, this);
        }

        public void SetParent(Transform t, bool worldPositionStays = true)
        {
            transform.SetParent(t, worldPositionStays);

            var netIdentity = t.GetComponent<NetworkIdentity>();
            if (netIdentity != null)
                ParentNetID = netIdentity.netId;
        }

        public void OnObjectSpawned(GameObject obj)
        {
            transform.parent = obj.transform;
        }

        private void OnParentNetIDChange(NetworkInstanceId newID)
        {
            ClientSpawningBroadcaster.Singleton.RemoveListener(m_ParentNetID, this);
            m_ParentNetID = newID;
            ClientSpawningBroadcaster.Singleton.RegisterListener(m_ParentNetID, this);
        }
    }
}

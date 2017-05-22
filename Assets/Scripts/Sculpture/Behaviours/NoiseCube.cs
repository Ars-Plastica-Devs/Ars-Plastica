using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Sculpture.Behaviours
{
    public class NoiseCube : CubeBehaviour
    {
        [SyncVar]
        private NetworkInstanceId m_ParentNetID;

        public override NetworkInstanceId ParentNetID
        {
            get { return m_ParentNetID; }
            set { m_ParentNetID = value; }
        }

        public float TargetZ;

        public void Initialize()
        {
            enabled = true;
        }

        private void Update()
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, TargetZ);
        }

        //Somehow, this is fixing a NetworkReader.ReadByte out of range error.
        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            base.OnDeserialize(reader, initialState);
        }
    }
}

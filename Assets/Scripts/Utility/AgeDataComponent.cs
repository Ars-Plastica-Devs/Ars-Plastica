using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Utility
{
    public class AgeDataComponent : NetworkBehaviour
    {
        private float m_SecondsOld;

        [SerializeField]
        [InspectorReadOnly]
        private float m_DaysOld;

        [SyncVar]
        public float LifeSpan;

        [SyncVar]
        public float DaysToGrown;

        public float DaysOld {
            get { return m_DaysOld; }
            set
            {
                m_DaysOld = value;
                m_SecondsOld = DayClock.Singleton.DaysToSeconds(m_DaysOld);
            }
        }

        public override void OnStartServer()
        {
            StartCoroutine(SyncDaysOldCoroutine());
        }

        private IEnumerator SyncDaysOldCoroutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(3f);
                RpcSetDaysOld(DaysOld);
            }
        }

        [ClientRpc]
        private void RpcSetDaysOld(float days)
        {
            DaysOld = days;
        }

        private void Start()
        {
            DaysOld = 0f;
        }

        private void Update()
        {
            m_SecondsOld += Time.deltaTime;
            
            if (DayClock.Singleton != null)
                DaysOld = DayClock.Singleton.SecondsToDays(m_SecondsOld);
        }
    }
}

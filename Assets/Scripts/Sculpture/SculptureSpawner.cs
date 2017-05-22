using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Sculpture
{
    public class SculptureSpawner : NetworkBehaviour
    {
        public string Sculpture;
        public float Delay;

        public override void OnStartServer()
        {
            base.OnStartServer();
            StartCoroutine(Spawn());
        }

        public override void OnStartClient()
        {
            if (!isServer)
                Destroy(gameObject);
        }

        private IEnumerator Spawn()
        {
            yield return new WaitForSeconds(Delay);

            while (SculptureManager.Instance == null)
                yield return new WaitForSeconds(Delay);

            SculptureManager.Instance.SpawnSculpture(Sculpture, transform.position);
            Destroy(gameObject);
        }
    }
}

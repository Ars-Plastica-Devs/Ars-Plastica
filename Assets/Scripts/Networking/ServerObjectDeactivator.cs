using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking
{
    public class ServerObjectDeactivator : NetworkBehaviour
    {
        public GameObject[] Objects;
        public override void OnStartServer()
        {
            for (var i = 0; i < Objects.Length; i++)
            {
                Objects[i].SetActive(false);
            }
        }
        public override void OnStartClient()
        {
            for (var i = 0; i < Objects.Length; i++)
            {
                Objects[i].SetActive(true);
            }
        }
    }
}

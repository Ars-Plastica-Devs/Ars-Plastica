using UnityEngine;

namespace Assets.Scripts.UI
{
    public class OpenURLHandler : MonoBehaviour
    {
        public string URL;

        public void OpenURL()
        {
            Application.OpenURL(URL);
        }
    }
}

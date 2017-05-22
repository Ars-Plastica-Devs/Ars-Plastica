using UnityEngine;
using UnityEngine.UI;

public class StreamSetPanelController : MonoBehaviour
{
    public Text Header;
    public InputField StreamInput;

    public delegate void StreamEditEnd(string stream);
    public event StreamEditEnd OnStreamEditEnd;

    public void OnSetClick()
    {
        if (OnStreamEditEnd != null)
        {
            OnStreamEditEnd(StreamInput.text);
        }
    }
}

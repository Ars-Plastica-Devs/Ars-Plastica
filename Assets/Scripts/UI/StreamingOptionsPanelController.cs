using UnityEngine;

public class StreamingOptionsPanelController : MonoBehaviour
{
    private StreamingAudioOption m_OptionBeingSet;
    public HUDManager Manager;
    public StreamingAudioOption[] AudioOptions;
    public StreamSetPanelController StreamSetController;

    private void Start ()
    {
        StreamSetController.OnStreamEditEnd += OnStreamSet;

        foreach (var option in AudioOptions)
        {
            option.OnPlay += OnOptionPlay;
            option.OnSetRequest += OnOptionRequestSet;
            option.AudioStream = PlayerPrefs.GetString("AudioStream " + option.Header.text, string.Empty);
        }
	}

    private void OnOptionPlay(StreamingAudioOption option)
    {
        Manager.BroadcastAudioStreamToPlay(option.AudioStream);
    }

    private void OnOptionRequestSet(StreamingAudioOption option)
    {
        StreamSetController.gameObject.SetActive(true);
        StreamSetController.Header.text = "Set Audio Stream " + option.Header.text;
        StreamSetController.StreamInput.text = option.AudioStream;
        m_OptionBeingSet = option;
    }

    private void OnStreamSet(string stream)
    {
        m_OptionBeingSet.AudioStream = stream;
        PlayerPrefs.SetString("AudioStream " + m_OptionBeingSet.Header.text, stream);
        PlayerPrefs.Save();
        StreamSetController.gameObject.SetActive(false);
    }
}

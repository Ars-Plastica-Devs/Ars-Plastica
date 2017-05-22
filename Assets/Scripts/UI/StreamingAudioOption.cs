using UnityEngine;
using UnityEngine.UI;

public class StreamingAudioOption : MonoBehaviour
{
    public string AudioStream;
    public Text Header;
    public Button PlayButton;
    public Button SetButton;

    public delegate void PlayAudioStreamDelegate(StreamingAudioOption option);
    public event PlayAudioStreamDelegate OnPlay;

    public delegate void SetAudioStreamDelegate(StreamingAudioOption option);
    public event SetAudioStreamDelegate OnSetRequest;

	private void Start ()
	{
	    PlayButton.onClick.AddListener(OnPlayClick);
        SetButton.onClick.AddListener(OnSetClick);
    }

    private void OnPlayClick()
    {
        if (OnPlay != null) OnPlay(this);
    }

    private void OnSetClick()
    {
        if (OnSetRequest != null) OnSetRequest(this);
    }
}

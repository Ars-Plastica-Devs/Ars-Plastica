using UnityEngine;
using UnityEngine.Networking;

public class OctantAudioManager : NetworkBehaviour
{
    //private readonly AudioSource[] m_AudioSources = new AudioSource[8];
    //private WorldBoundaryBox m_WorldBox;
    public string[] OctantAudioURLs = new string[8];

    /*public override void OnStartClient()
    {
        m_WorldBox = GetComponent<WorldBoundaryBox>();
        //Invoke("LoadAudioStreams", 1f);
    }*/

    /*private void LoadAudioStreams()
    {
        for (var i = 1; i < 9; i++)
        {
            if (string.IsNullOrEmpty(OctantAudioURLs[i - 1]))
                continue;

            var oct = m_WorldBox.GetOctantObject(i);
            var source = oct.GetComponent<AudioSource>();

            m_AudioSources[i - 1] = source;

            
            var www = new WWW(OctantAudioURLs[i - 1]);
            source.clip = www.GetAudioClip(true, true, AudioType.OGGVORBIS);
            //StartCoroutine(SetStreamedClip(www, source));
        }
    }*/

    /*private IEnumerator SetStreamedClip(WWW www, AudioSource source)
    {
        while (www.progress < .5f)
        {
            yield return null;
        }
        
        source.clip = www.GetAudioClip(true, true, AudioType.OGGVORBIS);
    }*/

    /*private void Update()
    {
        if (!isClient)
            return;

        for (var i = 0; i < 8; i++)
        {
            var aud = m_AudioSources[i];
            if (aud != null
                && aud.clip != null
                && !aud.isPlaying
                && aud.clip.loadState == AudioDataLoadState.Loading)
            {
                aud.Play();
            }
        }
    }*/
}

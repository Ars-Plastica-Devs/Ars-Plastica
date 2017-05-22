using UnityEngine;
using UnityEngine.Networking;

public class SkyboxLightControl : NetworkBehaviour
{
    private float m_Amplitude;
    private DayClock m_Clock;

    public Color ColorStart = Color.black;
    public Color ColorEnd = Color.magenta;
    public Light MySun;
    public float IntesnityStart = 0f;
    public float IntensityEnd = 1f;

    private void Start()
    {
        m_Clock = GetComponent<DayClock>();
    }

    private void Update()
    {
        var lerp = Mathf.PingPong(m_Clock.Hour, 12f) / 12f;
        RenderSettings.skybox.SetColor("_Tint", Color.Lerp(ColorStart, ColorEnd, lerp));
        m_Amplitude = Mathf.Lerp(IntesnityStart, IntensityEnd, lerp);
        MySun.intensity = m_Amplitude;
    }
}

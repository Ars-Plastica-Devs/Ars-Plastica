using System;
using UnityEngine;
using UnityEngine.UI;

public class VanishingText : MonoBehaviour
{
    private Text m_Text;
    private float m_Counter;
    private Color m_ColorAtFadeStart = Color.clear;
    private Color m_TargetFadeColor = Color.clear;
    public float StartFadePercentage = .9f;
    public float VanishingTime = 5f;
    public Action<GameObject> OnVanish;

	private void Start ()
	{
	    m_Text = GetComponent<Text>();
	}

    private void Update ()
    {
        if (m_Text.text.Length == 0) return;

        m_Counter += Time.deltaTime;

        if (m_Counter > VanishingTime * StartFadePercentage)
        {
            if (m_ColorAtFadeStart == Color.clear)
            {
                m_ColorAtFadeStart = m_Text.color;
                m_TargetFadeColor = new Color(m_ColorAtFadeStart.r, m_ColorAtFadeStart.g, m_ColorAtFadeStart.b, 0);
            }

            var progress = (VanishingTime - m_Counter) / (VanishingTime - (VanishingTime * StartFadePercentage));
            m_Text.color = Color.Lerp(m_TargetFadeColor, m_ColorAtFadeStart, progress);

            if (m_Counter > VanishingTime)
            {
                m_Text.text = string.Empty;
                m_Counter = 0f;

                if (OnVanish != null)
                    OnVanish(gameObject);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public class VanishingText : MonoBehaviour
{
    private Text m_Text;
    private float m_Counter;
    public float VanishingTime = 3f;

	private void Start ()
	{
	    m_Text = GetComponent<Text>();
	}

    private void Update ()
    {
        if (m_Text.text.Length == 0) return;

        m_Counter += Time.deltaTime;

        if (m_Counter > VanishingTime)
        {
            m_Text.text = string.Empty;
            m_Counter = 0f;
        }
    }
}

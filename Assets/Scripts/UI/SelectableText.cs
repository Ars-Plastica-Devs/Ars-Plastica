using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectableText : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private Text m_Text;

    [SerializeField]
    private Image m_BackgroundImage;

    private Color m_SelectedColor;
    private Color m_NormalColor;
    private bool m_Selected;

    public bool Selected
    {
        get { return m_Selected; }
        set
        {
            m_Selected = value;
            m_BackgroundImage.color = m_Selected 
                                    ? m_SelectedColor 
                                    : m_NormalColor;
        }
    }

    public Color SelectedColor
    {
        get { return m_SelectedColor; }
        set
        {
            m_SelectedColor = value;
            if (m_Selected)
            {
                m_BackgroundImage.color = m_SelectedColor;
            }
        }
    }

    public Color NormalColor
    {
        get { return m_NormalColor; }
        set
        {
            m_NormalColor = value;
            if (!m_Selected)
            {
                m_BackgroundImage.color = m_NormalColor;
            }
        }
    }

    public string Text
    {
        get { return m_Text.text; }
        set { m_Text.text = value; }
    }

    public float Width
    {
        get { return m_BackgroundImage.gameObject.GetComponent<RectTransform>().rect.width; }
    }

    public float Height
    {
        get { return m_BackgroundImage.gameObject.GetComponent<RectTransform>().rect.height; }
    }

    public Action<SelectableText> OnSelectedChanged;

    public void OnPointerDown(PointerEventData eventData)
    {
        var old = Selected;
        Selected = !Selected;

        if (OnSelectedChanged != null && Selected != old)
            OnSelectedChanged(this);
    }
}

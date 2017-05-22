using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
[Serializable]
public class LabeledDataField : UIBehaviour
{
    [SerializeField]
    [HideInInspector]
    private float m_DividingLine = .5f;

    [SerializeField] public Text Text;
    [SerializeField] public DataField DataField;

    public Font Font
    {
        get { return Text.font; }
        set
        {
            Text.font = value;
            DataField.textComponent.font = value;
        }
    }

    public FontStyle FontStyle
    {
        get { return Text.fontStyle; }
        set
        {
            Text.fontStyle = value;
            DataField.textComponent.fontStyle = value;
        }
    }

    public int FontSize
    {
        get { return Text.fontSize; }
        set
        {
            Text.fontSize = value;
            DataField.textComponent.fontSize = value;
        }
    }

    public TextAnchor TextAlignment
    {
        get { return Text.alignment; }
        set { Text.alignment = value; }
    }

    public Color FontColor
    {
        get { return Text.color; }
        set
        {
            Text.color = value;
            DataField.textComponent.color = value;
        }
    }

    public InputField.ContentType ContentType
    {
        get { return DataField.contentType; }
        set { DataField.contentType = value; }
    }

    public Image.Type BackgroundImageType
    {
        get { return DataField.GetComponent<Image>().type; }
        set
        {
            DataField.GetComponent<Image>().type = value;
            
        }
    }

    public string LabelText
    {
        get { return Text.text; }
        set { Text.text = value; }
    }

    public float Divider
    {
        get { return m_DividingLine; }
        set
        {
            m_DividingLine = Mathf.Clamp(value, 0f, 1f);
            PositionElements();
        }
    }

    public Data Data
    {
        get { return DataField.Data; }
        set { DataField.Data = value; }
    }

    public void Initialize()
    {
        if (GetComponent<RectTransform>() == null)
            gameObject.AddComponent<RectTransform>();

        var textObj = new GameObject("Text")
        {
            //hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
        };
        Text = textObj.AddComponent<Text>();
        Text.transform.SetParent(transform, false);

        var dataFieldObj = new GameObject("DataField")
        {
            //hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector
        };
        DataField = dataFieldObj.AddComponent<DataField>();
        DataField.transform.SetParent(transform, false);

        SetupDataField(DataField);
        PositionElements();
    }

    protected override void Start()
    {
        FontColor = new Color(.2f, .2f, .2f, 1f);
        TextAlignment = TextAnchor.MiddleCenter;
        GetComponent<RectTransform>().sizeDelta = new Vector2(156.7f, 18);

        base.Start();
    }

    private void SetupDataField(DataField dataField)
    {
        var image = dataField.gameObject.AddComponent<Image>();
        image.type = Image.Type.Sliced;
        dataField.targetGraphic = image;

        var textObj = new GameObject("Text");
        var placeHolderObj = new GameObject("Placeholder");

        textObj.transform.SetParent(dataField.transform, false);
        placeHolderObj.transform.SetParent(dataField.transform, false);

        dataField.textComponent = textObj.AddComponent<Text>();
        dataField.placeholder = placeHolderObj.AddComponent<Text>();

        dataField.textComponent.alignment = TextAnchor.MiddleCenter;
        ((Text)(dataField.placeholder)).alignment = TextAnchor.MiddleCenter;
    }

    private void PositionElements()
    {
        var rectTransform = GetComponent<RectTransform>();
        var textTransform = Text.rectTransform;
        var dataTransform = DataField.GetComponent<RectTransform>();

        //Set Pivot
        textTransform.pivot = new Vector2(0f, .5f);
        dataTransform.pivot = new Vector2(1f, .5f);

        //Set Anchors
        textTransform.anchorMin = new Vector2(0, 1);
        textTransform.anchorMax = new Vector2(0, 1);
        dataTransform.anchorMin = new Vector2(1, 1);
        dataTransform.anchorMax = new Vector2(1, 1);

        //Set Positions
        textTransform.anchoredPosition = new Vector2(0f, -rectTransform.rect.height / 2f);
        dataTransform.anchoredPosition = new Vector2(0f, -rectTransform.rect.height / 2f);

        //Set Widths
        textTransform.sizeDelta = new Vector2(rectTransform.rect.width * m_DividingLine, rectTransform.rect.height);
        dataTransform.sizeDelta = new Vector2(rectTransform.rect.width * (1f - m_DividingLine), rectTransform.rect.height);
    }
}
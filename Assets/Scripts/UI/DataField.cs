using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DataField : InputField
{
    public Data Data;

    protected override void Start()
    {
        base.Start();

        if (!Application.isPlaying)
            return;

        StartCoroutine(SetManager());
    }

    private IEnumerator SetManager()
    {
        while (HUDManager.Singleton == null)
            yield return new WaitForSeconds(.5f);

        onEndEdit.AddListener(val => HUDManager.Singleton.ForwardDataCommand(Data, val));
    }

    protected override void OnRectTransformDimensionsChange()
    {
        var t = GetComponent<RectTransform>();
        if (textComponent != null)
        {
            textComponent.rectTransform.pivot = new Vector2(0, 1);
            textComponent.rectTransform.anchorMin = new Vector2(0, 1);
            textComponent.rectTransform.anchorMax = new Vector2(0, 1);
            textComponent.rectTransform.anchoredPosition = new Vector2(0, 0);
            textComponent.rectTransform.sizeDelta = new Vector2(t.rect.width, t.rect.height);
        }

        if (placeholder != null)
        {
            placeholder.rectTransform.pivot = new Vector2(0, 1);
            placeholder.rectTransform.anchorMin = new Vector2(0, 1);
            placeholder.rectTransform.anchorMax = new Vector2(0, 1);
            placeholder.rectTransform.anchoredPosition = new Vector2(0, 0);
            placeholder.rectTransform.sizeDelta = new Vector2(t.rect.width, t.rect.height);
        }

        base.OnRectTransformDimensionsChange();
    }

    private void Update()
    {
        if (!isFocused)
            text = DataStore.Get(Data);
    }
}

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(LabeledDataField))]
[CanEditMultipleObjects]
public class LabeledDataFieldEditor : Editor
{
    [MenuItem("GameObject/UI/Labeled Data Field", false, 0)]
    public static void Init(MenuCommand command)
    {
        var ldf = new GameObject("Labeled Data Field");

        var ldfComp = ldf.AddComponent<LabeledDataField>();
        ldfComp.Initialize();

        ldfComp.DataField.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");

        GameObjectUtility.SetParentAndAlign(ldf, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(ldf, "Created " + ldf.name);
        Selection.activeObject = ldf;
    }

    [MenuItem("GameObject/Convert to Labeled Data Field", false, 0)]
    public static void ConvertToLabeledDataField(MenuCommand command)
    {
        if (!(command.context is GameObject))
            return;

        var go = ((GameObject) command.context);
        var parent = go.transform.parent;
        var oldRectTransform = go.GetComponent<RectTransform>();
        var text = go.GetComponent<Text>();
        var dataField = go.GetComponentInChildren<DataField>();

        if (oldRectTransform == null || text == null || dataField == null)
            return;

        command.context = parent.gameObject; //Set this so that Init child the LDF correctly

        var ldf = new GameObject("Labeled Data Field");

        var ldfComp = ldf.AddComponent<LabeledDataField>();

        ldfComp.DataField.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");

        GameObjectUtility.SetParentAndAlign(ldf, command.context as GameObject);
        Undo.RegisterCreatedObjectUndo(ldf, "Created " + ldf.name);
        Selection.activeObject = ldf;

        ldf.name = text.gameObject.name;
        ldfComp.Font = text.font;
        ldfComp.FontStyle = text.fontStyle;
        ldfComp.FontSize = text.fontSize;
        ldfComp.TextAlignment = text.alignment;
        ldfComp.FontColor = text.color;
        ldfComp.ContentType = dataField.contentType;
        ldfComp.BackgroundImageType = dataField.GetComponent<Image>().type;
        ldfComp.LabelText = text.text;
        ldfComp.Divider = .6f;
        ldfComp.Data = dataField.Data;

        var newRectTransform = ldf.GetComponent<RectTransform>();
        newRectTransform.pivot = oldRectTransform.pivot;
        newRectTransform.anchorMin = oldRectTransform.anchorMin;
        newRectTransform.anchorMax = oldRectTransform.anchorMax;
        newRectTransform.anchoredPosition = oldRectTransform.anchoredPosition;
        newRectTransform.anchoredPosition = new Vector2(81.45f, newRectTransform.anchoredPosition.y);

        DestroyImmediate(go);
        DestroyImmediate(text);
        DestroyImmediate(dataField);
    }

    public override void OnInspectorGUI()
    {
        var t = target as LabeledDataField;

        if (t == null)
            return;

        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Character", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Font");
        t.Font = (Font)EditorGUILayout.ObjectField(t.Font, typeof (Font), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Font Style");
        t.FontStyle = (FontStyle)EditorGUILayout.EnumPopup(t.FontStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Font Size");
        t.FontSize = EditorGUILayout.IntField(t.FontSize);
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField("Paragraph", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Text Alignment");
        t.TextAlignment = (TextAnchor)EditorGUILayout.EnumPopup(t.TextAlignment);
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Font Color");
        t.FontColor = EditorGUILayout.ColorField(t.FontColor);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Content Type");
        t.ContentType = (InputField.ContentType)EditorGUILayout.EnumPopup(t.ContentType);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Background Image Type");
        t.BackgroundImageType = (Image.Type)EditorGUILayout.EnumPopup(t.BackgroundImageType);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Label Text");
        t.LabelText = EditorGUILayout.TextField(t.LabelText);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Divider");
        t.Divider = EditorGUILayout.Slider(t.Divider, 0f, 1f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Data");
        t.Data = (Data)EditorGUILayout.EnumPopup(t.Data);
        EditorGUILayout.EndHorizontal();

        EditorUtility.SetDirty(t);
    }
}
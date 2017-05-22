using UnityEditor;

[CustomEditor(typeof(DataField))]
[CanEditMultipleObjects]
public class DataFieldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //Can't remember why this is required... be careful if deleting this
        //I think it keeps this call from being compiled away
        var t = (DataField) target;
    }
}

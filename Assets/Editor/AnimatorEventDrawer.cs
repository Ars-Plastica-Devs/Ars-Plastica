using Assets.Scripts.Animation;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorEvent))]
    public class AnimatorEventDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);

            var nameProp = property.FindPropertyRelative("EventName");
            var hashProp = property.FindPropertyRelative("NameHash");

            hashProp.intValue = Animator.StringToHash(nameProp.stringValue);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}

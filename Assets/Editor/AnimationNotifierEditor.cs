using Assets.Scripts.Animation;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    [CustomEditor(typeof(AnimationNotifier))]
    public class AnimationNotifierEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var t = (AnimationNotifier)target;
            t.NameHash = Animator.StringToHash(t.AnimName);
        }
    }
}

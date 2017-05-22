using UnityEngine;

public class BehaviourToggle : MonoBehaviour
{
    public Behaviour Target;
    [Tooltip("The key to toggle this component with. Use KeyCode.None to turn off keyboard toggle.")]
    public KeyCode ToggleKey = KeyCode.None;

    private void Start()
    {
        if (Target == null)
            Debug.LogError("Target is set to null in BehaviourToggle", this);
    }

    private void Update()
    {
        if (!Cursor.visible && Input.GetKeyDown(ToggleKey))
            Target.enabled = !Target.enabled;
    }
}
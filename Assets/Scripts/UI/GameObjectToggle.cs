using UnityEngine;

public class GameObjectToggle : MonoBehaviour
{
    [Tooltip("The GameObject to toggle. Defaults to the GameObject this component is attached to.")]
    public GameObject Target;
    [Tooltip("The key to toggle this object with. Use KeyCode.None to turn off keyboard toggle.")]
    public KeyCode ToggleKey = KeyCode.None;

    private void Start()
    {
        if (Target == null)
            Target = gameObject;
    }

    private void Update()
    {
        if (ToggleKey == KeyCode.None)
            return;

        if (!Cursor.visible && Input.GetKeyDown(ToggleKey))
            Toggle();
    }

    public void Toggle()
    {
        Target.SetActive(!Target.activeSelf);
    }

    private void OnValidate()
    {
        if (Target == null)
            Target = gameObject;
    }
}

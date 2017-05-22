using UnityEngine;

public class DeactivateOnMouseClick : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            gameObject.SetActive(!gameObject.activeSelf);
    }
}

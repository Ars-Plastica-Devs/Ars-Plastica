using UnityEngine;

public class Draggable : MonoBehaviour
{
    public void OnDrag()
    {
        transform.position = Input.mousePosition;
    }
}

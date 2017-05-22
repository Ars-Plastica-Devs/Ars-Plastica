using UnityEngine;

[ExecuteInEditMode]
public class PositionCopy : MonoBehaviour
{
    public Transform Source;
    public Vector3 LocalOffset = Vector3.zero;
    
    private void Update()
    {
        transform.position = Source.position + transform.TransformDirection(LocalOffset);
    }
}

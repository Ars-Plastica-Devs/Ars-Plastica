using UnityEngine;

[ExecuteInEditMode]
public class LocalRotationCopy : MonoBehaviour
{
    public Transform Source;

    private void Update()
    {
        transform.localRotation = Source.localRotation;
    }
}
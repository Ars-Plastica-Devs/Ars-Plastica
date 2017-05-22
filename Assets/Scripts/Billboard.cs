using System.Collections;
using UnityEngine;

/// <summary>
/// Have the transform this is attached to always face the main camera.
/// Used to have text in 3d world always appear facing the camera aka 'billboarding'
/// </summary>
[ExecuteInEditMode]
public class Billboard : MonoBehaviour
{
    private Camera m_Camera;

    private void Start()
    {
        StartCoroutine(SetCamera());
    }

    private IEnumerator SetCamera()
    {
        while ((m_Camera = Camera.main) == null)
        {
            yield return 0;
        }
    }

    private void LateUpdate()
    {
        if (m_Camera != null)
        {
            transform.LookAt(m_Camera.transform.position, Vector3.up);
            transform.Rotate(Vector3.up, 180);
        }
    }
}
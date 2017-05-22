//using System.Collections;
using UnityEngine;
//using UnityEngine.Networking;

//TODO: Verify that this works since removing it's networking dependence.
//The commented code/using statements are relevant to the networking implementation.

/// <summary>
/// Sharpens a TextMesh so it looks clear
/// </summary>
public class TextMeshSharpener : /*NetworkBehaviour*/MonoBehaviour
{
    private Camera m_Camera;
    private float m_LastPixelHeight = -1;
    private TextMesh m_Mesh;

    private void Start()
    {
        m_Mesh = GetComponent<TextMesh>();
    }

    /*public override void OnStartClient()
    {
        StartCoroutine(SetCamera());
    }*/

    /*private IEnumerator SetCamera()
    {
        while ((m_Camera = Camera.main) == null)
        {
            yield return 0;
        }
    }*/

    private void Update()
    {
        if (m_Camera == null)
        {
            m_Camera = Camera.main;
            return;
        }

        //Resize if resolution changes or if in editor
        if (m_Camera.pixelHeight != m_LastPixelHeight)
        {
            Resize();
        }
    }

    private void Resize()
    {
        var ph = m_Camera.pixelHeight;
        var ch = m_Camera.orthographicSize;
        var pixelRatio = (ch * 2f) / ph;
        var targetRes = 128f;
        m_Mesh.characterSize = pixelRatio * m_Camera.orthographicSize /
                               Mathf.Max(transform.localScale.x, transform.localScale.y);
        m_Mesh.fontSize = (int)Mathf.Round(targetRes / m_Mesh.characterSize);
        m_LastPixelHeight = ph;
    }
}

using UnityEngine;

/// <summary>
/// Sharpens a TextMesh so it looks clear
/// </summary>
public class TextMeshSharpener : MonoBehaviour
{
    private float m_LastPixelHeight = -1;
    private TextMesh m_Mesh;
	private void Start ()
	{
	    if (!Network.isClient)
	    {
	        enabled = false;
	        return;
	    }

        m_Mesh = GetComponent<TextMesh>();
        Resize();
	}
	
	private void Update ()
	{
	    if (!Network.isClient) return;
        //Resize if resolution changes or if in editor
	    if (Camera.main.pixelHeight != m_LastPixelHeight 
            || (Application.isEditor && !Application.isPlaying))
	    {
            Resize();
	    }
    }

    private void Resize()
    {
        var ph = Camera.main.pixelHeight;
        var ch = Camera.main.orthographicSize;
        var pixelRatio = (ch * 2f) / ph;
        var targetRes = 128f;
        m_Mesh.characterSize = pixelRatio * Camera.main.orthographicSize /
                               Mathf.Max(transform.localScale.x, transform.localScale.y);
        m_Mesh.fontSize = (int) Mathf.Round(targetRes / m_Mesh.characterSize);
        m_LastPixelHeight = ph;
    }
}

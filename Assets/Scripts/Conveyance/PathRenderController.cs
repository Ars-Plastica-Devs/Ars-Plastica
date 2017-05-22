using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class PointList : SyncListStruct<Vector3>
{

}

public class PathRenderController : NetworkBehaviour
{
    public LineRenderer Renderer;
    public TextMesh Text;

    public Color Color;
    public PointList Points = new PointList();
    public string Name;

    private void Start()
    {
        if (!isClient)
            return;

        //Points.Callback = OnPointsListChange;

        var renderer = GetComponent<LineRenderer>();
        renderer.material.color = Color;
        renderer.positionCount = Points.Count - 1;
        renderer.SetPositions(Points.ToArray());
        renderer.startColor = Color;
        renderer.endColor = Color;

        var text = transform.Find("Text");
        text.GetComponent<TextMesh>().text = Name;

        if (Points.Count == 0)
            return;

        text.transform.position = Points.Count % 2 == 0
                                ? (Points[(Points.Count / 2) - 1] + Points[(Points.Count / 2)]) / 2f
                                : Points[(Points.Count / 2)];
    }

    [ClientRpc]
    private void RpcRefreshPath()
    {
        var renderer = GetComponent<LineRenderer>();
        renderer.material.color = Color;
        renderer.positionCount = Points.Count - 1;
        renderer.SetPositions(Points.ToArray());
        renderer.startColor = Color;
        renderer.endColor = Color;

        if (Points.Count == 0)
            return;

        var text = transform.Find("Text");
        text.GetComponent<TextMesh>().text = Name;
        text.transform.position = Points.Count % 2 == 0
                                ? (Points[(Points.Count / 2) - 1] + Points[(Points.Count / 2)]) / 2f
                                : Points[(Points.Count / 2)];
    }

    [Server]
    public void ServerSetPoints(List<Vector3> points)
    {
        Points.Clear();
        foreach (var p in points)
        {
            Points.Add(p);
        }
        Invoke("RpcRefreshPath", .5f);
    }

    [Server]
    public void ServerSetName(string pathName)
    {
        OnNameChange(pathName);
        RpcSetName(pathName);
    }

    private void RpcSetName(string pathName)
    {
        OnNameChange(pathName);
    }

    private void OnNameChange(string newName)
    {
        Name = newName;
        var text = transform.Find("Text");
        text.GetComponent<TextMesh>().text = Name;

        text.transform.position = Points.Count % 2 == 0
                                ? Vector3.Lerp(Points[Points.Count / 2], Points[(Points.Count / 2) + 1], .5f)
                                : Points[(Points.Count / 2) + 1];
    }
}

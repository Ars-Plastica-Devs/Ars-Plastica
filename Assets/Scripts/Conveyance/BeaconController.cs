using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class BeaconController : NetworkBehaviour
{
    private readonly HashSet<string> m_PathNames = new HashSet<string>();
    private readonly SyncListString m_ConnectedBeacons = new SyncListString();

    public GameObject ConveyancePrefab;
    public SphereCollider DetectingCollider;
    [SyncVar(hook = "OnNameChange")]
    public string Name;

    private void Start()
    {
        SetDisplayedName();
    }

    [Server]
    public void DetectPaths(Dictionary<string, BeaconController[]> beaconsOnPaths)
    {
        m_ConnectedBeacons.Clear();

        //Get Beacon pairs on paths this beacon is connected to
        foreach (var kvp in beaconsOnPaths.Where(kvp => m_PathNames.Contains(kvp.Key)))
        {
            m_ConnectedBeacons.Add(kvp.Value[0] == this
                ? kvp.Value[1].Name
                : kvp.Value[0].Name);
        }
    }

    [Server]
    public void AddPath(string pathName)
    {
        m_PathNames.Add(pathName);
    }

    [Server]
    public void RemovePath(string pathName)
    {
        m_PathNames.Remove(pathName);
    }

    public List<string> GetBeacons()
    {
        return m_ConnectedBeacons.ToList();
    }

    private void OnNameChange(string val)
    {
        Name = val;
        SetDisplayedName();
    }

    private void SetDisplayedName()
    {
        var nameObj = transform.Find("Name");
        nameObj.GetComponent<TextMesh>().text = Name;
    }
}

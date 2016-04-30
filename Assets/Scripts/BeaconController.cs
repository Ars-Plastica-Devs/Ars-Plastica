using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class BeaconController : NetworkBehaviour
{
    private HashSet<GameObject> m_PathEndPoints = new HashSet<GameObject>();

    private readonly SyncListString m_PathNames = new SyncListString();

    public GameObject ConveyancePrefab;
    public SphereCollider DetectingCollider;
    [SyncVar(hook = "OnNameChange")]
    public string Name;

    private void Start()
    {
        SetDisplayedName();
    }

    /// <summary>
    /// Detect all path end points in range so they can be listed as valid paths to take from this location
    /// </summary>
    /// <param name="validPathObjects">A set of objects to restrict the detection to</param>
    [Server]
    public void DetectPaths(HashSet<GameObject> validPathObjects = null)
    {
        var endPoints = Physics.OverlapSphere(transform.position, DetectingCollider.radius * transform.localScale.x)
                                .Select(c => c.gameObject)
                                .Where(go => (validPathObjects == null || validPathObjects.Contains(go)) 
                                                && go.tag == "PathEndPoint");

        m_PathEndPoints = new HashSet<GameObject>(endPoints);
        m_PathNames.Clear();

        foreach (var pathEndPoint in m_PathEndPoints)
        {
            pathEndPoint.transform.parent = transform;
            var pathName = pathEndPoint.name;
            m_PathNames.Add(pathName);
        }
    }

    public List<string> GetPaths(bool withEndingDesignations = false)
    {
        return withEndingDesignations 
            ? m_PathNames.ToList() 
            : m_PathNames.Select(s => s.Split(' ')[0]).ToList();
    }

    private void OnNameChange(string val)
    {
        Name = val;
        SetDisplayedName();
    }

    private void SetDisplayedName()
    {
        var nameObj = transform.FindChild("Name");
        nameObj.GetComponent<TextMesh>().text = Name;
    }
}

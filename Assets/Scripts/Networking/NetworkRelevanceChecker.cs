using System.Collections.Generic;
using Assets.Octree;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkRelevanceChecker : NetworkBehaviour
{
    //Cache a list to hold objects in range - saves on memory allocations
    private static readonly List<Transform> CachedObjectList = new List<Transform>();
    private float m_LastUpdateTime;
    private NetworkIdentity m_NetworkIdentity;
    public int Range;
    public float UpdateInterval;
    public HashSet<NetworkConnection> RelevantTo = new HashSet<NetworkConnection>();

    private void Start()
    {
        UpdateInterval = UpdateInterval.Randomize(.01f);
        m_NetworkIdentity = GetComponent<NetworkIdentity>();
    }

    private void Update()
    {
        if (!NetworkServer.active)
            return;

        if (Time.time - m_LastUpdateTime > UpdateInterval)
        {
            m_NetworkIdentity.RebuildObservers(false);
            m_LastUpdateTime = Time.time;
        }
    }

    public override bool OnCheckObserver(NetworkConnection newObserver)
    {
        // this cant use newObserver.playerControllers[0]. must iterate to find a valid player.
        GameObject player = null;
        for (var i = 0; i < newObserver.playerControllers.Count; i++)
        {
            if (newObserver.playerControllers[i] == null || newObserver.playerControllers[i].gameObject == null)
                continue;

            player = newObserver.playerControllers[i].gameObject;
            break;
        }
        if (player == null)
            return false;

        var pos = player.transform.position;
        return (pos - transform.position).sqrMagnitude < Range * Range;
    }

    public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initial)
    {
        if (OctreeManager.Get(OctreeType.Player) == null)
        {
            Debug.Log("Could not find an Octree for the Player type");
            return false;
        }

        CachedObjectList.Clear();
        RelevantTo.Clear();
        OctreeManager.Get(OctreeType.Player).GetObjectsInRange(transform.position, CachedObjectList, Range);

        for (var i = 0; i < CachedObjectList.Count; i++)
        {
            var netID = CachedObjectList[i].GetComponent<NetworkIdentity>();
            if (netID == null)
                continue;

            RelevantTo.Add(netID.connectionToClient);
            observers.Add(netID.connectionToClient);
        }

        return true;
    }

    public override void OnSetLocalVisibility(bool vis)
    {
        var rends = GetComponentsInChildren<Renderer>();

        for (var i = 0; i < rends.Length; i++)
        {
            var rend = rends[i];
            rend.enabled = vis;
        }
    }
}

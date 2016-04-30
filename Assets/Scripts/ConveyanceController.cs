using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class ConveyanceController : NetworkBehaviour
{
    private readonly HashSet<GameObject> m_AttachedObjects = new HashSet<GameObject>();
    private Rigidbody m_Rigidbody;
    private List<Vector3> m_Points;
    private int m_NextPoint;

    [SerializeField]
    private string m_CurrentPath;
    public string CurrentPath {
        get { return m_CurrentPath; }
    }

    private bool m_ReversedPath;

    public float ProgressDistance = 3f;
    public float Speed = 20f;

    [Tooltip("The offset from the conveyances center to place an attached object")]
    public Vector3 AttachOffset = Vector3.zero;

    [SyncVar]
    public bool Running;
    public PathManager Manager;

    [SyncVar]
    public Vector3 Velocity;

    public int AttachedCount {
        get { return m_AttachedObjects.Count; }
    }

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        
        if (!isServer) return;

        Manager = GameObject.FindGameObjectWithTag("PathManager").GetComponent<PathManager>();
    }

    private void Update ()
    {
        //m_Rigidbody.velocity = Velocity;

        if (isClient)
        {
            foreach (var obj in m_AttachedObjects)
            {
                obj.transform.position = transform.position + AttachOffset;
            }
        }

        if (!Running || !isServer || m_Points == null || m_Points.Count < 2)
	        return;

	    var toPoint = m_Points[m_NextPoint] - transform.position;

        //Can progress to the next point
        if (toPoint.sqrMagnitude < (ProgressDistance * ProgressDistance))
        {
            m_NextPoint++;

            if (m_NextPoint >= m_Points.Count)
            {
                Running = false;
                //m_Rigidbody.velocity = Vector3.zero;
                Manager.NotifyOfConveyanceFinished(this);
            }
        }
        else
        {
            transform.position += toPoint.normalized * Speed * Time.deltaTime;
            //m_Rigidbody.velocity = toPoint.normalized * Speed;
        }

        foreach (var obj in m_AttachedObjects)
        {
            obj.transform.position = transform.position + AttachOffset;
        }
    }

    public void SetPath(string pathName, bool reversePath)
    {
        m_Points = new List<Vector3>(Manager.Paths[pathName]);

        m_ReversedPath = reversePath;
        if (m_ReversedPath)
            m_Points.Reverse();

        m_CurrentPath = pathName;
        m_NextPoint = 0;
    }

    public void RefreshPath()
    {
        m_Points = new List<Vector3>(Manager.Paths[m_CurrentPath]);

        if (m_ReversedPath)
            m_Points.Reverse();

        if (m_NextPoint >= m_Points.Count)
        {
            m_NextPoint = m_Points.Count;
        }
    }

    [Server]
    public void AttachObject(GameObject obj)
    {
        obj.transform.parent = transform;
        obj.transform.localPosition = AttachOffset;
        m_AttachedObjects.Add(obj);
        RpcAttachObject(obj);
    }

    [ClientRpc]
    private void RpcAttachObject(GameObject obj)
    {
        m_AttachedObjects.Add(obj);
    }

    [Server]
    public void DetachObject(GameObject obj)
    {
        obj.transform.parent = null;
        m_AttachedObjects.Remove(obj);
        RpcDetachObject(obj);
    }

    [ClientRpc]
    private void RpcDetachObject(GameObject obj)
    {
        obj.transform.parent = null;
        m_AttachedObjects.Remove(obj);
    }

    [Server]
    public void ServerStartRunning()
    {
        Running = true;
    }

    public void OnCollisionEnter(Collision coll)
    {
        if (Running)
            Physics.IgnoreCollision(GetComponent<Collider>(), coll.collider);
    }
}

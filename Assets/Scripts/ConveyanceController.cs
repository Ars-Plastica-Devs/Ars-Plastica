using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ConveyanceController : NetworkBehaviour
{
    private readonly HashSet<GameObject> m_AttachedObjects = new HashSet<GameObject>();
    private Rigidbody m_Rigidbody;
    private List<Vector3> m_Points;
    private int m_NextPoint;
    private int m_Direction = 1;
    private int m_AttachedCount;

    [SerializeField]
    private string m_CurrentPath;

    public float ProgressDistance = 3f;
    public float Speed = 20f;

    [Tooltip("The offset from the conveyances center to place an attached object")]
    public Vector3 AttachOffset = Vector3.zero;

    [SyncVar]
    public bool Running;
    public PathManager Manager;

    [SyncVar]
    public Vector3 Velocity;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void Update ()
    {
        m_Rigidbody.velocity = Velocity;

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
	        m_NextPoint += m_Direction;

	        if (m_NextPoint >= m_Points.Count)
	        {
	            m_Direction = -1;
	            m_NextPoint = m_Points.Count - 2;
	        }
            else if (m_NextPoint < 0)
            {
                m_Direction = 1;
                m_NextPoint = 1;

                if (m_AttachedObjects.Count == 0)
                    Running = false;
            }
	    }

        m_Rigidbody.velocity = toPoint.normalized * Speed;

        foreach (var obj in m_AttachedObjects)
        {
            obj.transform.position = transform.position + AttachOffset;
        }
    }

    public void SetPath(string pathName)
    {
        m_Points = Manager.Paths[pathName];
        m_CurrentPath = pathName;
        m_NextPoint = 0;
    }

    public void RefreshPath()
    {
        m_Points = Manager.Paths[m_CurrentPath];

        if (m_NextPoint >= m_Points.Count)
        {
            m_NextPoint = m_Points.Count;
        }
    }

    public void AttachObject(GameObject obj)
    {
        obj.transform.parent = transform;
        obj.transform.localPosition = AttachOffset;
        m_AttachedObjects.Add(obj);
    }

    public void DetachObject(GameObject obj)
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

using UnityEngine;

public class WorldBoundaryBox : MonoBehaviour
{
    public static WorldBoundaryBox Singleton;
    private readonly GameObject[] m_OctantCenters = new GameObject[8];

    public float Extent = 2500f;
    public Bounds Bounds;

    private void Start()
    {
        //if (Singleton == null)
            Singleton = this;

        Bounds = new Bounds(Vector3.zero, new Vector3(Extent, Extent, Extent));

        for (var i = 0; i < 8; i++)
        {
            m_OctantCenters[i] = transform.Find("Octant " + (i + 1)).gameObject;
            m_OctantCenters[i].transform.SetParent(transform);
        }

        var halfE = Extent / 2f;
        m_OctantCenters[0].transform.position = new Vector3(-halfE, halfE, halfE);
        m_OctantCenters[1].transform.position = new Vector3(halfE, halfE, halfE);
        m_OctantCenters[2].transform.position = new Vector3(halfE, halfE, -halfE);
        m_OctantCenters[3].transform.position = new Vector3(-halfE, halfE, -halfE);
        m_OctantCenters[4].transform.position = new Vector3(-halfE, -halfE, halfE);
        m_OctantCenters[5].transform.position = new Vector3(halfE, -halfE, halfE);
        m_OctantCenters[6].transform.position = new Vector3(halfE, -halfE, -halfE);
        m_OctantCenters[7].transform.position = new Vector3(-halfE, -halfE, -halfE);
    }

    public Vector3 GetOctantCenter(int i)
    {
        return m_OctantCenters[i - 1].transform.position;
    }

    public GameObject GetOctantObject(int i)
    {
        return m_OctantCenters[i - 1];
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || other.gameObject == null)
            return;

        //There are a few unexplained cases where this is called when the
        //other collider is still well within the world bounds, so we run
        //this check
        if (Mathf.Abs(other.transform.position.x) < Extent &&
            Mathf.Abs(other.transform.position.y) < Extent &&
            Mathf.Abs(other.transform.position.z) < Extent)
        {
            return;
        }

        var otherPos = WrapPositionInBounds(other.transform.position, Bounds, 10f);
        other.transform.position = otherPos;
    }

    public void BoundsCheck(GameObject other)
    {
        if (other == null)
            return;

        if (Mathf.Abs(other.transform.position.x) < Extent &&
            Mathf.Abs(other.transform.position.y) < Extent &&
            Mathf.Abs(other.transform.position.z) < Extent)
        {
            return;
        }

        var otherPos = WrapPositionInBounds(other.transform.position, Bounds, 10f);
        other.transform.position = otherPos;
    }

    public void OnValidate()
    {
        for (var i = 0; i < 8; i++)
        {
            m_OctantCenters[i] = transform.Find("Octant " + (i + 1)).gameObject;
            m_OctantCenters[i].transform.SetParent(transform);
        }

        var halfE = Extent / 2f;
        m_OctantCenters[0].transform.position = new Vector3(-halfE, halfE, halfE);
        m_OctantCenters[1].transform.position = new Vector3(halfE, halfE, halfE);
        m_OctantCenters[2].transform.position = new Vector3(halfE, halfE, -halfE);
        m_OctantCenters[3].transform.position = new Vector3(-halfE, halfE, -halfE);
        m_OctantCenters[4].transform.position = new Vector3(-halfE, -halfE, halfE);
        m_OctantCenters[5].transform.position = new Vector3(halfE, -halfE, halfE);
        m_OctantCenters[6].transform.position = new Vector3(halfE, -halfE, -halfE);
        m_OctantCenters[7].transform.position = new Vector3(-halfE, -halfE, -halfE);
    }

    private static Vector3 WrapPositionInBounds(Vector3 pos, Bounds bounds, float extraPad = 0f)
    {
        var x = pos.x;
        var y = pos.y;
        var z = pos.z;

        var wx = Mathf.Abs(bounds.size.x - Mathf.Abs(pos.x) * 2f) - extraPad;
        var wy = Mathf.Abs(bounds.size.y - Mathf.Abs(pos.y) * 2f) - extraPad;
        var wz = Mathf.Abs(bounds.size.z - Mathf.Abs(pos.z) * 2f) - extraPad;

        var clampLimit = bounds.extents.x * .95f;
        wx = Mathf.Clamp(wx, -clampLimit, clampLimit);
        wy = Mathf.Clamp(wy, -clampLimit, clampLimit);
        wz = Mathf.Clamp(wz, -clampLimit, clampLimit);

        if (pos.x < -bounds.extents.x)
            x = wx;
        else if (pos.x > bounds.extents.x)
            x = -wx;

        if (pos.y < -bounds.extents.y)
            y = wy;
        else if (pos.y > bounds.extents.y)
            y = -wy;

        if (pos.z < -bounds.extents.z)
            z = wz;
        else if (pos.z > bounds.extents.z)
            z = -wz;

        return new Vector3(x, y, z);
    }
}


using UnityEngine;
using UnityEngine.Networking;

public class Rotation : NetworkBehaviour
{
    private float m_FullRotationTime;
    private DayClock m_Dayclock;

    public float DaysForFullRotation = 4f;
	public Vector3 FromRotation = new Vector3 (0, 0, 0);
	public Vector3 ToRotation = new Vector3 (0, 360, 0);

	void Start ()
	{
	    if (!isServer) return;

		m_Dayclock = (DayClock) FindObjectOfType (typeof(DayClock));
		m_FullRotationTime = m_Dayclock.DaysToSeconds (DaysForFullRotation);
		transform.rotation = Quaternion.Euler(FromRotation);
	}

	void Update ()
	{
	    if (!isServer) return;

		m_FullRotationTime = m_Dayclock.DaysToSeconds (DaysForFullRotation);
		
		var r = Quaternion.AngleAxis ((Vector3.Distance(FromRotation, ToRotation) / m_FullRotationTime) * Time.deltaTime, ToRotation);
		transform.rotation = transform.rotation * r;
	}
}


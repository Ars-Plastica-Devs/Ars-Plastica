using UnityEngine;
using System.Collections;

public class Rotation : MonoBehaviour
{

	public float daysForFullRotation = 4f;
	public Vector3 fromRotation = new Vector3 (0, 0, 0);
	public Vector3 toRotation = new Vector3 (0, 360, 0);

	private float fullRotationTime;
	private DayClock dayclock;


	void Start ()
	{
		dayclock = (DayClock) FindObjectOfType (typeof(DayClock));
		fullRotationTime = dayclock.DaysToSeconds (daysForFullRotation);
		transform.rotation = Quaternion.Euler(fromRotation);
	}

	void Update ()
	{
		fullRotationTime = dayclock.DaysToSeconds (daysForFullRotation);
		
		Quaternion _r = Quaternion.AngleAxis ((Vector3.Distance(fromRotation, toRotation) / fullRotationTime) * Time.deltaTime, toRotation);
		transform.rotation = transform.rotation * _r;

	}
}


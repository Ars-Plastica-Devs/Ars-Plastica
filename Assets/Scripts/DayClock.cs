using UnityEngine;
using System.Collections;

/*
 * DayClock
		• For keeping track of time.
		• Can set length of a game 'day' in seconds (how long it takes for the directional light attached to rotate a full 360)
		• Convert Days <-> Seconds.
		• Currently attached to a Directional Light.

	TODO: Could be attached to empty game object and hold references to multiple directional lights that rotate independently.

 * */
public class DayClock : MonoBehaviour
{

	public float daylength = 24f; //aka time in seconds
	public float hour = 0f;

	public float startTime;

	public Vector3 fromRotation = new Vector3 (0, 0, 0);
	public Vector3 toRotation = new Vector3 (360, 0, 0);


	// Use this for initialization
	void Start ()
	{
		startTime = Time.time;
		transform.rotation = Quaternion.Euler(fromRotation);
	}
	
	// Update is called once per frame
	void Update ()
	{
		hour = Time.time - startTime;
		if (hour > daylength) {
			hour = 0f;
		}

		Quaternion _r = Quaternion.AngleAxis ((toRotation.x / daylength) * Time.deltaTime, Vector3.right);

		transform.rotation = transform.rotation * _r;
	}


	public bool isDay() {
		return true;
	}

	/*
	 * Accepts float representation of seconds.
	 * Returns number of days in given time period.
	 * */
	public float secondsToDays (float time) {

		return time / daylength;
	}

	/*
	 * Accepts float representation of cycles.
	 * Returns days in seconds.
	 * */
	public float DaysToSeconds(float days) {
		return daylength * days;
	}


}


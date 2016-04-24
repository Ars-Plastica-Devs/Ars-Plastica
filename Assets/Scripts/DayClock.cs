using UnityEngine;

/*
 * DayClock
		• For keeping track of time.
		• Can set length of a game 'day' in seconds (how long it takes for the directional light attached to rotate a full 360)
		• Convert Days <-> Seconds.
		• Currently attached to a Directional Light.

	TODO: Could be attached to empty game object and hold references to multiple directional lights that rotate independently.
    NOTE: Multiple directional lights will not give us the binary star effect we are looking for - only one star will show in the sky.

 * */
public class DayClock : MonoBehaviour
{
    private float m_DayStart;
	public float Daylength = 24f; //aka time in seconds
	public float Hour;

	public Vector3 FromRotation = new Vector3 (0, 0, 0);
	public Vector3 ToRotation = new Vector3 (360, 0, 0);


	// Use this for initialization
	void Start ()
	{
	    m_DayStart = Time.time;
        transform.rotation = Quaternion.Euler(FromRotation);
	}
	
	// Update is called once per frame
	void Update ()
	{
		Hour = Time.time - m_DayStart;
		if (Hour > Daylength) {
			Hour = 0f;
		    m_DayStart = Time.time;
		}

		var r = Quaternion.AngleAxis((ToRotation.x / Daylength) * Time.deltaTime, Vector3.right);

		transform.rotation = transform.rotation * r;
	}


    public float GetTimeOfDay()
    {
        return Hour;
    }

    public bool IsDay()
    {
		return true;
	}

	/*
	 * Accepts float representation of seconds.
	 * Returns number of days in given time period.
	 * */
	public float SecondsToDays (float time) {

		return time / Daylength;
	}

	/*
	 * Accepts float representation of cycles.
	 * Returns days in seconds.
	 * */
	public float DaysToSeconds(float days) {
		return Daylength * days;
	}


}


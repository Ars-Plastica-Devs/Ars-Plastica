using UnityEngine;
using UnityEngine.Networking;

/*
 * DayClock
		• For keeping track of time.
		• Can set length of a game 'day' in seconds (how long it takes for the directional light attached to rotate a full 360)
		• Convert Days <-> Seconds.
		• Currently attached to a Directional Light.

	TODO: Could be attached to empty game object and hold references to multiple directional lights that rotate independently.
    NOTE: Multiple directional lights will not give us the binary star effect we are looking for - only one star will show in the sky.
    NOTE: The above note might only apply because of the sky-box being used

 * */
public class DayClock : NetworkBehaviour
{
    public static DayClock Singleton;
    private float m_StartTime;
	[SyncVar] public float Daylength = 24f; //time in seconds
	[SyncVar] public float Hour;

	public Vector3 FromRotation = new Vector3 (0, 0, 0);
	public Vector3 ToRotation = new Vector3 (360, 0, 0);

    public delegate void TimeEventDelegate();
    public event TimeEventDelegate OnNight;
    public event TimeEventDelegate OnDay;

    private void Awake()
    {
        Singleton = this;
    }

	private void Start ()
	{
        m_StartTime = Time.time;
        //transform.rotation = Quaternion.Euler(FromRotation);

	    if (!isServer)
	        return;

        Daylength = DataStore.GetFloat(Data.DayLength);
    }

    private void Update ()
    {
		//var r = Quaternion.AngleAxis((ToRotation.x / Daylength) * Time.deltaTime, Vector3.right);
		//transform.rotation = transform.rotation * r;

        if (!isServer)
            return;

	    var t = Time.time - m_StartTime;
	    var secondsOfDay = t % Daylength;
        var wasDay = IsDay();
	    Hour = (secondsOfDay / Daylength) * 24f;

        if (wasDay && !IsDay())
        {
            if (OnNight != null)
                OnNight();
        }
        else if (!wasDay && IsDay())
        {
            if (OnDay != null)
                OnDay();
        }
    }


    public float GetTimeOfDay()
    {
        return Hour;
    }

    public bool IsDay()
    {
		return Hour > 6f && Hour < 18f;
	}

	/*
	 * Accepts float representation of seconds.
	 * Returns number of days in given time period.
	 * */
	public float SecondsToDays (float time)
    {
		return time / Daylength;
	}

	/*
	 * Accepts float representation of cycles.
	 * Returns days in seconds.
	 * */
	public float DaysToSeconds(float days)
    {
		return Daylength * days;
	}

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        if (Daylength != DataStore.GetFloat(Data.DayLength))
        {
            DataStore.Set(Data.DayLength, Daylength);
        }
    }
}


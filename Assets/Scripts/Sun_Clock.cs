using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class Sun_Clock : NetworkBehaviour {

	[SyncVar] private Quaternion syncSunRotation;

	public float DayLength = 24f;
	private float lerpRate;

	
	public DateTime beginTime;
	public Color SunColor;
	public float LightIntensity;


	// Use this for initialization
	void Start () {
		beginTime = DateTime.Now;
		lerpRate = 24 / DayLength;

	}
	
	// TODO: Change light intensity/color based on time of day.
	void Update () {
		if (isServer) {
//		light.intensity = Math.Sin ((double)Time.deltaTime * DayLength);
			this.transform.RotateAround (Vector3.right, Time.deltaTime / DayLength);
		}
	}
}

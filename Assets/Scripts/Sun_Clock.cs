using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class Sun_Clock : NetworkBehaviour {

	[SyncVar] private Quaternion syncSunRotation;

	public float DayLength = 24f;
	private float lerpRate;

	public DateTime beginTime;


	// Use this for initialization
	void Start () {
		beginTime = DateTime.Now;
		lerpRate = 24 / DayLength;
	}
	
	// Update is called once per frame
	void Update () {
		
		this.transform.RotateAround (Vector3.right, Time.deltaTime/DayLength);
	}
}

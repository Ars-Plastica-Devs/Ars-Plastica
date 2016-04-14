﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AIEntity_Herbivore : AIEntity
{	

	DayClock dayclock;

	//	Possible States for Herbivore
	private AIState youngState;
	private AIState teenState;
	private AIState adultState;

	private AIState huntingState;
	private AIState idleState;
	private AIState deadState;

	[SyncVar] public Vector3 scale;

	[SyncVar] public float youngSize = 1f;
	[SyncVar] public float teenSize = 2f;
	[SyncVar] public float adultSizeMin = 3f;
	[SyncVar] public float adultSizeMax = 3.5f;

	[SyncVar] public float daysAsYoung = 5f;
	[SyncVar] public float daysAsTeen = 3f;
	[SyncVar] public float lifeSpan = 100f;	

	[SyncVar] private float finalAdultSize;
	[SyncVar] private Vector3 adultScale;
	[SyncVar] private Vector3 teenScale;
	[SyncVar] private Vector3 initialHeightScale;
	[SyncVar] private float currentGrowTime = 0f;
	[SyncVar] private float totalGrowTime = 0f;

	[SyncVar] public float DaysOld = 0f;

	

	public void Start ()
	{
		if (!isServer)
			return;
		spawnTime = Time.time;
		dayclock = (DayClock) FindObjectOfType (typeof(DayClock));

		youngState = new AIState ("young", YoungStateStart, YoungStateUpdate, YoungStateEnd);
		teenState = new AIState ("teen", TeenStateStart, TeenStateUpdate, TeenStateEnd);
		adultState = new AIState ("adult", AdultStateStart, AdultStateUpdate, AdultStateEnd);

		currentState = youngState;


		finalAdultSize = Random.Range (adultSizeMin, adultSizeMax);
		Renderer rend = GetComponent<Renderer> ();
		if (!rend) {
			currentState = deadState;
		} else {
//			Debug.Log (rend.bounds.extents.y + " " + rend.bounds.size.y);
			float currentSize = rend.bounds.size.y;
			float initialScale = youngSize / currentSize;
			initialHeightScale = transform.localScale * initialScale;
			teenScale = Vector3.one * teenSize;
			adultScale = Vector3.one * finalAdultSize;
			this.transform.localScale = initialHeightScale;
			scale = this.transform.localScale;
		}

		SwitchState (currentState);
	}

	void Update ()
	{
		if (isClient) {
			this.transform.localScale = scale;
		}

		if (isServer) {
			if (currentState != null) {
				currentState.UpdateCallback ();

			}
			DaysOld = dayclock.secondsToDays (Time.time - spawnTime);
		}
	}
		

	/*
	Young State
	*/
	void YoungStateStart() {
//		Debug.Log ("Starting Young State");
		currentGrowTime = 0f;
		totalGrowTime = dayclock.DaysToSeconds (daysAsYoung);

	}

	void YoungStateUpdate() {
		if (!growLerp (initialHeightScale, teenScale)) {
			SwitchState (teenState);
		}
	}

	void YoungStateEnd() {
//		Debug.Log ("Ending Young State");
	}

	/*
	Teen State

	*/

	void TeenStateStart() {
//		Debug.Log ("Starting Teen State");
		currentGrowTime = 0f;
		totalGrowTime = dayclock.DaysToSeconds (daysAsTeen);
	}

	void TeenStateUpdate() {
		if (!growLerp (teenScale, adultScale)) {
			SwitchState (adultState);
		}
	}

	void TeenStateEnd() {
//		Debug.Log ("Ending Teen State");
	}

	/*
	Adult State

	*/

	void AdultStateStart() {
//		Debug.Log ("Starting Adult State");
	}

	void AdultStateUpdate() {
		
	}

	void AdultStateEnd() {
//		Debug.Log ("Ending Adult State");
	}


	//returns true if still growing, else if done then false
	bool growLerp(Vector3 initialScale, Vector3 endScale) {
		currentGrowTime += Time.deltaTime;
		if (currentGrowTime > totalGrowTime) {
			currentGrowTime = totalGrowTime;
			return false;
		}

		float lerpProgress = currentGrowTime / totalGrowTime;
//		transform.localScale = Vector3.Lerp (initialScale, endScale, lerpProgress);
		scale = Vector3.Lerp (initialScale, endScale, lerpProgress);
		return true;
	}

}

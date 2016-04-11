using UnityEngine;
using System.Collections;

public class AIEntity_Plant : AIEntity
{	

	//Nodules to spawn
	public Transform[] nodules;

	//	Possible States for Plant
	private AIState growState;
	private AIState idleState;
	private AIState deadState;

	/*
	GrowState vars.
	*/
	public float totalGrowDays = 3f;
	public float finalGrowSizeMin = 25f;
	public float finalGrowSizeMax = 55f;
	public float lifeSpan = 20f;
	public float totalShrinkTimeSeconds = 10f;

	private float totalGrowTime;
	private float currentGrowTime;
	private float pausedGrowTime; // Don't grow at night
	private float growFinalSize;
	private Vector3 initialHeightScale;
	private Vector3 finalHeightScale;
	private Renderer rend;
	private DayClock dayclock;
	private bool releasedSporesToday = false;
	private float lastDaySporesReleased = 0;

		
	public void Start ()
	{
		spawnTime = Time.time;
		dayclock = (DayClock) FindObjectOfType (typeof(DayClock));

		growState = new AIState ("grow", GrowStateStart, GrowStateUpdate, GrowStateEnd);
		idleState = new AIState ("idle", IdleStateStart, IdleStateUpdate, IdleStateEnd);
		deadState = new AIState ("adult", DeadStateStart, DeadStateUpdate, DeadStateEnd);

		SwitchState (growState);
	}

	

	void Update ()
	{
		
		if (currentState != null) {
			if (dayclock == null) {
				dayclock = (DayClock) FindObjectOfType (typeof(DayClock));
			}
			currentState.UpdateCallback ();
		}
	}

	void emitNodule() {
		if (nodules.Length < 1)
			return;
		Transform newObj;
		Vector3 v3;
		for (int i = 0; i < 3; i++) {
			v3 = new Vector3 (this.transform.position.x, this.rend.bounds.center.y + this.rend.bounds.extents.y, this.transform.position.z); 
			newObj = (Transform)Instantiate (nodules [0], v3, Quaternion.identity);

		}
	}
		
	void GrowStateStart() {
//		Debug.Log ("GrowStateStart");

		growFinalSize = Random.Range (finalGrowSizeMin, finalGrowSizeMax);
		rend = GetComponent<Renderer>();
		if (!rend) {
			SwitchState (deadState);
		}
		float currentHeight = rend.bounds.size.y;
		float initialHeight = growFinalSize / (2 * totalGrowDays);
		initialHeightScale = new Vector3(1, initialHeight / currentHeight, 1);
		finalHeightScale = new Vector3 (1, growFinalSize, 1);
		this.transform.localScale = initialHeightScale;
		currentGrowTime = 0f;
		totalGrowTime = dayclock.DaysToSeconds (totalGrowDays);
	}

	void GrowStateUpdate () {
		currentGrowTime = Time.time - spawnTime;
		if (currentGrowTime > totalGrowTime) {
			currentGrowTime = totalGrowTime;
			SwitchState (idleState);
		}
		if (dayclock.isDay ()) {

		}
		float lerpProgress = currentGrowTime / totalGrowTime;
		transform.localScale = Vector3.Lerp (initialHeightScale, finalHeightScale, lerpProgress);
	
	}

	void GrowStateEnd() {
	}

	void IdleStateStart () {
//		Debug.Log ("IdleStateStart");

	}

	void IdleStateUpdate() {
		if (dayclock.secondsToDays (Time.time - this.spawnTime) > lifeSpan) {
			//shrink
			currentGrowTime = Time.time - spawnTime;
			if (currentGrowTime >= totalGrowTime) {
				SwitchState (deadState);
			}
			float lerpProgress = currentGrowTime / totalGrowTime;
			transform.localScale = Vector3.Lerp (finalHeightScale, Vector3.zero, lerpProgress);
		} else {
			//release spores
			float today = Mathf.Floor (dayclock.secondsToDays (Time.time - this.spawnTime));
			if (today > lastDaySporesReleased) {
//				Debug.Log ("Release spores: Day " + today);
				emitNodule ();
				lastDaySporesReleased = today;
			}
		}
	}

	void IdleStateEnd() {
	}

	void DeadStateStart () {
//		Debug.Log ("DeadStateStart");

	}

	void DeadStateUpdate() {

	}

	void DeadStateEnd() {
//		Destroy (this);
	}
}


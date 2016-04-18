using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public abstract class AIEntity : NetworkBehaviour
{
	[SyncVar] public float health = 100;
	[SyncVar] public float DaysOld = 0f;

	public DayClock dayclock;

	[SerializeField]
	internal float spawnTime;


	virtual public void Start ()
	{
		if (!isServer) {
			GetComponentInChildren<RAIN.Core.AIRig> ().enabled = false;
		} else {
			dayclock = (DayClock)FindObjectOfType (typeof(DayClock));
			spawnTime = Time.time;
		}
	}

	virtual public void Update() {
		if (isServer) {
			DaysOld = dayclock.secondsToDays (Time.time - spawnTime);
		}
	}

	virtual public void doDamage(float damage)
	{
		health -= damage;
	}

	/*
	Returns true if AI should die.
	*/
	virtual public bool isDead() {
		if (health <= 0) {
			return true;
		} else {
			return false;
		}
	}
}


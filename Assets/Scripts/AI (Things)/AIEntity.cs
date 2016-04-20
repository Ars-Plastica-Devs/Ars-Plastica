using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/*
 * Base class for all networked AI.
 * 
 * 
 * */
public abstract class AIEntity : NetworkBehaviour
{
	[SyncVar] public float health = 100;
	[SyncVar] public float DaysOld = 0f;
	[SyncVar] public float spawnTime;

	public DayClock dayclock;


	virtual public void Start ()
	{
		if (isServer) {
			//only enable AI on server.
			dayclock = (DayClock)FindObjectOfType (typeof(DayClock));
			spawnTime = Time.time;
		} else {
			GetComponentInChildren<RAIN.Core.AIRig> ().enabled = false;
		}
	}

	virtual public void Update() {
		if (isServer) {
			DaysOld = dayclock.secondsToDays (Time.time - spawnTime);
		}
	}

	//Reduces health by amount. (negative amount heals).
	virtual public void doDamage(float damage)
	{
		health -= damage;
	}

	/*
	Returns true if AI should die.
	*/
	virtual public bool checkHealth() {
		if (health <= 0) {
			return true;
		} else {
			return false;
		}
	}
}


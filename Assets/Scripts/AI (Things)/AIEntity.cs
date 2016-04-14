using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;


public abstract class AIEntity : NetworkBehaviour {

	public float health = 100;
	//don't want these in inspector but still accessible by other scripts
	internal float spawnTime;
	internal AIState currentState;

	public void SwitchState(AIState state) {
		if (currentState != null) {
			currentState.EndCallback ();
		}
		currentState = state;
		currentState.StartCallback ();
	}

	void Spawn(Vector3 coordinate) {
		
	}

	void doDamage(float damage) {
		health -= damage;	
	}
}


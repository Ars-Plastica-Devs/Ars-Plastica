using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/*
 * AIEntity script.  Is abstract, so needs to be inherited by another script.
 * */
public abstract class AIEntityOld : NetworkBehaviour {

	public float health = 100;
	internal float spawnTime;

	//a FiniteStateMachine
	internal ArrayList currentStates;

	virtual public void Start()
	{
		currentStates = new ArrayList ();
		spawnTime = Time.time;
	}

	virtual public void Update()
	{
		foreach (AIState state in currentStates) {
			state.UpdateCallback ();
		}
	}
		
	//Start a state
	public void StartState(AIState state)
	{
		currentStates.Add (state);
		state.StartCallback ();
	}

	//Ends a state and removes from currentStates Array
	public void EndState(AIState state)
	{
		foreach(AIState _s in currentStates) {
			if (_s.Equals (state)) {
				_s.EndCallback ();
				currentStates.Remove (_s);
				break;
			}
		}
	}
		
		
	void doDamage(float damage)
	{
		health -= damage;	
	}
}


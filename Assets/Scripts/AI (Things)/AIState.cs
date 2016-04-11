using UnityEngine;
using System.Collections;
/*
State that an AI can be in.  A class that uses AIState is responsible for creating it's own callbacks. 
This class is just a way to keep track of what the current state is and which functions should be called.

*/
public class AIState {
	public string stateName;
	public float startTime;
	public float cyclesInState;

	public DayClock dayclock;

	public delegate void StateDelegate();
	public StateDelegate StartCallback;
	public StateDelegate UpdateCallback;
	public StateDelegate EndCallback;

	public AIState(string state, StateDelegate startcb, StateDelegate updatecb, StateDelegate endcb) {
		dayclock = (DayClock) MonoBehaviour.FindObjectOfType (typeof(DayClock));
		this.stateName = state;
		this.StartCallback = startcb;
		this.UpdateCallback = updatecb;
		this.EndCallback = endcb;
		this.startTime = Time.time;
	}


	public float daysInState () {
		return dayclock.secondsToDays (Time.time - startTime);
	}

	public float timeInState() {
		return Time.time - startTime;
	}

	public override bool Equals (object obj)
	{
		AIState temp = (AIState)obj;
		return temp.stateName == this.stateName;
	}

}


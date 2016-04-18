using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;
using RAIN.Serialization;
using RAIN.Representation;

[RAINAction]
public class WaitDays : RAINAction
{

	public float seconds;
	public float startTime;

	public Expression DaysToWait;

	public override void Start(RAIN.Core.AI ai)
	{
		base.Start(ai);
		if (!float.TryParse (DaysToWait.ExpressionAsEntered, out seconds)) {
			Debug.Log ("Could not parse DaysToWait in WaitDays node");
			seconds = 0;
		} else {
			DayClock dayclock = MonoBehaviour.FindObjectOfType<DayClock> ();
			if (dayclock != null) {
				seconds = dayclock.DaysToSeconds (seconds);

			} 
		}
		startTime = Time.time;
	}

	public override ActionResult Execute(RAIN.Core.AI ai)
	{
		if (Time.time - startTime > seconds) {
			return ActionResult.SUCCESS;

		} else {
			return ActionResult.RUNNING;
		}

	}

	public override void Stop(RAIN.Core.AI ai)
	{
		base.Stop(ai);
	}
}
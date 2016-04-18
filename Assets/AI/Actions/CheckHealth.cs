using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;

[RAINAction]
public class CheckHealth : RAINAction
{
	AIEntity entity;

	public override void Start(RAIN.Core.AI ai)
	{
		base.Start(ai);
		entity = ai.Body.GetComponent<AIEntity> ();
	}

	public override ActionResult Execute(RAIN.Core.AI ai)
	{

		if (entity.isDead ()) {
			return ActionResult.FAILURE;
		} else {
			return ActionResult.SUCCESS;
		}
	}


	public override void Stop(RAIN.Core.AI ai)
	{
		base.Stop(ai);
	}
}
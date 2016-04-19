using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;

/*
 * Grow continuously rather than in discrete steps (see GrowStep)
 * */
[RAINAction]
public class Grow : RAINAction
{
	
	AIEntity_Plant plant;

	public override void Start(RAIN.Core.AI ai)
	{
		base.Start(ai);
		
		plant = ai.Body.GetComponent<Plant_001> ();
		ai.WorkingMemory.SetItem<bool> ("finishedGrowing", false);
	}

	public override ActionResult Execute(RAIN.Core.AI ai)
	{

		if (!plant.growLerp()) {
			ai.WorkingMemory.SetItem<bool> ("finishedGrowing", true);
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
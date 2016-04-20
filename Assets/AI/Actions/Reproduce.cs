using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;

[RAINAction]
public class Reproduce : RAINAction
{
	AIEntity_Animal animal;
    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
		animal = ai.Body.GetComponent<AIEntity_Animal> ();
		if (animal) {
			animal.reproduce ();
		}
	
    }

    public override ActionResult Execute(RAIN.Core.AI ai)
    {
        return ActionResult.SUCCESS;
    }

    public override void Stop(RAIN.Core.AI ai)
    {
        base.Stop(ai);
    }
}
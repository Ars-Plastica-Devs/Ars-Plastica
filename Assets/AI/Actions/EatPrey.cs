using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;

[RAINAction]
public class EatPrey : RAINAction
{
	GameObject preyTarget;
    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
		AIEntity_Animal animal = ai.Body.GetComponent<AIEntity_Animal> ();
		preyTarget = ai.WorkingMemory.GetItem<GameObject> ("preyTarget");
		if (preyTarget != null && animal != null) {
			animal.eat (preyTarget);
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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;

[RAINAction]
public class EatNodule : RAINAction
{
	GameObject noduleTarget;
    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
		Herbivore_001 herb = ai.Body.GetComponent<Herbivore_001> ();
		noduleTarget = ai.WorkingMemory.GetItem<GameObject> ("noduleTarget");
		if (noduleTarget != null && herb != null) {
			herb.eat (noduleTarget);
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
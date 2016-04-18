using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;

[RAINAction]
public class ReleaseNodules : RAINAction
{

	Plant_001 plant;

    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
		plant = ai.Body.GetComponent<Plant_001> ();
    }

    public override ActionResult Execute(RAIN.Core.AI ai)
    {
		plant.emitNodule ();
        return ActionResult.SUCCESS;
    }

    public override void Stop(RAIN.Core.AI ai)
    {
        base.Stop(ai);
    }
}
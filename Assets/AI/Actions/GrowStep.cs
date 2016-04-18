using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;
using RAIN.Representation;

[RAINAction]
public class GrowStep : RAINAction
{
	Herbivore_001 entity;

	public Expression GrowthStage;
    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
		entity = ai.Body.GetComponent<Herbivore_001> ();
		
    }

    public override ActionResult Execute(RAIN.Core.AI ai)
    {
		if (GrowthStage != null) {
			entity.Grow (GrowthStage.ExpressionAsEntered);
		}
        return ActionResult.SUCCESS;
    }

    public override void Stop(RAIN.Core.AI ai)
    {
        base.Stop(ai);
    }
}
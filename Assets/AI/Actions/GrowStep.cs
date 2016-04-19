using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;
using RAIN.Representation;

/*
 * Grow in discrete steps rather than continuous (see Grow)
 * */
[RAINAction]
public class GrowStep : RAINAction
{
	AIEntity_Animal entity;

	public Expression GrowthStage;
    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
		entity = ai.Body.GetComponent<AIEntity_Animal> ();
		
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
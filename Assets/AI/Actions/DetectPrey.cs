using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;
using RAIN.Perception.Sensors;
using RAIN.Entities.Aspects;

[RAINAction]
public class DetectPrey : RAINAction
{
	RAINSensor preySensor;
	public override void Start(RAIN.Core.AI ai)
	{
		base.Start(ai);
		preySensor = ai.Senses.GetSensor("Prey Sensor");
	}

	public override ActionResult Execute(RAIN.Core.AI ai)
	{
		//only find new target if target is null
		if (ai.WorkingMemory.GetItem<GameObject> ("preyTarget") == null && preySensor != null) {
			preySensor.Sense ("Herbivore001", RAINSensor.MatchType.ALL);
			IList<RAINAspect> noduleAspects = preySensor.Matches;
			if (noduleAspects.Count == 0)
				return ActionResult.SUCCESS;
			ai.WorkingMemory.SetItem ("preyTarget", noduleAspects [Random.Range (0, noduleAspects.Count - 1)].Entity.Form);
		}

		return ActionResult.SUCCESS;
	}

    public override void Stop(RAIN.Core.AI ai)
    {
        base.Stop(ai);
    }
}
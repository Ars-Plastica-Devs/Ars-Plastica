using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RAIN.Action;
using RAIN.Core;
using RAIN.Perception.Sensors;
using RAIN.Entities.Aspects;

[RAINAction]
public class DetectNodule : RAINAction
{
	RAINSensor noduleSensor;
    public override void Start(RAIN.Core.AI ai)
    {
        base.Start(ai);
		noduleSensor = ai.Senses.GetSensor("Nodule Sensor");
    }

    public override ActionResult Execute(RAIN.Core.AI ai)
    {
		//only find new target if target is null
		if (ai.WorkingMemory.GetItem<GameObject> ("preyTarget") == null) {
			noduleSensor.Sense ("Nodule", RAINSensor.MatchType.ALL);
			IList<RAINAspect> noduleAspects = noduleSensor.Matches;
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
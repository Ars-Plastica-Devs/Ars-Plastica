using RAIN.Core;
using RAIN.Entities.Aspects;
using RAIN.Serialization;
using System;
using System.Collections.Generic;

namespace RAIN.Perception.Sensors.Filters
{
	[RAINElement ("Random Target Filter"), RAINSerializableClass]
	public class RandomTargetFilter : RAINSensorFilter
	{
		//
		// Fields
		// Example below, can be replaced with whatever.
//		[RAINSerializableField (Visibility = FieldVisibility.Show, ToolTip = "The number of Aspects to keep")]
//		public int countX = 1;


		[RAINNonSerializableField]
		private RAINAspect[] results = new RAINAspect[1];

		//
		// Methods
		//
		public override void Filter (RAINSensor aSensor, List<RAINAspect> aValues)
		{
			aSensor.AI.WorkingMemory.Clear ();

			if (aValues.Count > 0) {
				Random random = new Random ();
				RAINAspect[] newResult = { aValues [random.Next (0, aValues.Count - 1)] };
				this.results = newResult;
			}
		}
	}
}

using RAIN.Core;
//using RAIN.Perception.Sensors;
using RAIN.Entities.Aspects;
using RAIN.Serialization;
using System;
using System.Collections.Generic;
using UnityEngine;

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
			RAINAspect newResult;
			using (List<RAINAspect>.Enumerator enumerator = aValues.GetEnumerator ()) {
				IL_E7:
				while (enumerator.MoveNext ()) {
					RAINAspect current = enumerator.Current;
					if (current != null) {
						newResult = current;
						enumerator.Dispose ();
						aValues.Clear ();
						aValues.Add (newResult);
					}
				}
			}


//			if (aValues.Count > 0) {
//				RAINAspect newResult =  aValues [UnityEngine.Random.Range(0, aValues.Count-1)] ;
////				aSensor.AI.WorkingMemory.SetItem ("preyTarget", newResult [0].Position);
//				aValues.Clear();
//				aValues.Add (newResult);
////				Debug.Log (aSensor.AI.WorkingMemory.GetItem ("preyTarget"));
////				results [0] = newResult [0];
//			}
		}
	}
}

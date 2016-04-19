using UnityEngine;
using System.Collections;

public class Carnivore_001 : AIEntity_Animal
{

	override public void Start() {
		base.Start ();
	}


	override public bool checkHealth() {

		float daysSinceEaten = dayclock.secondsToDays (Time.time - lastTimeEaten);
		if (daysSinceEaten > numDaysWithoutFood) {
			doDamage (30);
			daysSinceEaten = Time.time;
		}
		//case: Old age
		if (DaysOld > lifeSpanInDays) {
			return true;
		}
		//case: Health < 0
		if (health <= 0) {
			return true;
		}	

		return false;
	}
}


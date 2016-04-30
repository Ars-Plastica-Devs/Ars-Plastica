using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Carnivore_001 : AIEntity_Animal
{
	public int numAnimalsEatenBeforeReproduce = 2;
	public int numAnimalsEaten;

	override public void Start() {
		base.Start ();
		numAnimalsEaten = 0;
	}

	override public void reproduce() {
		//if more time has passed since daysBetweenReproduction (in seconds) then animal should reproduce.
		bool shouldReproduce = ((Time.time - timeOfLastReproduction) > (dayclock.DaysToSeconds(daysBetweenReproduction))) ? true : false;
		if (this.DaysOld > daysOldUntilReproduction && this.health >= 100 && shouldReproduce) {
			if (numAnimalsEaten >= numAnimalsEatenBeforeReproduce) {
				Carnivore_001 newHerb = Instantiate (this);
				newHerb.transform.SetParent (this.transform.parent);
				NetworkServer.Spawn (newHerb.gameObject);
				timeOfLastReproduction = Time.time;
				numAnimalsEaten = 0;
			} else {
				numAnimalsEaten++;
			}
		}
	}

	override public bool checkHealth() {

		float daysSinceEaten = dayclock.SecondsToDays (Time.time - lastTimeEaten);
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


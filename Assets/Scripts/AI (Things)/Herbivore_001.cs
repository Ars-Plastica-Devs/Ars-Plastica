using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Herbivore_001 : AIEntity_Animal
{
	AIEcosystem ecosystem;

	override public void Start() {
		base.Start ();
		ecosystem = FindObjectOfType<AIEcosystem> ();
	}

	override public void eat(GameObject obj) {
		lastTimeEaten = Time.time;
		ecosystem.removeNodule ();
		Destroy (obj);
	}

	override public void reproduce() {
		//if more time has passed since daysBetweenReproduction (in seconds) then animal should reproduce.
		bool shouldReproduce = ((Time.time - timeOfLastReproduction) > (dayclock.DaysToSeconds(daysBetweenReproduction))) ? true : false;
		if (this.DaysOld > daysOldUntilReproduction && this.health >= 100 && shouldReproduce) {
			Herbivore_001 newHerb = Instantiate (this);
			newHerb.transform.SetParent (this.transform.parent);
			NetworkServer.Spawn (newHerb.gameObject);
			timeOfLastReproduction = Time.time;
		}
	}

	override public bool checkHealth() {
		
		float daysSinceEaten = dayclock.secondsToDays (Time.time - lastTimeEaten);
		if (daysSinceEaten > numDaysWithoutFood) {
			doDamage (30);
			daysSinceEaten = Time.time;
		}
		//case: Old age
		if (DaysOld > lifeSpanInDays) {
			Destroy (this);
			return true;
		}
		//case: Health < 0
		if (health <= 0) {
			Destroy (this);
			return true;
		}	

		return false;
	}
}


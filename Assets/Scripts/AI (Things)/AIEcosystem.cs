using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class AIEcosystem : MonoBehaviour
{
	internal AIState currentState;

	public int maxNumberPlants = 40;
	public int maxNumberHerbivores = 20;
	public int maxNumberCarnivores = 8;

	public int maxSpores = 200;

	public Transform[] plants;
	public Transform[] herbivores;
	public Transform[] carnivores;

	// Use this for initialization
	void Start ()
	{
		if (!NetworkManager.singleton || !(DayClock) FindObjectOfType (typeof(DayClock)))
			return;
		int numPlants = Random.Range (20, 40);
		Transform newobj;
		if (plants.Length > 0) {
			for (int i = 0; i < numPlants; i++) {
				newobj = (Transform) Instantiate (plants [0], new Vector3 (Random.Range (-20, 20), Random.Range (0, 5), Random.Range (-20, 20)), Quaternion.identity);
				newobj.SetParent (this.transform);
			}
		}

		int numHerbs = Random.Range (10, 20);
		if (herbivores.Length > 0) {
			for (int i = 0; i < numHerbs; i++) {
				newobj = (Transform) Instantiate (herbivores [0], new Vector3 (Random.Range (-10, 10), Random.Range (3, 10), Random.Range (-20, -10)), Quaternion.identity);
				newobj.SetParent (this.transform);
			}
		}
	
	}


	
	// Update is called once per frame
	void Update ()
	{
	
	}

	void StartStateStart() {

	}

	void StartStateUpdate() {

	}

	void StartStateEnd() {

	}

	void NeutralStateStart() {

	}
		

	void NeutralStateUpdate() {

	}

	void NeutralStateEnd() {

	}
}


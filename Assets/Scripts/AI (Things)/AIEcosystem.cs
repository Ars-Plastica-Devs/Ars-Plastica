using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class AIEcosystem : NetworkBehaviour
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
		NetworkServer.SpawnObjects ();
		if (!NetworkManager.singleton || !(DayClock) FindObjectOfType (typeof(DayClock)))
			return;
		if (!isServer)
			return;
		
		NetworkServer.Spawn (this.gameObject);

		int numPlants = Random.Range (maxNumberPlants/2, maxNumberPlants);
		Transform newobj;
		if (plants.Length > 0) {
			for (int i = 0; i < numPlants; i++) {
				newobj = (Transform) Instantiate (plants [0], new Vector3 (Random.Range (-20, 20), Random.Range (0, 5), Random.Range (-20, 20)), Quaternion.identity);
				newobj.SetParent (this.transform);
				NetworkServer.Spawn (newobj.gameObject);
			}
		}

		int numHerbs = Random.Range (maxNumberHerbivores / 2, maxNumberHerbivores);
		if (herbivores.Length > 0) {
			for (int i = 0; i < numHerbs; i++) {
				newobj = (Transform) Instantiate (herbivores [0], new Vector3 (Random.Range (-10, 10), Random.Range (3, 10), Random.Range (-20, -10)), Quaternion.identity);
				newobj.SetParent (this.transform);
				NetworkServer.Spawn (newobj.gameObject);
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


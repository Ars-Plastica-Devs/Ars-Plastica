using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

/*
 * 
 * The 'driver' class for spawning the networked AI stuff.
 * Just spawns them at start of game.
 * 
 * Should keep track of number alive (like it does for nodules) and spawn more if we need more.
 * 
 * */
public class AIEcosystem : NetworkBehaviour
{

	public int maxNumberPlants = 40;
	public int maxNumberHerbivores = 20;
	public int maxNumberCarnivores = 8;

	public int maxNodules = 200;
	[SerializeField]
	private int totalNodules = 0;

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

		int numCarnivores = Random.Range (maxNumberCarnivores / 2, maxNumberCarnivores);
		if (carnivores.Length > 0) {
			for (int i = 0; i < numCarnivores; i++) {
				newobj = (Transform) Instantiate (carnivores [0], new Vector3 (Random.Range (-100, 100), Random.Range (-20, 10), Random.Range (-100, 100)), Quaternion.identity);
				newobj.SetParent (this.transform);
				NetworkServer.Spawn (newobj.gameObject);
			}
		}


	}

	/*
	 * Returns true if a nodule can be added to the world. 
	 * */
	public bool addNodule() {
		if (totalNodules + 1 > maxNodules)
			return false;

		totalNodules++;
		return true;
	}


	public bool removeNodule() {
		if (totalNodules - 1 < 0)
			return false;

		totalNodules--;
		return true;
	}

	public int getTotalNodules() {
		return totalNodules;
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}
		
}


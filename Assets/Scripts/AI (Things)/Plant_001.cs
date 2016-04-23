using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

/*
 * This plant emits nodules.
 * */
public class Plant_001 : AIEntity_Plant {

	//Nodules prefabs to spawn
	public Transform[] nodules;
	public AIEcosystem ecosystem;

	// Use this for initialization
	override public void Start () {
		base.Start ();
		ecosystem = FindObjectOfType<AIEcosystem> ();
	}
		

	public void emitNodule() {
		if (nodules.Length < 1)
			return;
        //TODO: if i use this, replace null in here ---> vvv
		if (ecosystem != null && ecosystem.AddNodule (null)) {
			Transform newObj;
			Vector3 v3;
			for (int i = 0; i < 1; i++) {
				v3 = new Vector3 (this.transform.position.x, this.rend.bounds.center.y + this.rend.bounds.extents.y, this.transform.position.z); 
				newObj = (Transform)Instantiate (nodules [0], v3, Quaternion.identity);
				NetworkServer.Spawn (newObj.gameObject);
			}
		}
	}

}

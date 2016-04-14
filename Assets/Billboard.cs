using UnityEngine;
using System.Collections;

/*
 * Used to have text always face camera, aka billboarding. 
 * 
 */
public class Billboard : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Called after Update
	void LateUpdate () {
		if (Camera.main != null) {
			transform.LookAt (Camera.main.transform.position, Vector3.up);
			transform.Rotate (Vector3.up, 180);
		}
	}
}

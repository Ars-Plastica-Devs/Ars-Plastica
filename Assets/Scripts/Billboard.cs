using UnityEngine;
using System.Collections;

/*
 * Have the transform this is attached to always face the main camera.
 * Used to have text in 3d world always appear facing the camera aka 'billboarding'
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

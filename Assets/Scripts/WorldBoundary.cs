using UnityEngine;
using System.Collections;

public class WorldBoundary : MonoBehaviour
{
	public Transform otherBoundary;
	public string teleportDir = "y";

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}



	void OnTriggerExit(Collider other)
	{
		Debug.Log ("triggered");
		Vector3 newPosition;
		if (teleportDir == "y") {
			newPosition = new Vector3 (other.transform.position.x, otherBoundary.position.y + other.bounds.size.y, other.transform.position.z);
		} else if (teleportDir == "-y") {
			newPosition = new Vector3 (other.transform.position.x, otherBoundary.position.y - other.bounds.size.y, other.transform.position.z);
		} else if (teleportDir == "x") {
			newPosition = new Vector3 (otherBoundary.position.x + other.bounds.size.x, other.transform.position.y, other.transform.position.z);
		} else if (teleportDir == "-x") {
			newPosition = new Vector3 (otherBoundary.position.x - other.bounds.size.x, other.transform.position.y, other.transform.position.z);
		} else if (teleportDir == "z") {
			newPosition = new Vector3 (other.transform.position.x, other.transform.position.y, otherBoundary.position.z + other.bounds.size.z);
		} else if (teleportDir == "-z") {
			newPosition = new Vector3 (other.transform.position.x, other.transform.position.y, otherBoundary.position.z - other.bounds.size.z);
		} else {
			return;
		}
		other.transform.position = newPosition;

	}
}


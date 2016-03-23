using UnityEngine;
using System.Collections;

public class Entity_Movement : MonoBehaviour {

	Vector3 startPos;

	public float amplitude = 10f;
	public float period = 5f;
	public float rotationSpeed = 1f;

	protected void Start() {
		startPos = transform.position;
	}

	protected void Update() {
		Rotate ();
	}

	void Rotate() {
		transform.Rotate (0, rotationSpeed * Time.deltaTime, 0);
	}

	void Bounce() {
		float theta = Time.timeSinceLevelLoad / period;
		float distance = amplitude * Mathf.Sin(theta);
		transform.position = startPos + Vector3.up * distance;
	}
}

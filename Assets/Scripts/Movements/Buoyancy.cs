using UnityEngine;
using System.Collections;

public class Buoyancy : MonoBehaviour
{
	public float yAmount = 0f;
	public float period = 5f;
	public float amplitude = 10f;

	private Vector3 startPosition;

	// Use this for initialization
	void Start ()
	{
		startPosition = transform.position;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (yAmount != 0) {  //floating up/down
			transform.position += Vector3.up * yAmount;
		} else { //null buoyancy
			float theta = Time.timeSinceLevelLoad / period;
			float distance = amplitude * (1 + Mathf.Sin(theta)); //always positive
			transform.position = startPosition + Vector3.up * distance;
		}

	}
}


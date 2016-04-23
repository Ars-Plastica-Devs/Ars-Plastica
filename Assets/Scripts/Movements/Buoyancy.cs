using UnityEngine;
using UnityEngine.Networking;

public class Buoyancy : NetworkBehaviour
{
    private Vector3 m_StartPosition;

    public float YAmount = 0f;
	public float Period = 5f;
	public float Amplitude = 10f;

	private void Start ()
	{
	    enabled = isServer;

	    if (!isServer) return;
		m_StartPosition = transform.position;
	}

    private void FixedUpdate ()
	{
	    if (!isServer) return;

		if (YAmount != 0) {  //floating up/down
			transform.position += Vector3.up * YAmount * Time.fixedDeltaTime;
		} else { //periodic bouyancy
			var theta = Time.timeSinceLevelLoad / Period;
			var distance = Amplitude * (1 + Mathf.Sin(theta)); //always positive
			transform.position = m_StartPosition + Vector3.up * distance * Time.fixedDeltaTime;
		}

	}
}


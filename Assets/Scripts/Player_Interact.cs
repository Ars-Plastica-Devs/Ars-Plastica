using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Player_Interact : NetworkBehaviour
{

	public Text descriptionText;

	private Camera m_camera; 
	private Transform lastTransformHit;
	private Color ogColor;

	void Start ()
	{
		m_camera = Camera.main;

		descriptionText = GameObject.Find ("Canvas").GetComponentInChildren<Text>();
	
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (!isLocalPlayer)
			return;
		
		RaycastHit hit;
		Vector3 fwd = m_camera.transform.TransformDirection (Vector3.forward);

		if (Physics.Raycast (m_camera.transform.position, fwd, out hit, 1000f)) {

			Debug.DrawRay (transform.position, fwd * 100); 
//			Debug.Log ("Hit" + hit.transform.GetComponentInParent<Transform> ().name);

//			Debug.Log (lastTransformHit);
			if (hit.transform != lastTransformHit) {
				
				ResetLastHit ();
				lastTransformHit = hit.transform;

				if (lastTransformHit && lastTransformHit.GetComponentInParent<Renderer> ()) {
					ogColor = lastTransformHit.GetComponentInParent<Renderer> ().material.color;
					lastTransformHit.GetComponentInParent<Renderer> ().material.color = Color.red;
				} else {
					
				}

				SetText (lastTransformHit.name);
			}

		} else {
			if (lastTransformHit) {
				ResetLastHit ();
			}
//			Debug.Log ("Nothing");
		}
		
	}

	void ResetLastHit() {
		
		if (lastTransformHit && lastTransformHit.GetComponentInParent<Renderer>()) {
			Debug.Log ("Resetting Color " + ogColor.ToString ());
			lastTransformHit.GetComponentInParent<Renderer> ().material.color = ogColor;
			lastTransformHit = null;
		}

		SetText ("Nothing");

	}

	void SetText(string setText) {
		if (descriptionText) {
			descriptionText.text = setText;
		}
	}
}


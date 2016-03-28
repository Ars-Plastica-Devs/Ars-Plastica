using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

/*
For interacting with objects in the environment when we are looking at them.
TODO: Consider renaming.  PlayerLookingAt, then have BeingLookedAt for objects that react to being looked at.
*/
public class Player_Interact : NetworkBehaviour
{

	public Text descriptionText;
	public Image panel;

	private Camera m_camera; 
	private Transform lastTransformHit;
	private Color ogColor;

	void Start ()
	{
		m_camera = Camera.main;
		if(descriptionText)
			descriptionText = GameObject.Find ("Canvas").GetComponentInChildren<Text>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		
		if (!isLocalPlayer || !descriptionText)
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

		SetText ("");

	}

	void SetText(string setText) {
		if (descriptionText) {
			descriptionText.text = setText;
		}
	}
}


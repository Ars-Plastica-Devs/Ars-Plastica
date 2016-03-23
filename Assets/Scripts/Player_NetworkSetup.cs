using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Player_NetworkSetup : NetworkBehaviour {

	[SerializeField] Camera FPSCharacterCam;
	[SerializeField] AudioListener audioListener;


	// Use this for initialization
	public override void OnStartLocalPlayer ()
	{
		Debug.Log ("HELLO");

		string ogName = this.name;

		foreach (Camera c in Camera.allCameras) {
			c.enabled = false;
		}
		
		GetComponent<CharacterController>().enabled = true;
		GetComponent<FirstPersonController>().enabled = true;
		GetComponent<CharacterController>().enabled = true;

		

		FPSCharacterCam.enabled = true;


		audioListener.enabled = true;

		Debug.Log ("enabled " + FPSCharacterCam.enabled);

	}


}

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

/*
 * Set up our local player when we spawn.
 * */
public class Player_NetworkSetup : NetworkBehaviour {

	[SerializeField] Camera FPSCharacterCam;
	[SerializeField] AudioListener audioListener;

	public TextMesh overheadName; //the name to billboard over our head.

	public override void OnStartLocalPlayer ()
	{
		
		foreach (Camera c in Camera.allCameras) {
			c.enabled = false;
		}
		
		GetComponent<CharacterController>().enabled = true;
		GetComponent<FirstPersonController>().enabled = true;
		GetComponent<CharacterController>().enabled = true;

		overheadName.text = "";

		FPSCharacterCam.enabled = true;
		audioListener.enabled = true;

	}


}

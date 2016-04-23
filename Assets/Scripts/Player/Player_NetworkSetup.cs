using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Player_NetworkSetup : NetworkBehaviour {

	[SerializeField] Camera FPSCharacterCam;
	[SerializeField] AudioListener audioListener;

	public TextMesh overheadName;


	// Use this for initialization
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

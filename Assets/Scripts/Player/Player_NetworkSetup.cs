using UnityEngine;
using UnityEngine.Networking;

/*
 * Set up our local player when we spawn.
 * */
public class Player_NetworkSetup : NetworkBehaviour {

	[SerializeField] Camera FPSCharacterCam;
	[SerializeField] AudioListener audioListener;

	public TextMesh overheadName; //the name to billboard over our head.

    private void Start()
    {
        gameObject.tag = isLocalPlayer ? "Player" : "RemotePlayer";
    }

    public override void OnStartLocalPlayer ()
	{
		foreach (var c in Camera.allCameras) {
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

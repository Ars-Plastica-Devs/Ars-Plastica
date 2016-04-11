using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PlayerNetworkSync : NetworkBehaviour {
	//Setup
	[SerializeField] Camera FPSCharacterCamera;
	[SerializeField] AudioListener audioListener;
	//Sync Rotation
	[SyncVar] Quaternion syncPlayerRotation; 
	[SyncVar] Quaternion syncCameraRotation;
	[SerializeField] Transform playerTransform;
	[SerializeField] Transform camTransform;
	[SerializeField] float lerpRate = 15f;
	//Sync Position
	[SyncVar] Vector3 syncPos;
	[SerializeField] Transform myTransform;

	void Start () {
		if(isLocalPlayer){
			Camera.main.enabled = false;

			GetComponent<CharacterController>().enabled = true;
			GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = true;
			FPSCharacterCamera.enabled = true;
			audioListener.enabled = true;
		}
	}

	void FixedUpdate () {
		TransmitRotations();
		TransmitPosition();
		LerpPosition();
		LerpRotations();
	}

	//If not local player object, lerp to new rotation
	void LerpRotations(){
		if(!isLocalPlayer){
			playerTransform.rotation = Quaternion.Lerp (playerTransform.rotation, syncPlayerRotation, Time.deltaTime * lerpRate);
			camTransform.rotation = Quaternion.Lerp (playerTransform.rotation, syncCameraRotation, Time.deltaTime * lerpRate);
		}
	}
	//If not local player object, lerp to new position
	void LerpPosition(){
		if(!isLocalPlayer){
			myTransform.position = Vector3.Lerp(myTransform.position, syncPos, Time.deltaTime * lerpRate);
		}
	}

	//Send local position to server
	[Command]
	void CmdProvidePositionToServer(Vector3 pos){
		syncPos = pos;
	}

	//Send local rotation to server
	[Command]
	void CmdProvideRotationsToServer(Quaternion playerRot, Quaternion cameraRot){
		syncPlayerRotation = playerRot;
		syncCameraRotation = cameraRot;
	}

	[ClientCallback]
	void TransmitPosition(){
		if(isLocalPlayer){
			CmdProvidePositionToServer(transform.position);
		}
	}

	[ClientCallback]
	void TransmitRotations(){
		if(isLocalPlayer){
			CmdProvideRotationsToServer(playerTransform.rotation, camTransform.rotation);
		}
	}

}
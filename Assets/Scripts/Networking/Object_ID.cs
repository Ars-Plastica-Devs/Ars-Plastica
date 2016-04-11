using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Object_ID : NetworkBehaviour {

	[SyncVar] public string objectUniqueID;
	[SyncVar] public string objectDescription;
	[SyncVar] public string objectName;
	private NetworkInstanceId objectNetID;
	private Transform myTransform;

	// Use this for initialization
	void Start () {
		GetNetIdentity ();
		SetIdentity ();
	}

	void Awake() {
		myTransform = transform;
	}
	
	// Update is called once per frame
	void Update () {
//		if (myTransform.name == "") {
//
//		}
	}

	void GetNetIdentity() {
		objectNetID = GetComponent<NetworkIdentity> ().netId;
	}

	void SetIdentity() {
		myTransform.name = "Entity: " + objectName + ":" + objectUniqueID; 
	}
}

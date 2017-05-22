using UnityEngine;
using UnityEngine.Networking;

//TODO: This can probably be deleted - doesn't seem to be used
public class Object_ID : NetworkBehaviour {

	[SyncVar] public string ObjectUniqueID;
	[SyncVar] public string ObjectDescription;
	[SyncVar] public string ObjectName;
	//private NetworkInstanceId m_ObjectNetID;
	private Transform m_MyTransform;

	// Use this for initialization
	void Start ()
    {
		GetNetIdentity ();
		SetIdentity ();
	}

	void Awake()
    {
		m_MyTransform = transform;
	}
	
	// Update is called once per frame
	void Update ()
    {
//		if (myTransform.name == "") {
//
//		}
	}

	void GetNetIdentity()
    {
		//m_ObjectNetID = GetComponent<NetworkIdentity> ().netId;
	}

	void SetIdentity()
    {
		m_MyTransform.name = "Entity: " + ObjectName + ":" + ObjectUniqueID; 
	}
}

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class OfflinePlayerTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if (NetworkManager.singleton && NetworkManager.singleton.isNetworkActive) {
			Debug.Log ("Online");
			this.gameObject.SetActive (false);
		} else {
			Debug.Log ("Offline");
			this.gameObject.SetActive (true);
			SceneManager.LoadScene ("Menu");
		}



	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

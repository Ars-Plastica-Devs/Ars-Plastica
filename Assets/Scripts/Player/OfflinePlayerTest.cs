using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


/*
 * For development.
 * When in online scene, check if online.
 * If offline, switch to Menu scene to start a Host game (so we can test networked entities);
 * */
public class OfflinePlayerTest : MonoBehaviour {

	void Start () {
		if (NetworkManager.singleton && NetworkManager.singleton.isNetworkActive) {
			Debug.Log ("Online");
		} else {
			Debug.Log ("Offline");
			SceneManager.LoadScene ("Menu");
		}
		this.gameObject.SetActive (false);
	}

}

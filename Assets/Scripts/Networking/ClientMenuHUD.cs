using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;


public class ClientMenuHUD : MonoBehaviour {

	public ArsNetworkManager nm;
	public Text playerName;
	public GameObject[] possibleAvatars;
	public GameObject playerAvatar;
	public ToggleGroup characterToggleGroup;
	public GameObject togglePrefab;

	
	// Use this for initialization
	void Start () {
		if(!nm)
			nm = GetComponent<ArsNetworkManager> ();
		ShowAvatars ();

		if (nm.skipStartScreen) {
			StartGame ();
		}
		
	}
	
	// Update is called once per frame
	void Update () {
		if (FindObjectOfType<ToggleGroup> () != null && Camera.main.transform.childCount == 0) {
			ShowAvatars ();
		}
	}
		

	//show possible avatars for selection
	void ShowAvatars() {
		float x = -3;
		ToggleGroup tg = FindObjectOfType<ToggleGroup> ();
		int i = 0;
		foreach (GameObject obj in possibleAvatars) {
			GameObject _o = Instantiate (obj);
			_o.transform.SetParent(Camera.main.transform);
			_o.transform.localPosition = new Vector3 (0 + x, 1, 5);
			_o.name = obj.name;
			x += 3;
			
			//Setup toggle
			Vector2 _v = Camera.main.WorldToScreenPoint (_o.transform.position);
			GameObject _t = (GameObject)Instantiate (togglePrefab);
			_t.transform.localPosition = Vector3.zero;
			_t.transform.position = new Vector3 (_v.x - 5, _v.y - 40, 0);
			_t.transform.SetParent(tg.transform, true);
			_t.GetComponent<Toggle> ().isOn = i++ == 0 ? true : false;
			_t.GetComponentInChildren<Text> ().text = _o.name; 
			_t.GetComponent<Toggle> ().group = characterToggleGroup;
		}

		playerAvatar = possibleAvatars [0];
	}


	public void StartGame() {
		string _selectedAvatar = "";
		foreach (Toggle t in characterToggleGroup.ActiveToggles ()) {
			_selectedAvatar = t.GetComponentInChildren<Text> ().text;
		}
		foreach (GameObject av in possibleAvatars) {
			if (av.name == _selectedAvatar) {
				playerAvatar = av;
			}
		}
			
		
		nm.ArsStartClient ();
		


	}


}

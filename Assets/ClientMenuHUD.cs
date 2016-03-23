using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ClientMenuHUD : MonoBehaviour {


	public Text playerName;

	public Text r;
	public Text g;
	public Text b;

	public Color playerColor;
	public NetworkManager nm;
	
	// Use this for initialization
	void Start () {
		nm = GetComponent<NetworkManager> ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void StartGame() {
		int rc = 0, gc = 0, bc = 0;
		if (r && !r.text.Equals("")) {
			try {
				rc = int.Parse(r.text);
			} catch {
				rc = 0;
			}
		}
		if (g && !g.text.Equals("")) {
			try {
				gc = int.Parse(g.text);
			} catch {
				gc = 0;
			}
		}
		if (b && !b.text.Equals("")) {
			try {
				bc = int.Parse(b.text);
			} catch {
				bc = 0;
			}
		}

		playerColor = new Color (rc, gc, bc);

		nm.StartClient ();

	}
}

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UILocationTranslator : MonoBehaviour
{
    private GameObject _player;
    private GameObject m_Player
    {
        get
        {
            if (_player != null) return _player;

            var players = GameObject.FindGameObjectsWithTag("Player");
            if (players.GetLength(0) == 0) return null;

            for (var i = 0; i < players.GetLength(0); i++)
            {
                var p = players[i];
                if (p != null 
                    && p.GetComponent<NetworkIdentity>() != null 
                    && p.GetComponent<NetworkIdentity>().isLocalPlayer)
                {
                    _player = p;
                    return _player;
                }
            }

            return null;
        }
    }
    public Text LocationTextElement;

	private void Update ()
	{
	    if (m_Player == null) return;

	    var pos = m_Player.transform.position;
	    LocationTextElement.text = "Loc.: {" + (int)pos.x + ", " + (int)pos.y + ", " + (int)pos.z + "}";
	}
}

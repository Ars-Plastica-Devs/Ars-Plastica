using UnityEngine;
using UnityEngine.UI;

public class UILocationTranslator : MonoBehaviour
{
    private GameObject _player;
    private GameObject m_Player
    {
        get
        {
            if (_player != null) return _player;

            _player = GameObject.FindGameObjectWithTag("Player");
            return _player;
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

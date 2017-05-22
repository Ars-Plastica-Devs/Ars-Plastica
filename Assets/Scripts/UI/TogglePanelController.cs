using UnityEngine;
using UnityEngine.UI;

public class TogglePanelController : MonoBehaviour
{
    private HUDManager m_Manager;
    private bool m_PreventPredationCommandSend;
    private bool m_PreventPredationCommandLock;
    public Toggle PredationToggle;

	private void Start ()
	{
        m_Manager = GameObject.FindGameObjectWithTag("MainUI").GetComponent<HUDManager>();
        Ecosystem.OnDataChangeClientside += OnDataChange;
	    PredationToggle.onValueChanged.AddListener(OnPredationToggle);
	}

    private void OnDataChange(Data key, string value)
    {
        switch (key)
        {
            case Data.EcoPredation:
                PredationToggle.isOn = value == "on";
                if (m_PreventPredationCommandLock)
                {
                    m_PreventPredationCommandLock = false;
                    return;
                }
                m_PreventPredationCommandSend = true;
                return;
        }
    }

    private void OnPredationToggle(bool val)
    {
        if (m_PreventPredationCommandSend)
        {
            m_PreventPredationCommandSend = false;
            return;
        }

        m_PreventPredationCommandLock = true;
        m_Manager.ForwardCommand("/data EcoPredation " + (val ? "on" : "off"));
    }
}

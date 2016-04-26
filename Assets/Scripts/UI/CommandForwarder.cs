using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

//This behaviour takes the input from the UI and passes it to the local player, 
//while also activating/deactivating the UI field as needed.
//This insulates the CommandProcessor class from the UI details.
public class CommandForwarder : MonoBehaviour
{
    private CommandProcessor _processor;
    private CommandProcessor m_Processor {
        get
        {
            //Lazy load this. I don't want to rely on when
            //the FirstPersonController will be enabled. By lazy loading,
            //we wait as long as possible to where it will be initialized.
            if (_processor != null) return _processor;

            var processor = GameObject.FindGameObjectsWithTag("Player")
                .First(cp => cp.GetComponent<NetworkIdentity>().isLocalPlayer)
                .GetComponent<CommandProcessor>();

            _processor = processor;
            _processor.OnOutputReceived = SetOutput;
            return _processor;
        }
    }

    private bool m_Deactivate;

    public InputField SelectOnEnter;
    public Text CommandOutputTarget;

    private void Update()
    {
        if (m_Deactivate)
        {
            SelectOnEnter.text = string.Empty;
            SelectOnEnter.DeactivateInputField();
            m_Deactivate = false;
            m_Processor.EnableInput();
            return; //makes sure we don't re-select the input field
        }

        if (Input.GetKeyUp(KeyCode.Return) && GUI.GetNameOfFocusedControl() == string.Empty)
        {
            SelectOnEnter.ActivateInputField();
            ForwardDisableInput();
        }
    }

    public void ForwardDisableInput()
    {
        m_Processor.DeactivateGameInput();
    }

    public void ForwardCommand(string cmd)
    {
        //On next update, deactivate, this way we dont re-catch the return event and select the field
        m_Deactivate = true;

        if (!cmd.StartsWith("/"))
            return;

        m_Processor.ProcessCommand(cmd.Substring(1));
    }

    public void SetOutput(string output)
    {
        CommandOutputTarget.text = output;
    }
}

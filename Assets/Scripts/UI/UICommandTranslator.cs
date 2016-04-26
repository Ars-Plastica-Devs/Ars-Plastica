using UnityEngine;

public class UICommandTranslator : MonoBehaviour
{
    private CommandForwarder m_Forwarder;

    private void Start()
    {
        m_Forwarder = GetComponent<CommandForwarder>();
    }

    public void OnSpawnPlant()
    {
        m_Forwarder.ForwardCommand("/spawn plant");
    }

    public void OnSpawnHerbivore()
    {
        m_Forwarder.ForwardCommand("/spawn herbivore");
    }

    public void OnSpawnCarnivore()
    {
        m_Forwarder.ForwardCommand("/spawn carnivore");
    }

    public void TogglePredation(bool value)
    {
        m_Forwarder.ForwardCommand("/world predation " + (value ? "on" : "off"));
    }
}

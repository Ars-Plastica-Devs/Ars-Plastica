using UnityEngine;
using UnityEngine.UI;

public class PopulationControlPanelController : MonoBehaviour
{
    private AIEcosystem m_Ecosystem;
    public CommandForwarder Forwarder;
    public InputField NoduleCurrentField;
    public InputField NoduleMaxField;
    public InputField PlantMinField;
    public InputField PlantCurrentField;
    public InputField PlantMaxField;
    public InputField HerbivoreMinField;
    public InputField HerbivoreCurrentField;
    public InputField HerbivoreMaxField;
    public InputField CarnivoreMinField;
    public InputField CarnivoreCurrentField;
    public InputField CarnivoreMaxField;

    private void Start ()
    {
        m_Ecosystem = GameObject.FindGameObjectWithTag("Ecosystem").GetComponent<AIEcosystem>();

        NoduleMaxField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world limit nodules " + val));
        PlantCurrentField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world set-count plants " + val));
        PlantMaxField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world limit plants " + val));
        HerbivoreMinField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world minimum herbivores " + val));
        HerbivoreCurrentField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world set-count herbivores " + val));
        HerbivoreMaxField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world limit herbivores " + val));
        CarnivoreMinField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world minimum carnivores " + val));
        CarnivoreCurrentField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world set-count carnivores " + val));
        CarnivoreMaxField.onEndEdit.AddListener(val => Forwarder.ForwardCommand("/world limit carnivores " + val));
    }

    public void Update()
    {
        if (!NoduleCurrentField.isFocused)
            NoduleCurrentField.text = m_Ecosystem.CurrentNoduleCount.ToString();
        if (!NoduleMaxField.isFocused)
            NoduleMaxField.text = m_Ecosystem.NoduleLimit.ToString();
        if (!PlantCurrentField.isFocused)
            PlantCurrentField.text = m_Ecosystem.CurrentPlantCount.ToString();
        if (!PlantMaxField.isFocused)
            PlantMaxField.text = m_Ecosystem.PlantLimit.ToString();
        if (!HerbivoreMinField.isFocused)
            HerbivoreMinField.text = m_Ecosystem.HerbivoreMinimum.ToString();
        if (!HerbivoreCurrentField.isFocused)
            HerbivoreCurrentField.text = m_Ecosystem.CurrentHerbivoreCount.ToString();
        if (!HerbivoreMaxField.isFocused)
            HerbivoreMaxField.text = m_Ecosystem.HerbivoreLimit.ToString();
        if (!CarnivoreMinField.isFocused)
            CarnivoreMinField.text = m_Ecosystem.CarnivoreMinimum.ToString();
        if (!CarnivoreCurrentField.isFocused)
            CarnivoreCurrentField.text = m_Ecosystem.CurrentCarnivoreCount.ToString();
        if (!CarnivoreMaxField.isFocused)
            CarnivoreMaxField.text = m_Ecosystem.CarnivoreLimit.ToString();
    }
}

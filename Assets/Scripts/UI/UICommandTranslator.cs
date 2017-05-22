using UnityEngine;

public class UICommandTranslator : MonoBehaviour
{
    private HUDManager m_Manager;

    private void Start()
    {
        m_Manager = GetComponent<HUDManager>();
    }

    public void OnSpawnPlant()
    {
        m_Manager.ForwardCommand("/spawn plant");
    }

    public void OnSpawnFloatingSnatcher()
    {
        m_Manager.ForwardCommand("/spawn floating-snatcher");
    }

    public void OnSpawnEmbeddedSnatcher()
    {
        m_Manager.ForwardCommand("/spawn embedded-snatcher");
    }

    public void OnSpawnFloatGLColony()
    {
        m_Manager.ForwardCommand("/spawn floatgl-colony");
    }

    public void OnSpawnFloatGSColony()
    {
        m_Manager.ForwardCommand("/spawn floatgs-colony");
    }

    public void OnSpawnSporeGun()
    {
        m_Manager.ForwardCommand("/spawn spore-gun");
    }

    public void OnSpawnAirPlant()
    {
        m_Manager.ForwardCommand("/spawn air-plant");
    }

    public void OnKillFloatGLColony()
    {
        m_Manager.ForwardCommand("/kill floatgl-colony");
    }

    public void OnKillFloatGSColony()
    {
        m_Manager.ForwardCommand("/kill floatgs-colony");
    }

    public void OnSpawnFungiB()
    {
        m_Manager.ForwardCommand("/spawn fungi-b");
    }

    public void OnSpawnBrushHead()
    {
        m_Manager.ForwardCommand("/spawn brush-head");
    }

    public void OnSpawnTriHorse()
    {
        m_Manager.ForwardCommand("/spawn tri-horse");
    }

    public void OnSpawnDownDown()
    {
        m_Manager.ForwardCommand("/spawn down-down");
    }

    public void OnSpawnTortilla()
    {
        m_Manager.ForwardCommand("/spawn tortilla");
    }

    public void OnSpawnHerbistar()
    {
        m_Manager.ForwardCommand("/spawn herbistar");
    }

    public void OnSpawnJabarkie()
    {
        m_Manager.ForwardCommand("/spawn jabarkie");
    }

    public void OnSpawnGnomehatz()
    {
        m_Manager.ForwardCommand("/spawn gnomehatz");
    }

    public void OnSpawnFellyJish()
    {
        m_Manager.ForwardCommand("/spawn fellyjish");
    }

    public void OnSpawnHerbivore()
    {
        m_Manager.ForwardCommand("/spawn herbivore");
    }

    public void OnSpawnCarnivore()
    {
        m_Manager.ForwardCommand("/spawn carnivore");
    }

    public void OnSpawnSculpture(string type)
    {
        m_Manager.ForwardCommand("/sculpture " + type);
    }

    public void TogglePredation(bool value)
    {
        m_Manager.ForwardCommand("/world predation " + (value ? "on" : "off"));
    }
}

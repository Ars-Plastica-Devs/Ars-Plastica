using UnityEngine;

public abstract class InteractionHandler : MonoBehaviour
{
    private HUDManager _manager;
    protected HUDManager HUDManager
    {
        get { return _manager 
                ?? (_manager = GameObject.FindGameObjectWithTag("MainUI").GetComponent<HUDManager>()); }
    }

    private bool m_Active;
    public bool Active
    {
        get { return m_Active; }
        set
        {
            if (m_Active == value) return;

            m_Active = value;
            SetActiveState(m_Active);
        }
    }

    public abstract void OnInteract(PlayerInteractionController controller, bool clickInteract = false);
    protected abstract void SetActiveState(bool state);
}

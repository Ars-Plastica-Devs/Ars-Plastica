using UnityEngine;

public enum ReticuleType
{
    None,
    Crosshair,
    Hand
}

public class ReticuleController : MonoBehaviour
{
    [SerializeField]
    private ReticuleType m_CurrentReticule = ReticuleType.None;

    [SerializeField]
    private GameObject m_CrossHairObject;
    [SerializeField]
    private GameObject m_HandObject;

    public ReticuleType CurrentReticule
    {
        get { return m_CurrentReticule; }
        set
        {
            Deactivate(m_CurrentReticule);
            m_CurrentReticule = value;
            Activate(m_CurrentReticule);
        }
    }

    private void Start()
    {
        if (m_CrossHairObject == null)
            Debug.Log("CrossHardObject is set to null on ReticuleController", this);
        if (m_HandObject == null)
            Debug.Log("HandObject is set to null on ReticuleController", this);

        m_CrossHairObject.SetActive(false);
        m_HandObject.SetActive(false);

        Activate(m_CurrentReticule);
    }

    private void Activate(ReticuleType reticuleType)
    {
        switch (reticuleType)
        {
            case ReticuleType.Crosshair:
                m_CrossHairObject.SetActive(true);
                break;
            case ReticuleType.Hand:
                m_HandObject.SetActive(true);
                break;
        }
    }

    private void Deactivate(ReticuleType reticuleType)
    {
        switch (reticuleType)
        {
            case ReticuleType.Crosshair:
                m_CrossHairObject.SetActive(false);
                break;
            case ReticuleType.Hand:
                m_HandObject.SetActive(false);
                break;
        }
    }
}

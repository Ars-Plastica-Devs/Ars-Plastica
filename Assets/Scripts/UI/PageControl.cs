using UnityEngine;

public class PageControl : MonoBehaviour
{
    private GameObject m_ActivePage;

    [SerializeField]
    private int m_Page;
    public int Page
    {
        get { return m_Page; }
        set
        {
            m_Page = value;
            if (Page < 0)
                Page = Pages.Length - 1;
            else if (Page > Pages.Length - 1)
                Page = 0;
            SetActivePage();
        }
    }

    public GameObject[] Pages;

    private void Start()
    {
        if (Pages.Length == 0)
            return;

        for (var i = 0; i < Pages.Length; i++)
            Pages[i].SetActive(false);

        m_ActivePage = Pages[Page];
        SetActivePage();
    }

    public void Prev()
    {
        Page--;
    }

    public void Next()
    {
        Page++;
    }

    private void SetActivePage()
    {
        m_ActivePage.SetActive(false);
        m_ActivePage = Pages[Page];
        m_ActivePage.SetActive(true);
    }
}

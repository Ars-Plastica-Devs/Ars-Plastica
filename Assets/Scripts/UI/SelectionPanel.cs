using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectionPanel : MonoBehaviour
{
    private readonly Dictionary<string, SelectableText> m_Options = new Dictionary<string, SelectableText>();
    private float m_CurrentBottom;

    public Color SelectedColor = Color.blue;
    public Color NormalColor = Color.clear;
    public GameObject ContentParent;
    public GameObject SelectableTextPrefab;

	private void Start ()
    {
	
	}

    private void Update ()
    {
	
	}

    public void AddOption(string option)
    {
        var sel = Instantiate(SelectableTextPrefab).GetComponent<SelectableText>();
        sel.SelectedColor = SelectedColor;
        sel.NormalColor = NormalColor;
        sel.Text = option;
        sel.transform.position = new Vector3(0, m_CurrentBottom, 0);
        sel.OnSelectedChanged = OnSelectedChanged;
        m_CurrentBottom -= sel.Height;
        sel.transform.SetParent(ContentParent.transform, false);

        if (m_Options.ContainsKey(option))
            m_Options[option] = sel;
        else m_Options.Add(option, sel);
    }

    public void AddOptions(params string[] options)
    {
        foreach (var option in options)
        {
            AddOption(option);
        }
    }

    public void AddDataToOption(string option, string data)
    {

    }

    public void ClearOptions()
    {
        foreach (var option in m_Options.Values)
        {
            Destroy(option.gameObject);
        }

        m_CurrentBottom = 0f;
        m_Options.Clear();
    }

    public List<string> SelectedOptions()
    {
        return m_Options.Values.Where(sel => sel.Selected).Select(sel => sel.Text).ToList();
    }

    private void OnSelectedChanged(SelectableText t)
    {
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            foreach (var sel in m_Options.Values.Where(s => s != t))
            {
                sel.Selected = false;
            }
        }
    }
}

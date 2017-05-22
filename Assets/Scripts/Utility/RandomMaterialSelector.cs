using UnityEngine;
using UnityEngine.Networking;

public class RandomMaterialSelector : NetworkBehaviour
{
    [SyncVar(hook="OnIndexChange")] private int m_Index;
    public Material[] Materials;
    public Renderer[] Renderers;

    private void Start()
    {
        if (!isServer)
            return;

        SelectRandomMaterial();
    }

    private void SelectRandomMaterial()
    {
        m_Index = Random.Range(0, Materials.Length);

        for (var i = 0; i < Renderers.Length; i++)
        {
            Renderers[i].material = Materials[m_Index];
        }
    }

    private void OnIndexChange(int newVal)
    {
        m_Index = newVal;

        if (m_Index >= Materials.Length || m_Index < 0)
        {
            Debug.Log(gameObject.GetComponent<HerbivoreBase>().GetType().Name + " error with material selection. Tried " + newVal + " with limit " + Materials.Length);
            return;
        }

        for (var i = 0; i < Renderers.Length; i++)
        {
            Renderers[i].material = Materials[m_Index];
        }
    }
}

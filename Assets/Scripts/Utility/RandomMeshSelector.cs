using UnityEngine;
using UnityEngine.Networking;

public class RandomMeshSelector : NetworkBehaviour
{
    [SyncVar(hook="OnIndexChange")] private int m_Index;
    public Mesh[] Meshes;
    public MeshFilter Filter;

    private void Start()
    {
        if (!isServer)
        {
            Filter.mesh = Meshes[m_Index];
            return;
        }

        SelectRandomMesh();
    }

    private void SelectRandomMesh()
    {
        m_Index = Random.Range(0, Meshes.Length);

        Filter.mesh = Meshes[m_Index];
    }

    private void OnIndexChange(int newVal)
    {
        m_Index = newVal;

        Filter.mesh = Meshes[m_Index];
    }
}

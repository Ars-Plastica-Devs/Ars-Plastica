using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class InfoSupplier : NetworkBehaviour
{
    private List<IInfoSupplier> m_Suppliers;

    private float m_UpdateRate = 1f;
    private float m_UpdateCounter;
    [HideInInspector]
    public List<string> DataStrings = new List<string>();

    [TextArea(3, 10)]
    public string ConstantData = string.Empty;

    private void Start()
    {
        if (!isServer) return;

        //Add some randomness to the value so that it diverges over time for
        //objects spawned simultaneously
        m_UpdateRate += Random.value * (m_UpdateRate * .05f);
        m_Suppliers = new List<IInfoSupplier>(GetComponents<IInfoSupplier>());

        foreach (var data in m_Suppliers.SelectMany(supplier => supplier.GetData()))
        {
            DataStrings.Add(data);
        }
    }

    private void Update()
    {
        if (!isServer) return;

        m_UpdateCounter += Time.deltaTime;
        if (m_UpdateCounter > m_UpdateRate)
        {
            m_UpdateCounter = 0f;

            DataStrings.Clear();
            if (!string.IsNullOrEmpty(ConstantData))
                DataStrings.Add(ConstantData);

            //Better memory efficiency with for loop - tested and confirmed
            for (var i = 0; i < m_Suppliers.Count; i++)
            {
                DataStrings.AddRange(m_Suppliers[i].GetData());
            }
        }
    }

    public string GetDataString()
    {
        var dataString = DataStrings.Aggregate(string.Empty, (current, ss) => current + (ss + "\n"));
        dataString = dataString.TrimEnd('\n');
        return dataString;
    }
}

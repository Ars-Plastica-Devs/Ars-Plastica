using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using UnityEngine.Networking;

public class DataSupplier : NetworkBehaviour
{
    private List<IDataSupplier> m_Suppliers;

    public SyncListString SyncedStrings = new SyncListString();

    private void Start()
    {
        m_Suppliers = new List<IDataSupplier>(GetComponents<IDataSupplier>());

        SyncedStrings.Callback += OnDataChange;

        //TODO: We only want to run the following on the server
        //if (!isServer) return;

        SyncedStrings.Add(gameObject.name);
        foreach (var data in m_Suppliers.SelectMany(supplier => supplier.GetData()))
        {
            SyncedStrings.Add(data);
        }
    }

    private void Update()
    {
        //TODO: We only want to run this on the server
        //if (!isServer) return;


    }

    private void OnDataChange(SyncList<string>.Operation op, int itemindex)
    {
        //TODO: Changes are only tracked on the client
        //if (!isClient) return;
    }
}

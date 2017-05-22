using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameOptionsDropdown : MonoBehaviour
{
    public ArsNetworkManager NetworkManager;
    public Dropdown Dropdown;
    public GameObject ErrorMessageObject;

    private void Start()
    {
        Dropdown.options = NetworkManager.ServerListing.Servers.Select(o => new Dropdown.OptionData(o.Name)).ToList();
        Dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        OnDropdownValueChanged(0);
    }

    private void Update()
    {
        //Debug.Log("It's Alive! (GameOptionsDropDown.cs)");
    }

    private void OnDropdownValueChanged(int val)
    {
        if (NetworkManager.ServerListing.Servers[val].IP == "localhost")
        {
            NetworkManager.IsDevelopment = true;
            NetworkManager.SelfContainedHost = true;
        }

        NetworkManager.networkAddress = NetworkManager.ServerListing.Servers[val].IP;

        ValidateVersion(NetworkManager.ServerListing.Servers[val].Version);
    }

    private void ValidateVersion(string version)
    {
        ErrorMessageObject.SetActive((NetworkManager.CurrentVersionNumber != version) && version != "Any");
    }
}

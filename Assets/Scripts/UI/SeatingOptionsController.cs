using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SeatingOptionsController : NetworkBehaviour, ICommandReceiver
{
    public InputField WidthField;
    public InputField HeightField;
    public InputField DistanceField;

    public GameObject StandsPrefab;

    private void Start()
    {
        var sc = StandsPrefab.GetComponent<Stands>();
        WidthField.text = sc.Width.ToString();
        HeightField.text = sc.Height.ToString();
        DistanceField.text = sc.DistanceBetweenSeats.ToString();

        CommandProcessor.PendingReceivers.Add(gameObject);
    }

    public void SendSpawnSeatingCommand()
    {
        HUDManager.Singleton.ForwardCommand("/seating " + WidthField.text + " " + HeightField.text + " " + DistanceField.text);
    }

    public bool IsCommandRelevant(string cmd, GameObject sender = null)
    {
        return cmd.StartsWith("seating");
    }

    public string RunCommand(string cmd, GameObject sender)
    {
        var tokens = cmd.Split(' ');

        var width = uint.Parse(tokens[1]);
        var height = uint.Parse(tokens[2]);
        var dist = float.Parse(tokens[3]);
        var pos = sender.transform.position + sender.transform.forward * 5f;
        var rot = Quaternion.LookRotation(-sender.transform.forward, Vector3.up);
        var stands = (GameObject)Instantiate(StandsPrefab, pos, rot);
        var sc = stands.GetComponent<Stands>();
        sc.Width = width;
        sc.Height = height;
        sc.DistanceBetweenSeats = dist;

        NetworkServer.Spawn(stands);

        return "Spawning Seating";
    }
}

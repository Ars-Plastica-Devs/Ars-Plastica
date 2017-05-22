using UnityEngine;
using UnityEngine.Networking;

public class PredationHandler : MonoBehaviour
{
    public GameObject BloodPrefab;

    private void Start()
    {
        Ecosystem.Singleton.OnPredationEvent += OnPredation;
    }

    private void OnPredation(Creature creaturekilled)
    {
        var obj = Instantiate(BloodPrefab, creaturekilled.transform.position, creaturekilled.transform.rotation);
        NetworkServer.Spawn(obj);
    }

    private void OnDestroy()
    {
        if (Ecosystem.Singleton != null)
            Ecosystem.Singleton.OnPredationEvent -= OnPredation;
    }
}

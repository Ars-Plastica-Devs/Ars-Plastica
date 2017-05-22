using UnityEngine;
using UnityEngine.Networking;

public abstract class Creature : NetworkBehaviour
{
    public static bool PredationAllowed = true;
    public static bool ReproductionAllowed = true;

    public float Health = 100;
    protected float BirthTime;

    protected virtual void Start()
    {
        BirthTime = Time.time;

        var net = GetComponent<NetworkProximityChecker>();
        if (net != null)
        {
            net.visUpdateInterval += Random.value * .5f;
        }

        var rel = GetComponent<NetworkRelevanceChecker>();
        if (rel != null)
        {
            rel.UpdateInterval += Random.value * .5f;
        }
    }

    public virtual void Damage(float amount)
    {
        Health -= amount;
    }
}

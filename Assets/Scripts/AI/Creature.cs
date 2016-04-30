using UnityEngine;
using UnityEngine.Networking;

public abstract class Creature : NetworkBehaviour
{
    public float Health = 100;
    protected float BirthTime;

    protected virtual void Start()
    {
        var net = GetComponent<NetworkProximityChecker>();

        if (net != null)
        {
            net.visUpdateInterval += Random.value * .5f;
        }
    }

    public virtual void Damage(float amount)
    {
        Health -= amount;
    }
}

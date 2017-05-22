using UnityEngine;

public class SnatcherNodule : Nodule
{
    public delegate void EatHandler(Creature eater);
    public event EatHandler OnEat;

    public Vector3 TargetLocation;
    public float Range = 3f;

    public WanderParameters WanderParameters = new WanderParameters(4f, 20f, 40f);

    private void Start()
    {
        Type = NoduleType.Snatcher;

        if (!isServer) return;

        TargetLocation = transform.position;
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        var vel = Vector3.zero;

        if ((transform.position - TargetLocation).sqrMagnitude < Range * Range)
        {
            vel += Steering.Wander(gameObject, ref WanderParameters);
        }
        else
        {
            vel += Steering.Seek(gameObject, TargetLocation);
        }

        transform.position += vel * Time.fixedDeltaTime;
    }

    public override void GetEaten(Creature eater)
    {
        if (OnEat != null)
        {
            OnEat(eater);
        }
    }
}

using UnityEngine;

public class Wanderer : MonoBehaviour
{
    public WanderParameters Parameters;
    public float Speed;

    private void FixedUpdate()
    {
        transform.position += Steering.Wander(gameObject, ref Parameters) 
                                * Speed * Time.fixedDeltaTime;
    }
}

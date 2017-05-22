using UnityEngine;

public class RandomInitialRotation : MonoBehaviour
{
    private void Start()
    {
        transform.rotation = Random.rotation;
    }
}

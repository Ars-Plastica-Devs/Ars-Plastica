using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 Rotation;

    private void FixedUpdate ()
    {
	    transform.Rotate(Rotation * Time.fixedDeltaTime, Space.Self);
	}
}
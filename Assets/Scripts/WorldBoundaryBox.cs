using UnityEngine;
using UnityEngine.Networking;

public class WorldBoundaryBox : NetworkBehaviour
{
    public float Extent = 2500f;

    private void OnTriggerExit(Collider other)
    {
        //There are a few unexplained cases where this is called when the
        //other collider is still well within the world bounds, so we run
        //this check
        if (Mathf.Abs(other.transform.position.x) < Extent &&
            Mathf.Abs(other.transform.position.y) < Extent &&
            Mathf.Abs(other.transform.position.z) < Extent)
        {
            return;
        }

        var otherPos = other.transform.position;

        var teleportDir =   otherPos.x > Extent ? new Vector3(-1f, 0, 0) :
                            otherPos.x < -Extent ? new Vector3(1f, 0, 0) :
                            otherPos.y > Extent ? new Vector3(0, -1f, 0) :
                            otherPos.y < -Extent ? new Vector3(0, 1f, 0) :
                            otherPos.z > Extent ? new Vector3(0, 0, -1f) :
                            new Vector3(0, 0, 1f);

        var possiblePosition = teleportDir * (Extent * 2f) + other.transform.position;
        possiblePosition -= other.bounds.size + Vector3.one;

        other.transform.position = possiblePosition;
    }
}


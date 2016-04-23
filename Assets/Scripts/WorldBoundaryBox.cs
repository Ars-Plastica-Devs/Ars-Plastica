using UnityEngine;
using UnityEngine.Networking;

public class WorldBoundaryBox : NetworkBehaviour
{
    public float Extent = 2500f;

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

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


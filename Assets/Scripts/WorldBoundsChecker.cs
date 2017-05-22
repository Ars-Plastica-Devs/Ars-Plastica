using UnityEngine;
using UnityEngine.Networking;

public class WorldBoundsChecker : NetworkBehaviour
{
    private float m_Counter;
    public float Rate = 2f;

    private void Update()
    {
        if (!isServer)
            return;

        m_Counter += Time.deltaTime;

        if (!(m_Counter > Rate) 
            || WorldBoundaryBox.Singleton == null)
            return;

        m_Counter = 0f;
        WorldBoundaryBox.Singleton.BoundsCheck(gameObject);
    }
}

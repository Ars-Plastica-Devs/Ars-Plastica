using Assets.Octree;
using UnityEngine;
using UnityEngine.Networking;

public class ShrinkOnProximity : CubeBehaviour
{
    private float m_InitialScale;
    private float m_InteractionRadius;
    private float m_PlayerCheckRate = .05f;
    private float m_PlayerCheckCounter;

    [SyncVar]
    private NetworkInstanceId m_ParentNetID;

    public override NetworkInstanceId ParentNetID
    {
        get { return m_ParentNetID; }
        set { m_ParentNetID = value; }
    }

    private void Start()
    {
        m_PlayerCheckRate = Random.Range(m_PlayerCheckRate * .95f, m_PlayerCheckRate * 1.05f);
    }

    public void SetInitialScale(float f)
    {
        m_InitialScale = f;
    }

    public void Initialize(float interactRadius, float size)
    {
        enabled = true;
        m_InitialScale = size;
        m_InteractionRadius = interactRadius;

        if (isServer)
            RpcInitialize(interactRadius, size);
    }

    [ClientRpc]
    private void RpcInitialize(float interactRadius, float size)
    {
        enabled = true;
        m_InitialScale = size;
        m_InteractionRadius = interactRadius;
    }

    public void SetInteractionRadius(float r)
    {
        m_InteractionRadius = r;
        //RpcSetInteractionRadius(r);
    }

    /*[ClientRpc]
    private void RpcSetInteractionRadius(float r)
    {
        m_InteractionRadius = r;
    }*/

    public void Reset()
    {
        transform.localScale = new Vector3(m_InitialScale, m_InitialScale, m_InitialScale);
    }

    private void OnDisable()
    {
        Reset();
    }

    private void FixedUpdate()
    {
        if (!isClient)
            return;

        m_PlayerCheckCounter += Time.fixedDeltaTime;
        if (m_PlayerCheckCounter > m_PlayerCheckRate)
        {
            m_PlayerCheckCounter = 0f;
            CheckForPlayer();
        }
    }

    private void CheckForPlayer()
    {
        var player = OctreeManager.Get(OctreeType.Player).GetClosestObject(transform.position, m_InteractionRadius);

        ProximityShrink(player);
    }

    private void ProximityShrink(Transform player)
    {
        if (player == null)
        {
            Reset();
            return;
        }

        var d = (player.position - transform.position).magnitude;
        var per = Mathf.Clamp(d / m_InteractionRadius, 0f, 1f);

        var scale = per * m_InitialScale;
        transform.localScale = new Vector3(scale, scale, scale);
    }
}

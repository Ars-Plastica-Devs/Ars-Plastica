using Assets.Octree;
using UnityEngine;
using UnityEngine.Networking;

//TODO: Use IProximitySensor
public class MakeTransparent : CubeBehaviour
{
    private enum State
    {
        Active,
        Inactive
    }

    private Renderer m_Renderer;
    private float m_InteractionRadius;
    private float m_PlayerCheckRate = .1f;
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
        m_Renderer = GetComponent<Renderer>();
        m_PlayerCheckRate = Random.Range(m_PlayerCheckRate * .95f, m_PlayerCheckRate * 1.05f);
    }

    private void OnEnable()
    {
        if (m_Renderer == null)
            m_Renderer = GetComponent<Renderer>();

        m_Renderer.enabled = true;
    }

    public void Initialize(float interactRadius, float size)
    {
        enabled = true;
        m_InteractionRadius = interactRadius;
        transform.localScale = new Vector3(size, size, size);

        if (m_Renderer == null)
            m_Renderer = GetComponent<Renderer>();

        m_Renderer.enabled = true;

        if (isServer)
            RpcInitialize(interactRadius, size);
    }

    [ClientRpc]
    private void RpcInitialize(float interactRadius, float size)
    {
        enabled = true;
        m_InteractionRadius = interactRadius;
        transform.localScale = new Vector3(size, size, size);

        if (m_Renderer == null)
            m_Renderer = GetComponent<Renderer>();

        m_Renderer.enabled = true;
    }

    public void SetInteractionRadius(float r)
    {
        m_InteractionRadius = r;
        RpcSetInteractionRadius(r);
    }

    [ClientRpc]
    private void RpcSetInteractionRadius(float r)
    {
        m_InteractionRadius = r;
    }

    public void Reset()
    {
        m_Renderer.enabled = true;
    }

    private void OnDisable()
    {
        Reset();
    }

    private void FixedUpdate()
    {
        if (!isClient || !enabled)
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

        SetState(player == null ? State.Inactive : State.Active);
    }

    private void SetState(State state)
    {
        m_Renderer.enabled = state == State.Inactive;
    }
}
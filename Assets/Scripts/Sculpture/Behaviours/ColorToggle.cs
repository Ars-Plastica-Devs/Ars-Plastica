using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.1f)]
public class ColorToggle : CubeBehaviour
{
    private enum State
    {
        Positive,
        Negative
    }

    [SyncVar(hook="OnStateChange")]
    private State m_State = State.Negative;

    private ColorToggleInteractHandler m_InteractHandler;

    [SyncVar(hook="OnPositiveColorChange")]
    [HideInInspector]
    public Color PositiveColor = Color.black;

    [SyncVar(hook="OnNegativeColorChange")]
    [HideInInspector]
    public Color NegativeColor = Color.white;

    public int LayerToSet;

    [SyncVar]
    private NetworkInstanceId m_ParentNetID;

    public override NetworkInstanceId ParentNetID
    {
        get { return m_ParentNetID; }
        set { m_ParentNetID = value; }
    }

    private void Start()
    {
        gameObject.layer = LayerToSet;
        SetColor(m_State == State.Negative ? NegativeColor : PositiveColor);

        m_InteractHandler = gameObject.GetComponent<ColorToggleInteractHandler>()
                            ?? gameObject.AddComponent<ColorToggleInteractHandler>();
        m_InteractHandler.ColorToggle = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        SetColor(m_State == State.Negative ? NegativeColor : PositiveColor);
    }

    public void Initialize()
    {
        enabled = true;
        SetColor(m_State == State.Negative ? NegativeColor : PositiveColor);

        GetComponent<BoxCollider>().enabled = true;
    }

    [Server]
    public void Toggle()
    {
        m_State = m_State == State.Positive ? State.Negative : State.Positive;
    }

    private void OnStateChange(State newState)
    {
        if (m_State == newState)
            return;

        m_State = newState;
        SetColor(m_State == State.Negative ? NegativeColor : PositiveColor);
    }

    private void OnPositiveColorChange(Color newColor)
    {
        PositiveColor = newColor;
        if (m_State == State.Positive)
            SetColor(PositiveColor);
    }

    private void OnNegativeColorChange(Color newColor)
    {
        NegativeColor = newColor;
        if (m_State == State.Negative)
            SetColor(NegativeColor);
    }

    private void SetColor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }
}

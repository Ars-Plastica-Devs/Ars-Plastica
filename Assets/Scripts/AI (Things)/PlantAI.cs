using UnityEngine;
using UnityEngine.Networking;

public class PlantAI : Creature
{
    private enum GrowthState
    {
        Growing,
        Grown,
        Dead
    }

    private DayClock m_Clock;
    private float m_LastDaySporesReleased;
    private int m_NodulesSpawnedToday;
    private Vector3 m_InitialScale;
    private Vector3 m_FinalScale;
    private float m_InitialHeight;
    private float m_FinalHeight;
    private float m_NextTargetHeight;
    private Vector3 m_PreviousTargetScale;
    private Vector3 m_NextTargetScale;
    private float m_CurrentGrowTime;
    private float m_DaysDead;

    private readonly FSM<GrowthState> m_GrowthBrain = new FSM<GrowthState>();

    private Color m_LifeColor;
    public Color DeathColor;

    public AIEcosystem Ecosystem;

    public float LifeSpan = 20f;
    public float DaysBeforeRebirth = 3f;
    public float DaysToDoubleSize = 3f;
    public float DaysToDieInShade = 3f;
    public float FullSizeMin = 25f;
    public float FullSizeMax = 55f;
    public int NodulesPerDay = 20;
    public float NoduleDispersalRange = 3f;

    public float DaysOld;
    [SyncVar]
    public Vector3 Scale;

    public GameObject NodulePrefab;

    protected override void Start()
    {
        base.Start();
        if (!isServer)
            return;

        m_LifeColor = GetComponent<Renderer>().material.color;

        m_Clock = FindObjectOfType<DayClock>();

        m_GrowthBrain.In(GrowthState.Growing)
            .If(() => Scale.sqrMagnitude >= m_FinalScale.sqrMagnitude)
                .GoTo(GrowthState.Grown)
            .ExecuteOnEntry(GrowingStart)
            .ExecuteWhileIn(NoduleReleaseUpdate, GrowingUpdate);

        m_GrowthBrain.In(GrowthState.Grown)
            .If(() => DaysOld > LifeSpan)
                .GoTo(GrowthState.Dead)
            .ExecuteWhileIn(NoduleReleaseUpdate);

        m_GrowthBrain.In(GrowthState.Dead)
            .If(() => m_DaysDead > DaysBeforeRebirth)
                .GoTo(GrowthState.Growing)
            .ExecuteOnEntry(DeathStart)
            .ExecuteWhileIn(DeathUpdate)
            .ExecuteOnExit(DeathExit);

        m_GrowthBrain.Initialize(GrowthState.Growing);
    }

    private void Update()
    {
        if (isClient)
        {
            transform.localScale = Scale;
        }

        if (isServer)
        {
            m_GrowthBrain.Update(Time.deltaTime);
            DaysOld = m_Clock.SecondsToDays(Time.time - BirthTime);
        }
    }

    private void GrowingStart()
    {
        BirthTime = Time.time;
        m_LastDaySporesReleased = Mathf.Floor(m_Clock.SecondsToDays(Time.time - BirthTime));
        var rend = GetComponent<Renderer>();

        m_InitialHeight = rend.bounds.size.y;
        m_NextTargetHeight = m_InitialHeight * 2f;
        m_FinalHeight = Random.Range(FullSizeMin, FullSizeMax);
        
        m_InitialScale = transform.localScale;
        m_PreviousTargetScale = m_InitialScale;
        m_NextTargetScale = new Vector3(m_InitialScale.x, m_InitialScale.y * 2, m_InitialScale.z);
        m_FinalScale = new Vector3(transform.localScale.x, (m_FinalHeight / m_InitialHeight) * (transform.localScale.y), transform.localScale.z);
        
        Scale = transform.localScale;
    }

    private void GrowingUpdate()
    {
        m_CurrentGrowTime += Time.deltaTime;

        if (m_CurrentGrowTime > m_Clock.DaysToSeconds(DaysToDoubleSize))
        {
            m_CurrentGrowTime = 0;
            Scale = m_NextTargetScale;

            m_PreviousTargetScale = m_NextTargetScale;
            m_NextTargetScale = new Vector3(Scale.x, Scale.y * 2, Scale.z);
            m_NextTargetHeight *= 2f;

            if (m_NextTargetHeight > m_FinalHeight)
            {
                m_NextTargetHeight = m_FinalHeight;
                m_NextTargetScale = m_FinalScale;
            }
            return;
        }

        var lerpProgress = m_CurrentGrowTime / m_Clock.DaysToSeconds(DaysToDoubleSize);
        Scale = Vector3.Lerp(m_PreviousTargetScale, m_NextTargetScale, lerpProgress);
        transform.localScale = Scale;
    }

    private void NoduleReleaseUpdate()
    {
        var today = Mathf.Floor(m_Clock.SecondsToDays(Time.time - BirthTime));

        if (today > m_LastDaySporesReleased && m_Clock.IsDay())
        {
            m_LastDaySporesReleased = today;
            if (NodulePrefab == null)
                return;

            //Spawn nodules for .4-.6 days (a bit of randomness to break things up)
            InvokeRepeating("EmitNodule", 0f, m_Clock.DaysToSeconds(.4f + (Random.value * .2f)) / NodulesPerDay);
        }
    }

    private void DeathStart()
    {
        m_DaysDead = 0f;
        m_CurrentGrowTime = 0f;
        m_PreviousTargetScale = transform.localScale;
        m_NextTargetScale = m_InitialScale;
        GetComponent<Renderer>().material.color = DeathColor;
        RpcSetColor(DeathColor);
    }

    private void DeathUpdate()
    {
        m_DaysDead += m_Clock.SecondsToDays(Time.deltaTime);

        m_CurrentGrowTime += Time.deltaTime;

        var lerpProgress = m_CurrentGrowTime / m_Clock.DaysToSeconds(DaysBeforeRebirth);
        Scale = Vector3.Lerp(m_PreviousTargetScale, m_NextTargetScale, lerpProgress);
        transform.localScale = Scale;
    }

    private void DeathExit()
    {
        GetComponent<Renderer>().material.color = m_LifeColor;
        RpcSetColor(m_LifeColor);
        m_DaysDead = 0f;
    }

    [ClientRpc]
    private void RpcSetColor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }

    private void EmitNodule()
    {
        var rend = GetComponent<Renderer>();

        var offsetX = Random.value * NoduleDispersalRange;
        var offsetZ = Random.value * NoduleDispersalRange;
        var pos = new Vector3(transform.position.x + offsetX, rend.bounds.center.y + rend.bounds.extents.y, transform.position.z + offsetZ);

        if (!Ecosystem.CanAddNodule())
        {
            m_NodulesSpawnedToday = 0;
            CancelInvoke("EmitNodule");
            return;
        }

        var obj = (GameObject)Instantiate(NodulePrefab, pos, Quaternion.identity);
        Ecosystem.AddNodule(obj);
        NetworkServer.Spawn(obj);

        m_NodulesSpawnedToday++;
        if (m_NodulesSpawnedToday > NodulesPerDay)
        {
            m_NodulesSpawnedToday = 0;
            CancelInvoke("EmitNodule");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlantAI : PlantBase, IInfoSupplier
{
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

    public override PlantType Type {
        get { return PlantType.Generic; }
    }

    private Color m_LifeColor;
    public Color DeathColor;

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

    protected override void Start()
    {
        base.Start();

        if (!isServer)
            return;

        LifeSpan = DataStore.GetFloat(Data.PlantLifeSpan);
        DaysBeforeRebirth = DataStore.GetFloat(Data.PlantDaysBeforeRebirth);
        DaysToDoubleSize = DataStore.GetFloat(Data.PlantDaysToDoubleSize);
        DaysToDieInShade = DataStore.GetFloat(Data.PlantDaysToDieInShade);
        FullSizeMin = DataStore.GetFloat(Data.PlantFullSizeMin);
        FullSizeMax = DataStore.GetFloat(Data.PlantFullSizeMax);
        NodulesPerDay = DataStore.GetInt(Data.PlantNodulesPerDay);
        NoduleDispersalRange = DataStore.GetFloat(Data.PlantNoduleDispersalRange);

        m_LifeColor = GetComponent<Renderer>().material.color;

        DayClock.Singleton = FindObjectOfType<DayClock>();

        GrowthBrain.In(GrowthState.Growing)
            .If(() => Scale.sqrMagnitude >= m_FinalScale.sqrMagnitude)
                .GoTo(GrowthState.Grown)
            .ExecuteOnEntry(GrowingStart)
            .ExecuteWhileIn(NoduleReleaseUpdate, GrowingUpdate);

        GrowthBrain.In(GrowthState.Grown)
            .If(() => DaysOld > LifeSpan)
                .GoTo(GrowthState.Dead)
            .ExecuteWhileIn(NoduleReleaseUpdate);

        GrowthBrain.In(GrowthState.Dead)
            .If(() => m_DaysDead > DaysBeforeRebirth)
                .GoTo(GrowthState.Growing)
            .ExecuteOnEntry(DeathStart)
            .ExecuteWhileIn(DeathUpdate)
            .ExecuteOnExit(DeathExit);

        GrowthBrain.Initialize(GrowthState.Growing);
    }

    private void Update()
    {
        if (isClient)
        {
            transform.localScale = Scale;
        }

        if (isServer)
        {
            GrowthBrain.Update(Time.deltaTime);
            DaysOld = DayClock.Singleton.SecondsToDays(Time.time - BirthTime);
        }
    }

    protected virtual void GrowingStart()
    {
        BirthTime = Time.time;
        m_LastDaySporesReleased = Mathf.Floor(DayClock.Singleton.SecondsToDays(Time.time - BirthTime));
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

    protected virtual void GrowingUpdate()
    {
        m_CurrentGrowTime += Time.deltaTime;

        if (m_CurrentGrowTime > DayClock.Singleton.DaysToSeconds(DaysToDoubleSize))
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

        var lerpProgress = m_CurrentGrowTime / DayClock.Singleton.DaysToSeconds(DaysToDoubleSize);
        Scale = Vector3.Lerp(m_PreviousTargetScale, m_NextTargetScale, lerpProgress);
        transform.localScale = Scale;
    }

    protected virtual void NoduleReleaseUpdate()
    {
        var today = Mathf.Floor(DayClock.Singleton.SecondsToDays(Time.time - BirthTime));

        if (today > m_LastDaySporesReleased && DayClock.Singleton.IsDay())
        {
            m_LastDaySporesReleased = today;

            //Spawn nodules for .4-.6 days (a bit of randomness to break things up)
            InvokeRepeating("EmitNodule", 0f, DayClock.Singleton.DaysToSeconds(.4f + (Random.value * .2f)) / NodulesPerDay);
        }
    }

    protected virtual void DeathStart()
    {
        m_DaysDead = 0f;
        m_CurrentGrowTime = 0f;
        m_PreviousTargetScale = transform.localScale;
        m_NextTargetScale = m_InitialScale;
        GetComponent<Renderer>().material.color = DeathColor;
        RpcSetColor(DeathColor);
    }

    protected virtual void DeathUpdate()
    {
        m_DaysDead += DayClock.Singleton.SecondsToDays(Time.deltaTime);

        m_CurrentGrowTime += Time.deltaTime;

        var lerpProgress = m_CurrentGrowTime / DayClock.Singleton.DaysToSeconds(DaysBeforeRebirth);
        Scale = Vector3.Lerp(m_PreviousTargetScale, m_NextTargetScale, lerpProgress);
        transform.localScale = Scale;
    }

    protected virtual void DeathExit()
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

        if (!Ecosystem.Singleton.CanAddNodule())
        {
            m_NodulesSpawnedToday = 0;
            CancelInvoke("EmitNodule");
            return;
        }

        Ecosystem.Singleton.SpawnNodule(pos, Quaternion.identity, NoduleType.Floating);

        m_NodulesSpawnedToday++;
        if (m_NodulesSpawnedToday > NodulesPerDay)
        {
            m_NodulesSpawnedToday = 0;
            CancelInvoke("EmitNodule");
        }
    }

    public List<string> GetData()
    {
        switch (GrowthBrain.CurrentState)
        {
            case GrowthState.Dead:
                return new List<string> { "This Plant is Dead!" };
            case GrowthState.Growing:
                return new List<string> { "This growing Plant is " + (int)DaysOld + " days old" };
            case GrowthState.Grown:
                return new List<string> { "This grown Plant is " + (int)DaysOld + " days old" };
        }

        return new List<string>();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        if (LifeSpan != DataStore.GetFloat(Data.PlantLifeSpan))
        {
            DataStore.Set(Data.PlantLifeSpan, LifeSpan);
        }
        if (DaysBeforeRebirth != DataStore.GetFloat(Data.PlantDaysBeforeRebirth))
        {
            DataStore.Set(Data.PlantDaysBeforeRebirth, DaysBeforeRebirth);
        }
        if (DaysToDoubleSize != DataStore.GetFloat(Data.PlantDaysToDoubleSize))
        {
            DataStore.Set(Data.PlantDaysToDoubleSize, DaysToDoubleSize);
        }
        if (DaysToDieInShade != DataStore.GetFloat(Data.PlantDaysToDieInShade))
        {
            DataStore.Set(Data.PlantDaysToDieInShade, DaysToDieInShade);
        }
        if (FullSizeMin != DataStore.GetFloat(Data.PlantFullSizeMin))
        {
            DataStore.Set(Data.PlantFullSizeMin, FullSizeMin);
        }
        if (FullSizeMax != DataStore.GetFloat(Data.PlantFullSizeMax))
        {
            DataStore.Set(Data.PlantFullSizeMax, FullSizeMax);
        }
        if (NodulesPerDay != DataStore.GetInt(Data.PlantNodulesPerDay))
        {
            DataStore.Set(Data.PlantNodulesPerDay, NodulesPerDay);
        }
        if (NoduleDispersalRange != DataStore.GetFloat(Data.PlantNoduleDispersalRange))
        {
            DataStore.Set(Data.PlantNoduleDispersalRange, NoduleDispersalRange);
        }
    }

    public static void ChangePlantData(Data key, string value, IEnumerable<PlantAI> plants)
    {
        switch (key)
        {
            case Data.PlantLifeSpan:
                var lifeSpan = float.Parse(value);
                foreach (var plant in plants)
                {
                    plant.LifeSpan = lifeSpan;
                }
                break;
            case Data.PlantDaysBeforeRebirth:
                var rebirthDays = float.Parse(value);
                foreach (var plant in plants)
                {
                    plant.DaysBeforeRebirth = rebirthDays;
                }
                break;
            case Data.PlantDaysToDoubleSize:
                var doubleDays = float.Parse(value);
                foreach (var plant in plants)
                {
                    plant.DaysToDoubleSize = doubleDays;
                }
                break;
            case Data.PlantDaysToDieInShade:
                var shadeDays = float.Parse(value);
                foreach (var plant in plants)
                {
                    plant.DaysToDieInShade = shadeDays;
                }
                break;
            case Data.PlantFullSizeMin:
                var min = float.Parse(value);
                foreach (var plant in plants)
                {
                    plant.FullSizeMin = min;
                }
                break;
            case Data.PlantFullSizeMax:
                var max = float.Parse(value);
                foreach (var plant in plants)
                {
                    plant.FullSizeMax = max;
                }
                break;
            case Data.PlantNodulesPerDay:
                var nodPerDay = int.Parse(value);
                foreach (var plant in plants)
                {
                    plant.NodulesPerDay = nodPerDay;
                }
                break;
            case Data.PlantNoduleDispersalRange:
                var disperseRange = float.Parse(value);
                foreach (var plant in plants)
                {
                    plant.NoduleDispersalRange = disperseRange;
                }
                break;
        }
    }
}

using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Rigidbody))]
public abstract class Animal : Creature
{
    protected enum GrowthState
    {
        Young,
        Teen,
        Adult,
        Dead
    }

    protected readonly FSM<GrowthState> GrowthBrain = new FSM<GrowthState>();
    [SerializeField]

    protected GrowthState CurrentGrowthState;

    protected Rigidbody Rigidbody;
    protected DayClock Clock;
    protected float FinalAdultSize;
    protected Vector3 AdultScale;
    protected Vector3 TeenScale;
    protected Vector3 InitialHeightScale;
    protected Vector3 StateStartScale;
    protected Vector3 StateEndScale;
    protected float CurrentGrowTime;
    protected float TotalGrowTime;
    protected float TimeSinceEating;
    protected float LastDayOfReproduction;

    public static bool PredationAllowed = true;
    public static bool ReproductionAllowed = true;

    public float YoungSize = 2f;
    public float TeenSize = 3f;
    public float AdultSizeMin = 5f;
    public float AdultSizeMax = 6f;

    public float DaysOld;
    public float DaysAsYoung = 3f;
    public float DaysAsTeen = 4f;
    public float LifeSpan = 200f;

    public float BaseSpeed;
    [SyncVar]
    public Vector3 Scale;
    [SyncVar]
    public Vector3 Velocity;

    public AIEcosystem Ecosystem;

    protected virtual void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        base.Start();

        if (!isServer)
            return;

        BirthTime = Time.time;
        Clock = FindObjectOfType<DayClock>();

        FinalAdultSize = Random.Range(AdultSizeMin, AdultSizeMax);

        var rend = GetComponent<Renderer>();
        var currentSize = rend.bounds.size.y;
        var initialScale = YoungSize / currentSize;
        InitialHeightScale = transform.localScale * initialScale;
        TeenScale = Vector3.one * TeenSize;
        AdultScale = Vector3.one * FinalAdultSize;
        transform.localScale = InitialHeightScale;
        Scale = transform.localScale;

        GrowthBrain.In(GrowthState.Young)
            .If(() => DaysOld >= DaysAsYoung)
                .GoTo(GrowthState.Teen)
            .ExecuteOnEntry(YoungStart)
            .ExecuteWhileIn(GrowthUpdate, BehaviourUpdate);

        GrowthBrain.In(GrowthState.Teen)
            .If(() => DaysOld >= DaysAsYoung + DaysAsTeen)
                .GoTo(GrowthState.Adult)
            .ExecuteOnEntry(TeenStart)
            .ExecuteWhileIn(GrowthUpdate, BehaviourUpdate);

        GrowthBrain.In(GrowthState.Adult)
            .If(() => DaysOld > LifeSpan)
                .GoTo(GrowthState.Dead)
            .ExecuteOnEntry(AdultStart)
            .ExecuteWhileIn(GrowthUpdate, BehaviourUpdate);

        GrowthBrain.In(GrowthState.Dead)
            .DoOnce(Die);

        GrowthBrain.Initialize(GrowthState.Young);
    }

    protected virtual void YoungStart()
    {
        CurrentGrowTime = 0f;
        TotalGrowTime = Clock.DaysToSeconds(DaysAsYoung);
        StateStartScale = InitialHeightScale;
        StateEndScale = TeenScale;
    }

    protected virtual void TeenStart()
    {
        CurrentGrowTime = 0f;
        TotalGrowTime = Clock.DaysToSeconds(DaysAsTeen);
        StateStartScale = TeenScale;
        StateEndScale = AdultScale;
    }

    protected virtual void AdultStart()
    {
        StateStartScale = AdultScale;
        StateEndScale = AdultScale;
    }

    protected void GrowthUpdate()
    {
        CurrentGrowTime += Time.deltaTime;
        DaysOld = Clock.SecondsToDays(Time.time - BirthTime);
        Scale = Vector3.Lerp(StateStartScale, StateEndScale, CurrentGrowTime / TotalGrowTime);
        transform.localScale = Scale;
    }

    protected abstract void BehaviourUpdate();
    protected abstract void Die();
}


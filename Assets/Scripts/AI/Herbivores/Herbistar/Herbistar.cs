using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof (HerbistarAnimAudioController))]
[SpawnableCreature("herbistar", HerbivoreType.Herbistar)]
public class Herbistar : HerbivoreBase
{
    private HerbistarAnimAudioController m_AnimAudioController;

    private float m_SwitchAnimCounter;

    public float SwitchAnimTime = 3f;
    [SyncVar] public float Scale = 1f;

    public override HerbivoreType Type
    {
        get { return HerbivoreType.Herbistar; }
    }

    protected override void Start()
    {
        if (!isServer)
        {
            base.Start();
            return;
        }

        m_AnimAudioController = GetComponent<HerbistarAnimAudioController>();
        m_AnimAudioController.OnDyingFinished += Die;

        Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.HerbistarInitialScale),
            DataStore.GetFloat(Data.HerbistarFinalScaleMin),
            DataStore.GetFloat(Data.HerbistarFinalScaleMax));

        AgeData.DaysToGrown = DataStore.GetFloat(Data.HerbistarDaysToGrown);
        AgeData.LifeSpan = DataStore.GetFloat(Data.HerbistarLifeSpan);
        BaseSpeed = DataStore.GetFloat(Data.HerbistarBaseSpeed);

        WanderParameters.Radius = DataStore.GetFloat(Data.HerbistarWanderRadius);
        WanderParameters.Distance = DataStore.GetFloat(Data.HerbistarWanderDistance);
        WanderParameters.Jitter = DataStore.GetFloat(Data.HerbistarWanderJitter);

        base.Start();
        Scale = Grower.Scale;

        BehaviourBrain.In(BehaviourState.Running)
            .If(() => Health <= 0)
                .GoTo(BehaviourState.Death)
            .ExecuteWhileIn(Wander, AnimSwitchUpdate);

        BehaviourBrain.In(BehaviourState.Death)
            .DoOnce(Die);

        BehaviourBrain.Initialize(BehaviourState.Running);
    }

    private int m_UpdateCount;
    protected override void Update()
    {
        //Debug.Log("Scale at " + m_UpdateCount++ + ": " + Scale);
        if (isClient && !isServer)
        {
            if (Scale != 0f && !float.IsNaN(Scale))
            {
                transform.localScale = new Vector3(Scale, Scale, Scale);
            }
        }

        base.Update();

        if (isServer && Grower.Scale.PercentDifference(Scale) > .03f)
        {
            Scale = Grower.Scale;
            Debug.Assert(Scale > 0f);
        }
    }

    protected override void BehaviourUpdate()
    {
        BehaviourBrain.Update(Time.deltaTime);
    }

    private void Wander()
    {
        var vel = Steering.Wander(gameObject, ref WanderParameters);

        if (vel.sqrMagnitude < .2f)
            vel = transform.forward;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vel, transform.up), 1f);
        Rigidbody.velocity = vel.normalized * BaseSpeed;
    }

    private void AnimSwitchUpdate()
    {
        m_SwitchAnimCounter += Time.deltaTime;
        if (m_SwitchAnimCounter > SwitchAnimTime)
        {
            m_SwitchAnimCounter = 0f;
            m_AnimAudioController.SwitchMoveAnimation();
        }
    }

    public override void StartDeathThrows()
    {
        m_AnimAudioController.DoDie();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        if (AgeData == null)
            AgeData = GetComponent<AgeDataComponent>();

        DataStore.SetIfDifferent(Data.HerbistarDaysToGrown, AgeData.DaysToGrown);
        DataStore.SetIfDifferent(Data.HerbistarLifeSpan, AgeData.LifeSpan);
        DataStore.SetIfDifferent(Data.HerbistarBaseSpeed, BaseSpeed);
        DataStore.SetIfDifferent(Data.HerbistarWanderRadius, WanderParameters.Radius);
        DataStore.SetIfDifferent(Data.HerbistarWanderDistance, WanderParameters.Distance);
        DataStore.SetIfDifferent(Data.HerbistarWanderJitter, WanderParameters.Jitter);
    }

    public static void ChangeHerbistarData(Data key, string value, IEnumerable<Herbistar> herbistarEnum)
    {
        var herbistars = herbistarEnum.ToList();
        switch (key)
        {
            case Data.HerbistarInitialScale:
                var initScale = float.Parse(value);
                herbistars.ForEach(b => ((ScaledGrowth)b.Grower).InitialScale = initScale);
                break;
            case Data.HerbistarFinalScaleMin:
                var scaleMin = float.Parse(value);
                herbistars.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMin;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.HerbistarFinalScaleMax:
                var scaleMax = float.Parse(value);
                herbistars.ForEach(b =>
                {
                    ((ScaledGrowth)b.Grower).FinalScaleMin = scaleMax;
                    ((ScaledGrowth)b.Grower).RecalculateFinalScale();
                });
                break;
            case Data.HerbistarWanderRadius:
                var wandRadius = float.Parse(value);
                herbistars.ForEach(h => h.WanderParameters.Radius = wandRadius);
                break;
            case Data.HerbistarWanderDistance:
                var wandDist = float.Parse(value);
                herbistars.ForEach(h => h.WanderParameters.Distance = wandDist);
                break;
            case Data.HerbistarWanderJitter:
                var wandJitter = float.Parse(value);
                herbistars.ForEach(h => h.WanderParameters.Jitter = wandJitter);
                break;
            case Data.HerbistarLifeSpan:
                var lifeSpan = float.Parse(value);
                herbistars.ForEach(c => c.AgeData.LifeSpan = lifeSpan);
                break;
            case Data.HerbistarBaseSpeed:
                var baseSpeed = float.Parse(value);
                herbistars.ForEach(c => c.BaseSpeed = baseSpeed);
                break;
            case Data.HerbistarDaysToGrown:
                var dtg = float.Parse(value);
                herbistars.ForEach(j => j.AgeData.DaysToGrown = dtg);
                break;
        }
    }
}
    

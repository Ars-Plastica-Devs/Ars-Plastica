using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Networking;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.5f)]
[RequireComponent(typeof(SnatcherAnimAudioController))]
[SpawnableCreature("embedded-snatcher", PlantType.EmbeddedSnatcher)]
[SpawnableCreature("floating-snatcher", PlantType.FloatingSnatcher)]
public class SnatcherPlant : PlantBase
{
    private ScaledGrowth m_Grower;
    private Rigidbody m_Rigidbody;
    private bool m_NoduleAlive;
    private float m_NoduleRespawnCounter;
    private float m_TimeLastEaten;
    private Vector3 m_FloatVelocity;
    private Vector3 m_FloatAngVelcoty;
    private Creature m_EatingAnimal;
    [SyncVar] private bool m_AnimalTrapped;
    private float m_IdlingTimer;
    private const float IDLE_ACTION_RATE = 5f;

    private SnatcherAnimAudioController m_AnimAudioController;

    public override PlantType Type {
        get { return Floating ? PlantType.FloatingSnatcher : PlantType.EmbeddedSnatcher; }
    }

    [HideInInspector]
    public SnatcherNodule Nodule;

    public float TendrilLength = 5f;
    public float NoduleDistance = 10f;
    public float TendrilSpeed = 2f;
    public float FloatSpeed = 1f;
    public float RotationSpeed = .5f;
    public float NoduleRespawnDelay = 10f;
    public float ChildDelay = 30f;
    public float DaysToGrown = 7f;
    public float DaysBeforeVulnerable = 5f;
    public float DaysOld;

    public bool VulnerableToPredators;
    public bool Floating;

    [SyncVar]
    public float Scale = 1f;

    public Transform NoduleTarget;
    public Transform TongueEnd;
    public Collider BodyCollider;

    protected override void Start()
    {
        if (NoduleTarget == null)
            Debug.LogError("NoduleTarget is set to null in SnatcherPlant", this);
        if (TongueEnd == null)
            Debug.LogError("TongueEnd is set to null in SnatcherPlant", this);
        if (BodyCollider == null)
            Debug.LogError("BodyCollider is set to null in SnatcherPlant", this);

        base.Start();

        m_Rigidbody = GetComponent<Rigidbody>();
        m_AnimAudioController = GetComponent<SnatcherAnimAudioController>();

        if (!isServer)
            m_Rigidbody.isKinematic = true;

        if (!isServer)
            return;

        m_AnimAudioController.OnTongueExtended += OnTongueExtended;
        m_AnimAudioController.OnAttackFinished += OnAttackFinished;
        m_AnimAudioController.OnReleaseSpore += OnReleaseSpore;
        m_AnimAudioController.OnDeathFinished += OnDeath;

        m_Grower = new ScaledGrowth(transform,
            DataStore.GetFloat(Data.SnatcherInitialScale),
            DataStore.GetFloat(Data.SnatcherFullScaleMin),
            DataStore.GetFloat(Data.SnatcherFullScaleMax));

        NoduleDistance = DataStore.GetFloat(Data.SnatcherNoduleDistance);
        NoduleRespawnDelay = DataStore.GetFloat(Data.SnatcherNoduleRespawnDelay);
        DaysToGrown = DataStore.GetFloat(Data.SnatcherDaysToGrown);
        DaysBeforeVulnerable = DataStore.GetFloat(Data.SnatcherDaysBeforeVulnerable);
        FloatSpeed = DataStore.GetFloat(Data.SnatcherFloatSpeed);
        RotationSpeed = DataStore.GetFloat(Data.SnatcherRotationSpeed);
        ChildDelay = DataStore.GetFloat(Data.SnatcherChildDelay);

        m_TimeLastEaten = Time.time;

        GrowthBrain.In(GrowthState.Growing)
            .If(() => DaysOld > DaysToGrown)
                .GoTo(GrowthState.Grown)
            .ExecuteOnEntry(GrowingStart, FloatingStart)
            .ExecuteWhileIn(GrowingUpdate, FloatingUpdate, IdleActionUpdate);

        GrowthBrain.In(GrowthState.Grown)
            /*.If(() => m_Clock.SecondsToDays(m_TimeLastEaten - Time.time) > DaysBeforeVulnerable)
                .GoTo(GrowthState.Dead)*/
            .ExecuteOnEntry(GrownStart)
            .ExecuteWhileIn(GrownUpdate, FloatingUpdate, VulnerableUpdate, IdleActionUpdate);

        GrowthBrain.In(GrowthState.Dead)
            .DoOnce(Die);

        GrowthBrain.Initialize(GrowthState.Growing);
    }

    private void Update()
    {
        if (Scale != 0f)
            transform.localScale = new Vector3(Scale, Scale, Scale);

        if (!isServer)
            return;

        DaysOld = DayClock.Singleton.SecondsToDays(Time.time - BirthTime);

        GrowthBrain.Update(Time.deltaTime);

        if (isServer && m_Grower.Scale.PercentDifference(Scale) > .03f)
            Scale = m_Grower.Scale;
    }

    private void LateUpdate()
    {
        if (m_EatingAnimal != null && m_AnimalTrapped)
        {
            m_EatingAnimal.transform.position = TongueEnd.position;
        }
    }

    private void FixedUpdate()
    {
        if (!isServer)
            return;

        transform.Translate(m_Rigidbody.velocity * Time.fixedDeltaTime);

        var v = transform.rotation.eulerAngles;
        v += m_Rigidbody.angularVelocity * Time.fixedDeltaTime;
        transform.rotation = Quaternion.Euler(v);
    }

    private void GrowingStart()
    {
        m_Grower.StartGrowing();
        Scale = m_Grower.Scale;
    }

    private void GrowingUpdate()
    {
        m_Grower.GrowthUpdate(DaysOld / DaysToGrown);
    }

    private void GrownStart()
    {
        m_TimeLastEaten = Time.time; // Set this so we don't instantly become vulnerable upon growing up
        m_AnimAudioController.DoBelch();
    }

    private void GrownUpdate()
    {
        if (!m_NoduleAlive)
        {
            m_NoduleRespawnCounter += Time.deltaTime;
            if (m_NoduleRespawnCounter > NoduleRespawnDelay)
            {
                m_NoduleRespawnCounter = 0;
                m_AnimAudioController.DoBelch();
            }
        }
    }

    private void FloatingStart()
    {
        if (!Floating)
            return;

        m_FloatVelocity = Random.onUnitSphere * FloatSpeed;
        m_FloatAngVelcoty = Random.onUnitSphere * RotationSpeed;
    }

    private void FloatingUpdate()
    {
        if (!Floating)
            return;

        m_Rigidbody.velocity = m_FloatVelocity;
        m_Rigidbody.angularVelocity = m_FloatAngVelcoty;

        if (Nodule != null)
            Nodule.TargetLocation = NoduleTarget.position;
    }

    private void IdleActionUpdate()
    {
        if (!m_AnimAudioController.Idling)
            return;

        m_IdlingTimer += Time.deltaTime;
        if (m_IdlingTimer > IDLE_ACTION_RATE)
        {
            m_IdlingTimer = 0f;
            m_AnimAudioController.DoIdleAction();
        }
    }

    private void Die()
    {
        RemoveNodule();

        m_AnimAudioController.DoDeath();
    }

    private void OnDeath()
    {
        Ecosystem.Singleton.KillPlant(this);
    }

    private void VulnerableUpdate()
    {
        VulnerableToPredators = DayClock.Singleton.SecondsToDays(Time.time - m_TimeLastEaten) > DaysBeforeVulnerable;
    }

    private void OnReleaseSpore()
    {
        Nodule = (SnatcherNodule)Ecosystem.Singleton.SpawnNodule(NoduleTarget.position, Quaternion.identity, NoduleType.Snatcher);
        Nodule.transform.rotation = transform.rotation;
        Nodule.OnEat += OnNoduleEaten;
        m_NoduleAlive = true;

        Nodule.GetComponent<NetworkedChild>().SetParent(transform);
    }

    private void RemoveNodule()
    {
        if (!m_NoduleAlive) return;

        Nodule.OnEat -= OnNoduleEaten;
        Nodule.transform.SetParent(null, true);
        Ecosystem.Singleton.RemoveNodule(Nodule);
        Nodule = null;
        m_NoduleAlive = false;
    }

    private void OnNoduleEaten(Creature eater)
    {
        if (!isServer) return;

        m_EatingAnimal = eater;
        RpcTrapAnimal(eater.gameObject);
        RemoveNodule();
        m_AnimAudioController.DoAttack();
    }

    private void OnTongueExtended()
    {
        if (!isServer) return;

        m_EatingAnimal.transform.position = TongueEnd.position;
        m_AnimalTrapped = true;
    }

    [ClientRpc]
    private void RpcTrapAnimal(GameObject obj)
    {
        m_EatingAnimal = obj.GetComponent<Creature>();
    }

    private void OnAttackFinished()
    {
        if (!isServer) return;

        if (m_EatingAnimal != null && m_AnimalTrapped)
        {
            Ecosystem.Singleton.KillHerbivore((HerbivoreBase)m_EatingAnimal);

            m_TimeLastEaten = Time.time;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.SnatcherNoduleRespawnDelay, NoduleRespawnDelay);
        DataStore.SetIfDifferent(Data.SnatcherDaysToGrown, DaysToGrown);
        DataStore.SetIfDifferent(Data.SnatcherDaysBeforeVulnerable, DaysBeforeVulnerable);
        DataStore.SetIfDifferent(Data.SnatcherFloatSpeed, FloatSpeed);
        DataStore.SetIfDifferent(Data.SnatcherRotationSpeed, RotationSpeed);
        DataStore.SetIfDifferent(Data.SnatcherChildDelay, ChildDelay);
    }

    public static void ChangeSnatcherData(Data key, string value, IEnumerable<SnatcherPlant> snatchersEnum)
    {
        var snatchers = snatchersEnum.ToList();
        switch (key)
        {
            case Data.SnatcherNoduleRespawnDelay:
                var delay = float.Parse(value);
                snatchers.ForEach(s => s.NoduleRespawnDelay = delay);
                break;
            case Data.SnatcherDaysToGrown:
                var days = float.Parse(value);
                snatchers.ForEach(s => s.DaysToGrown = days);
                break;
            case Data.SnatcherInitialScale:
                var init = float.Parse(value);
                snatchers.ForEach(s => s.m_Grower.InitialScale = init);
                break;
            case Data.SnatcherFullScaleMin:
                var min = float.Parse(value);
                snatchers.ForEach(s =>
                {
                    s.m_Grower.FinalScaleMin = min;
                    s.m_Grower.RecalculateFinalScale();
                });
                break;
            case Data.SnatcherFullScaleMax:
                var max = float.Parse(value);
                snatchers.ForEach(s =>
                {
                    s.m_Grower.FinalScaleMin = max;
                    s.m_Grower.RecalculateFinalScale();
                });
                break;
            case Data.SnatcherDaysBeforeVulnerable:
                var daysToStarve = float.Parse(value);
                snatchers.ForEach(s => s.DaysBeforeVulnerable = daysToStarve);
                break;
            case Data.SnatcherFloatSpeed:
                var floatSpeed = float.Parse(value);
                snatchers.ForEach(s => s.FloatSpeed = floatSpeed);
                break;
            case Data.SnatcherRotationSpeed:
                var rotSpeed = float.Parse(value);
                snatchers.ForEach(s => s.RotationSpeed = rotSpeed);
                break;
            case Data.SnatcherChildDelay:
                var childDelay = float.Parse(value);
                snatchers.ForEach(s => s.ChildDelay = childDelay);
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer || other.isTrigger)
            return;

        if (other.gameObject.tag == "Herbivore")
        {
            //m_HerbivoresInRange.Add(other.gameObject);
        }
        else if (other.gameObject.tag == "Player" || other.gameObject.tag == "RemotePlayer")
        {
            m_AnimAudioController.DoWardOff();
        }
    }

    /*private void OnTriggerExit(Collider other)
    {
        if (!isServer || other.isTrigger)
            return;

        if (other.gameObject.tag == "Herbivore")
        {
            //m_HerbivoresInRange.Remove(other.gameObject);
        }
        else if (other.gameObject.tag == "Player" || other.gameObject.tag == "RemotePlayer")
        {
        }
    }*/

    private void OnDestory()
    {
        if (isServer)
        {
            RemoveNodule();
        }
    }
}

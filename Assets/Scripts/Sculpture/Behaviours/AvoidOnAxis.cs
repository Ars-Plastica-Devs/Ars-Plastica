using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 1, sendInterval = 0.1f)]
public class AvoidOnAxis : CubeBehaviour
{
    [System.Serializable]
    public struct AvoidParameters
    {
        [Tooltip("The range from it's starting location this will move")]
        public float DistanceToTravel;
        [Tooltip("The max/min variation to be added to the distance this object travels")]
        public float DistanceVariation;
        [Tooltip("The speed at which this object will travel")]
        public float Speed;
        [Tooltip("The max/min variation to be added to the speed at which this object travels")]
        public float SpeedVariation;

        //We might use this to have a delay when the player moves within/out of range
        //Dropped this feature for now because it wasn't as easy as I had hoped
        [Tooltip("NOT IMPLEMENTED CURRENTLY")]
        public float MovementDelayRange;

        public AvoidParameters(float distToTravel, float distVariation, float speed, float speedVariation, float moveDelayRange)
        {
            DistanceToTravel = distToTravel;
            DistanceVariation = distVariation;
            Speed = speed;
            SpeedVariation = speedVariation;
            MovementDelayRange = moveDelayRange;
        }
    }

    private enum State
    {
        Avoiding,
        Returning,
        Idle,
        Away
    }

    //This count should help us handle cases where there are multiple avatars in range
    private int m_TriggeredCount;
    private Vector3 m_StartPosition;
    private Vector3 m_TargetPosition;
    private State m_State = State.Idle;
    private float m_TimeProgress;
    private float m_TimeNeeded;
    private float m_DistanceWithVariaton;
    private float m_SpeedWithVariaton;
    private SphereCollider m_Collider;

    public string[] TriggeringTags = { "Player", "RemotePlayer" };
    public AvoidParameters AvoidParams = new AvoidParameters(20f, 6f, 6f, 2f, .5f);

    [SyncVar] private NetworkInstanceId m_ParentNetID;

    public override NetworkInstanceId ParentNetID
    {
        get { return m_ParentNetID; }
        set { m_ParentNetID = value; }
    }
   
    private void Start()
    {
        m_Collider = GetComponent<SphereCollider>();
    }

    public void Initialize()
    {
        enabled = true;
        m_Collider = GetComponent<SphereCollider>();
        m_Collider.enabled = true;
    }

    public void SetInteractionRadius(float r)
    {
        m_Collider.radius = r;
    }

    public void ActivateBehaviour()
    {
        //Set these for convenience
        var distanceToTravel = AvoidParams.DistanceToTravel;
        var distanceVariation = AvoidParams.DistanceVariation;
        var speed = AvoidParams.Speed;
        var speedVariation = AvoidParams.SpeedVariation;

        if (m_State == State.Idle)
        {
            //Add our variation for speed and distance
            m_DistanceWithVariaton = distanceToTravel + (Random.value * distanceVariation) -
                                     (Random.value * distanceVariation);
            m_SpeedWithVariaton = speed + (Random.value * speedVariation) -
                                     (Random.value * speedVariation);

            m_State = State.Avoiding;
            m_TimeNeeded = m_DistanceWithVariaton / m_SpeedWithVariaton;
            m_TimeProgress = 0;

            var axis = Random.Range(0, 6);
            m_StartPosition = transform.localPosition;

            Vector3 offset;
            switch (axis)
            {
                case 0:
                    offset = new Vector3(m_DistanceWithVariaton, 0, 0);
                    break;
                case 1:
                    offset = new Vector3(-m_DistanceWithVariaton, 0, 0);
                    break;
                case 2:
                    offset = new Vector3(0, m_DistanceWithVariaton, 0);
                    break;
                case 3:
                    offset = new Vector3(0, -m_DistanceWithVariaton, 0);
                    break;
                case 4:
                    offset = new Vector3(0, 0, m_DistanceWithVariaton);
                    break;
                case 5:
                    offset = new Vector3(0, 0, -m_DistanceWithVariaton);
                    break;
                default:
                    offset = new Vector3(m_DistanceWithVariaton, 0, 0);
                    break;
            }

            m_TargetPosition = m_StartPosition + offset;
            RpcActivate(m_StartPosition, m_TargetPosition, m_TimeProgress, m_TimeNeeded, m_State);
        }
        else if (m_State == State.Returning)
        {
            m_State = State.Avoiding;
            m_TimeNeeded = m_DistanceWithVariaton / m_SpeedWithVariaton;
            m_TimeProgress = ((transform.localPosition - m_StartPosition).magnitude / m_DistanceWithVariaton) * m_TimeNeeded;

            RpcActivate(m_StartPosition, m_TargetPosition, m_TimeProgress, m_TimeNeeded, m_State);
        }
    }

    [ClientRpc]
    private void RpcActivate(Vector3 start, Vector3 target, float timeProgress, float timeNeeded, State state)
    {
        if (!enabled) enabled = true;
        m_StartPosition = start;
        m_TargetPosition = target;
        m_TimeProgress = timeProgress;
        m_TimeNeeded = timeNeeded;
        m_State = state;
    }

    public void DeactivateBehaviour()
    {
        if (m_State == State.Idle)
        {
            return;
        }
        m_State = State.Returning;
        RpcSetState(m_State);
    }

    [ClientRpc]
    private void RpcSetState(State state)
    {
        m_State = state;
    }

    public void Reset()
    {
        transform.localPosition = m_StartPosition;
        m_TargetPosition = m_StartPosition;
        m_Collider.center = Vector3.zero;
        m_State = State.Idle;
        m_TimeNeeded = 0f;
        m_TimeProgress = 0f;
        m_DistanceWithVariaton = 0f;
        m_SpeedWithVariaton = 0f;
    }

    private static bool shown = false;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !shown)
        {
            shown = true;
            Debug.Log("Parent name: " + transform.parent.name);
            Debug.Log("Local Position: " + transform.localPosition);
            Debug.Log("Global Position: " + transform.position);
        }

        if (m_State == State.Avoiding)
        {
            m_TimeProgress += Time.deltaTime;
            var lerpTarget = Vector3.Lerp(m_StartPosition, m_TargetPosition, m_TimeProgress / m_TimeNeeded);
            m_Collider.center -= (lerpTarget - transform.localPosition);
            transform.localPosition = lerpTarget;

            if (m_TimeProgress / m_TimeNeeded > 1f)
            {
                transform.localPosition = m_TargetPosition;
                m_State = State.Away;
            }
        }
        else if (m_State == State.Returning)
        {
            //Tick time backwards so we Lerp in reverse
            m_TimeProgress -= Time.deltaTime;
            var lerpTarget = Vector3.Lerp(m_StartPosition, m_TargetPosition, m_TimeProgress / m_TimeNeeded);
            m_Collider.center -= (lerpTarget - transform.localPosition);
            transform.localPosition = lerpTarget;

            if (m_TimeProgress / m_TimeNeeded < 0f)
            {
                transform.localPosition = m_StartPosition;
                m_Collider.center = Vector3.zero;
                m_State = State.Idle;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || !isServer) return;

        if (TriggeringTags.Contains(other.gameObject.tag))
        {
            m_TriggeredCount++;

            if (m_TriggeredCount == 1)
            {
                ActivateBehaviour();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled || !isServer) return;

        if (TriggeringTags.Contains(other.gameObject.tag))
        {
            m_TriggeredCount--;

            if (m_TriggeredCount == 0)
            {
                DeactivateBehaviour();
            }
        }
    }
}

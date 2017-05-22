using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class FloatingNodule : Nodule
{
    private Vector3 m_Velocity = Vector3.zero;
    private float m_Counter;
    public float SendRate = 1f;

    [SyncVar(hook="OnFloatSpeedChange")]
    public float FloatSpeed = 1f;

    private void Start()
    {
        Type = NoduleType.Floating;

        if (!isServer) return;

        var speed = DataStore.GetFloat(Data.NoduleFloatingFloatSpeed);
        FloatSpeed = speed + (Random.Range(-.05f * speed, .05f * speed));
        SendRate = DataStore.GetFloat(Data.NoduleFloatingSendRate);
        SendRate += (Random.Range(-.05f * SendRate, .05f * SendRate)); //Add variability so that send times diverge

        m_Velocity = new Vector3(0, FloatSpeed, 0);
        RpcPostMovementData(transform.position/*, m_Velocity*/);
    }

    public override void OnStartClient()
    {
        m_Velocity = new Vector3(0, FloatSpeed, 0);
    }

    private void Update()
    {
        if (!isServer) return;

        m_Counter += Time.deltaTime;
        if (m_Counter > SendRate)
        {
            m_Counter = 0f;
            RpcPostMovementData(transform.position/*, m_Velocity*/);
        }
    }

    private void FixedUpdate()
    {
        transform.position += m_Velocity * Time.fixedDeltaTime;
    }

    private void OnFloatSpeedChange(float newSpeed)
    {
        FloatSpeed = newSpeed;
        m_Velocity = new Vector3(0, FloatSpeed, 0);
    }

    [ClientRpc]
    private void RpcPostMovementData(Vector3 pos/*, Vector3 vel*/)
    {
        transform.position = pos;
        //m_Velocity = vel;
    }

    public override void GetEaten(Creature eater)
    {
        Ecosystem.Singleton.RemoveNodule(this);
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.NoduleFloatingFloatSpeed, FloatSpeed);
        DataStore.SetIfDifferent(Data.NoduleFloatingSendRate, SendRate);
    }

    public static void ChangeNoduleData(Data key, string value, IEnumerable<GameObject> nodules)
    {
        switch (key)
        {
            case Data.NoduleFloatingFloatSpeed:
                var speed = float.Parse(value);
                foreach (var nodule in nodules.Select(n => n.GetComponent<FloatingNodule>()))
                {
                    nodule.FloatSpeed = speed + (Random.Range(-.05f * speed, .05f * speed));
                    nodule.m_Velocity = new Vector3(0, nodule.FloatSpeed, 0);
                }
                break;
            case Data.NoduleFloatingSendRate:
                var rate = float.Parse(value);
                foreach (var nodule in nodules.Select(n => n.GetComponent<FloatingNodule>()))
                {
                    nodule.SendRate = rate;
                    nodule.SendRate += (Random.Range(-.05f * rate, .05f * rate));//Add variability so that send times diverge
                }
                break;
        }
    }
}

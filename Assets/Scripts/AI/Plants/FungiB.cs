using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SpawnableCreature("fungi-b", PlantType.FungiB)]
public class FungiB : PlantBase
{
    public override PlantType Type
    {
        get { return PlantType.FungiB; }
    }

    private Vector3 m_InitialScale;

    public WanderParameters Parameters;
    public float Speed;
    public Vector3 Rotation;
    public float ScaleFactor = 1f;

    protected override void Start()
    {
        m_InitialScale = transform.localScale;

        if (!isServer)
        {
            base.Start();
            return;
        }

        ScaleFactor = DataStore.GetFloat(Data.FungiBScaleFactor);
        ApplyScaleFactor();
    }

    private void FixedUpdate()
    {
        transform.position += Steering.Wander(gameObject, ref Parameters)
                                * Speed * Time.fixedDeltaTime;
        transform.Rotate(Rotation * Time.fixedDeltaTime, Space.Self);
    }

    private void ApplyScaleFactor()
    {
        transform.localScale = (m_InitialScale * ScaleFactor);
    }

    private void OnValidate()
    {
        if (Application.isPlaying || isClient) return;

        DataStore.SetIfDifferent(Data.FungiBScaleFactor, ScaleFactor);
    }

    public static void ChangeFungiBData(Data key, string value, IEnumerable<FungiB> fungiEnumberable)
    {
        var fungi = fungiEnumberable.ToList();
        switch (key)
        {
            case Data.FungiBScaleFactor:
                var sf = float.Parse(value);
                fungi.ForEach(f => f.ScaleFactor = sf);
                break;
        }
    }
}

using UnityEngine;
using UnityEngine.Networking;

public class Tendril : NetworkBehaviour
{
    public enum TendrilState
    {
        Retracted, Extended, Moving
    }

    public GameObject TendrilEnd;

    public float MinTendrilLengthScale;
    public float MaxTendrilLengthScale;
    public TendrilState State = TendrilState.Extended;

    public Vector3 EndPoint()
    {
        return TendrilEnd.transform.position;
    }

    public void ShrinkTendril(float amount)
    {
        var scale = transform.localScale;
        if (scale.y - amount < MinTendrilLengthScale)
        {
            transform.localScale = new Vector3(scale.x, MinTendrilLengthScale, scale.z);
            State = TendrilState.Retracted;
        }
        else
        {
            transform.localScale = new Vector3(scale.x, scale.y - amount, scale.z);
            State = TendrilState.Moving;
        }
    }

    public void GrowTendril(float amount)
    {
        var scale = transform.localScale;
        if (scale.y + amount > MaxTendrilLengthScale)
        {
            transform.localScale = new Vector3(scale.x, MaxTendrilLengthScale, scale.z);
            State = TendrilState.Extended;
        }
        else
        {
            transform.localScale = new Vector3(scale.x, scale.y + amount, scale.z);
            State = TendrilState.Extended;
        }
    }

    public void ExtendFully()
    {
        var scale = transform.localScale;
        transform.localScale = new Vector3(scale.x, MaxTendrilLengthScale, scale.z);
        State = TendrilState.Extended;
    }
}

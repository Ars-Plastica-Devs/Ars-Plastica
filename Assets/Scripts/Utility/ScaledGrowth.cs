using System;
using Assets.Scripts.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

public class ScaledGrowth : IGrower
{
    [Flags]
    public enum GrowthAxes
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        All = X | Y | Z
    }

    private readonly Transform m_Owner;
    private float m_FinalScale;

    private const float INITIAL_SCALE_RANDOMNESS = .2f;

    /// <summary>
    /// Represents which axes should be scaled
    /// </summary>
    private readonly GrowthAxes m_Axes;

    public float InitialScale;
    public float FinalScaleMin;
    public float FinalScaleMax;

    public GrowthState State { get; private set; }
    public float Scale { get; private set; }

    public ScaledGrowth(Transform owner, float initScale, float finalMin, float finalMax, GrowthAxes axes = GrowthAxes.All)
    {
        m_Owner = owner;
        m_Axes = axes;

        InitialScale = initScale.Randomize(INITIAL_SCALE_RANDOMNESS);
        FinalScaleMin = finalMin;
        FinalScaleMax = finalMax;

        Debug.Assert(InitialScale > 0f, "Initial Scale was less than 0f: " + InitialScale);
        Debug.Assert(FinalScaleMin < FinalScaleMax, "FinalScaleMin was less than FinalScaleMax: " + FinalScaleMin + " " + FinalScaleMax);
    }

    public void StartGrowing()
    {
        m_Owner.localScale = new Vector3(InitialScale, InitialScale, InitialScale);
        Scale = InitialScale;
        m_FinalScale = Random.Range(FinalScaleMin, FinalScaleMax);
    }

    public void GrowthUpdate(float percentOfGrowth)
    {
        Scale = Mathf.Lerp(InitialScale, m_FinalScale, percentOfGrowth);

        var scale = Vector3.zero;

        if ((m_Axes & GrowthAxes.All) == GrowthAxes.All)
        {
            scale = new Vector3(Scale, Scale, Scale);
        }
        else
        {
            //Apply the scale to individual axes
            if ((m_Axes & GrowthAxes.X) == GrowthAxes.X)
                scale += new Vector3(Scale, 0, 0);
            if ((m_Axes & GrowthAxes.Y) == GrowthAxes.Y)
                scale += new Vector3(0, Scale, 0);
            if ((m_Axes & GrowthAxes.Z) == GrowthAxes.Z)
                scale += new Vector3(0, 0, Scale);

            //if an axis is zero, we keep the current scale on that axis
            if (scale.x == 0)
                scale.x = m_Owner.localScale.x;
            if (scale.y == 0)
                scale.y = m_Owner.localScale.y;
            if (scale.z == 0)
                scale.z = m_Owner.localScale.z;
        }

        Debug.Assert(scale.x > 0f, "scale was less than 0f: " + scale.x);

        m_Owner.localScale = scale;

        State = (percentOfGrowth > 1f) ? GrowthState.Grown : GrowthState.Growing;
    }

    public void RecalculateFinalScale()
    {
        if (FinalScaleMax < FinalScaleMin)
            FinalScaleMax = FinalScaleMin;
        m_FinalScale = Random.Range(FinalScaleMin, FinalScaleMax);
    }
}

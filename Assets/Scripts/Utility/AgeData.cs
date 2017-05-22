using System;

[Serializable]
public struct AgeData
{
    public float LifeSpan;
    public float DaysToGrown;
    public float DaysOld;

    public AgeData(float lifeSpan, float daysToGrown, float daysOld)
    {
        LifeSpan = lifeSpan;
        DaysToGrown = daysToGrown;
        DaysOld = daysOld;
    }
}
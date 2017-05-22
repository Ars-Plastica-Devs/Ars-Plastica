public enum GrowthState
{
    Growing,
    Grown
}

public interface IGrower
{
    GrowthState State { get; }
    float Scale { get; }

    void StartGrowing();
    void GrowthUpdate(float percentOfGrowth);
}

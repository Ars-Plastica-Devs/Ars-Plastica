using System.Collections.Generic;

public abstract class PlantBase : Creature
{
    private struct GrowthStateComparer : IEqualityComparer<GrowthState>
    {
        public bool Equals(GrowthState x, GrowthState y)
        {
            return x == y;
        }

        public int GetHashCode(GrowthState obj)
        {
            return (int)obj;
        }
    }

    private static readonly GrowthStateComparer GStateComparer = new GrowthStateComparer();

    protected enum GrowthState
    {
        Growing,
        Grown,
        Dead
    }

    protected readonly FSM<GrowthState> GrowthBrain = new FSM<GrowthState>(GStateComparer);

    public abstract PlantType Type { get; }
}

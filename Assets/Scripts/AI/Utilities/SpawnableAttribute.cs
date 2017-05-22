using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SpawnableCreatureAttribute : Attribute
{
    public Type PlacerType;
    public string Name;
    public Enum Type;

    private SpawnableCreatureAttribute(string name, Enum type, Type spawnPlacerType)
    {
        if (spawnPlacerType != null && !typeof(SpawnablePlacer).IsAssignableFrom(spawnPlacerType))
            throw new Exception("Cannot use the given type as a SpawnablePlacer");

        Name = name;
        Type = type;
        PlacerType = spawnPlacerType;
    }

    public SpawnableCreatureAttribute(string name, HerbivoreType type, Type spawnPlacerType = null)
        : this(name, (Enum)type, spawnPlacerType)
    {
    }

    public SpawnableCreatureAttribute(string name, CarnivoreType type, Type spawnPlacerType = null)
        : this(name, (Enum)type, spawnPlacerType)
    {
    }

    public SpawnableCreatureAttribute(string name, PlantType type, Type spawnPlacerType = null)
        : this(name, (Enum) type, spawnPlacerType)
    {
    }
}

using System.Linq;

public abstract class FloatGrassCluster : PlantBase
{
    protected FloatGrassBlade[] Blades;

    public int MinBladecount;
    public int MaxBladeCount;
    public float BladeSpawnHorizontalRange;
    public float BladeSpawnVerticalRange;
    public float FloatSpeed;
    public float FloatRange;

    protected void UpdateBlades(float dt)
    {
        var needsCompacting = false;
        for (var i = 0; i < Blades.Length; i++)
        {
            if (Blades[i] == null)
            {
                needsCompacting = true;
                continue;
            }

            Blades[i].BladeUpdate(dt);
        }

        if (needsCompacting)
            CompactBladesArray();
    }

    protected void FixedUpdateBlades()
    {
        var needsCompacting = false;
        for (var i = 0; i < Blades.Length; i++)
        {
            if (Blades[i] == null)
            {
                needsCompacting = true;
                continue;
            }

            Blades[i].BladeFixedUpdate();
        }

        if (needsCompacting)
            CompactBladesArray();
    }

    /// <summary>
    /// Removes all null references from the Blades array, 
    /// and resizes the array appropriately
    /// </summary>
    private void CompactBladesArray()
    {
        Blades = Blades.Where(b => b != null).ToArray();
    }

    public override void OnNetworkDestroy()
    {
        if (Blades == null)
            return;

        foreach (var blade in Blades.Where(b => b != null))
        {
            Ecosystem.Singleton.KillPlant(blade);
        }

        base.OnNetworkDestroy();
    }

    private void OnDestroy()
    {
        if (Blades == null)
            return;

        foreach (var blade in Blades.Where(b => b != null))
        {
            Destroy(blade.gameObject);
        }
    }
}

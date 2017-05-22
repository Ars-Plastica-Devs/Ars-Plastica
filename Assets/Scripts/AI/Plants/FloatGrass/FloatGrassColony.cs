using System.Linq;

public abstract class FloatGrassColony : PlantBase
{
    //This should have so much more.... but Unity can't support
    //Generic NetworkBehaviours!
    protected FloatGrassCluster[] Clusters;
    
    public int MinClusterCount;
    public int MaxClusterCount;
    public float ClusterSpawnHorizontalRange;
    public float ClusterSpawnVerticalRange;

    public override void OnNetworkDestroy()
    {
        if (Clusters == null)
            return;

        foreach (var c in Clusters.Where(t => t != null))
        {
            Ecosystem.Singleton.KillPlant(c);
        }

        base.OnNetworkDestroy();
    }

    private void OnDestroy()
    {
        if (Clusters == null)
            return;

        foreach (var c in Clusters.Where(t => t != null))
        {
            Destroy(c.gameObject);
        }
    }
}

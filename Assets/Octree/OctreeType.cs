namespace Assets.Octree
{
    //If you want to have multiple octrees for different classes of enemies/objects/whatever
    //add an entry here so that you can use the OctreeManager to easily grab the
    //relevant Octree
    public enum OctreeType
    {
        Player,
        Herbivore,
        Carnivore,
        Nodule
    }
}

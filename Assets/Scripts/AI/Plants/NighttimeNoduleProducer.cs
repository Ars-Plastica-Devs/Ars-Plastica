public class NighttimeNoduleProducer : NoduleProducer
{
    private void Start()
    {
        DayClock.Singleton.OnNight += OnNight;
    }

    private void OnNight()
    {
        StartEmittingNodules();
    }

    private void OnDestory()
    {
        DayClock.Singleton.OnNight -= OnNight;
    }
}

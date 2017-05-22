public class DaytimeNoduleProducer : NoduleProducer
{
    private void Start()
    {
        DayClock.Singleton.OnDay += OnDay;
    }

    private void OnDay()
    {
        StartEmittingNodules();
    }

    private void OnDestory()
    {
        DayClock.Singleton.OnDay -= OnDay;
    }
}

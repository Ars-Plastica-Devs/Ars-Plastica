using System.Collections.Generic;

public interface IProximitySensor<T>
{
    float Range { get; set; }
    float RefreshRate { get; set; }

    T Closest { get; }
    HashSet<T> KClosest { get; }

    void SensorUpdate();
    void ForceUpdate();
}

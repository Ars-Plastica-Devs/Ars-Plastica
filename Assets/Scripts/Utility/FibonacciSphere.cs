using UnityEngine;

public static class FibonacciSphere
{
    //Adapted from http://stackoverflow.com/a/26127012/6324364
    public static Vector3[] Sample(uint samples, bool randomize = true)
    {
        var rnd = 1;
        if (randomize)
            rnd = (int)(Random.value * samples);

        var points = new Vector3[samples];
        var offset = 2f / samples;
        var increment = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (var i = 0; i < samples; i++)
        {
            var y = ((i * offset) - 1f) + (offset / 2f);
            var r = Mathf.Sqrt(1 - (y * y));

            var phi = ((i + rnd) % samples) * increment;

            var x = Mathf.Cos(phi) * r;
            var z = Mathf.Sin(phi) * r;

            points[i] = new Vector3(x, y, z);
        }

        return points;
    }
}

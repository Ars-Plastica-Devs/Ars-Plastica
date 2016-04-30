using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct WanderParameters
{
    public float Jitter;
    public float Radius;
    public float Distance;
    public Vector3 WanderTarget;

    public WanderParameters(float jitter, float radius, float distance)
    {
        Jitter = jitter;
        Radius = radius;
        Distance = distance;
        WanderTarget = Random.onUnitSphere;
    }
}

public static class Steering
{
    public static Vector3 Cohesion(GameObject self, ICollection<GameObject> others)
    {
        var cohesionVel = others.Aggregate(Vector3.zero, (current, herb) => current + herb.transform.position);
        cohesionVel /= others.Count;
        cohesionVel -= self.transform.position;

        return cohesionVel.normalized;
    }

    public static Vector3 Cohesion(GameObject self, ICollection<GameObject> others, out Vector3 cohesionCenter)
    {
        var cohesionVel = others.Where(o => o != null).Aggregate(Vector3.zero, (current, herb) => current + herb.transform.position);
        cohesionVel /= others.Count;
        cohesionCenter = cohesionVel;
        cohesionVel -= self.transform.position;

        return cohesionVel.normalized;
    }

    public static Vector3 Separation(GameObject self, ICollection<GameObject> others)
    {
        return -1f * Cohesion(self, others);
    }

    public static Vector3 Wander(GameObject obj, ref WanderParameters parameters)
    {
        //Randomly adjust wanderTarget, and then constrain it to a circle of radius Radius
        parameters.WanderTarget += Random.insideUnitSphere * parameters.Jitter;
        parameters.WanderTarget = parameters.WanderTarget.normalized * parameters.Radius;

        //Get that target in world distance
        var targetLocal = parameters.WanderTarget + new Vector3(0, 0, parameters.Distance);
        var targetWorld = obj.transform.TransformPoint(targetLocal);

        return (targetWorld - obj.transform.position).normalized;
    }

    public static Vector3 Pursuit(GameObject obj, GameObject target, float maxSpeed)
    {
        var tRigidbody = target.GetComponent<Rigidbody>();

        var dist = target.transform.position - obj.transform.position;
        var t = dist.magnitude / maxSpeed;

        var futurePosition = target.transform.position + ((tRigidbody != null) 
                                                            ? tRigidbody.velocity * t 
                                                            : Vector3.zero);

        return (futurePosition - obj.transform.position).normalized;
    }

    public static Vector3 Evade(GameObject obj, GameObject target, float maxSpeed)
    {
        return -1f * Pursuit(obj, target, maxSpeed);
    }
}

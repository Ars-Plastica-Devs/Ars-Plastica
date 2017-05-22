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
        WanderTarget = Vector3.zero;
    }
}

[Serializable]
public struct FlockingOptions
{
    public float AlignmentWeight;
    public float CohesionWeight;
    public float WanderWeight;

    public float MinDispersion;
    public float MaxDispersion;
    [HideInInspector] public float MinDispersionSquared;
    [HideInInspector] public float MaxDispersionSquared;

    public FlockingOptions(float align, float cohere, float wander, float minDispersion, float maxDispersion)
    {
        AlignmentWeight = align;
        CohesionWeight = cohere;
        WanderWeight = wander;

        MinDispersion = minDispersion;
        MaxDispersion = maxDispersion;

        MinDispersionSquared = minDispersion * minDispersion;
        MaxDispersionSquared = maxDispersion * maxDispersion;
    }
}

public static class Steering
{
    /*public static Vector3 Cohesion(GameObject self, ICollection<GameObject> others)
    {
        var cohesionVel = others.Aggregate(Vector3.zero, (current, herb) => current + herb.transform.position);
        cohesionVel /= others.Count;
        cohesionVel -= self.transform.position;

        return cohesionVel.normalized;
    }*/

    public static Vector3 Seek(GameObject self, Vector3 target)
    {
        return Seek(self.transform, target);
    }

    public static Vector3 Seek(Transform self, Vector3 target)
    {
        return (target - self.position).normalized;
    }

    public static Vector3 Cohesion<T>(GameObject self, ICollection<T> others, out Vector3 cohesionCenter)
        where T : Component
    {
        //Slight micro-optimization here (over a Vector3), but this is called quite often.
        float x = 0, y = 0, z = 0;
        //NOTE: We have tested removing the LINQ expression from this foreach loop - any difference
        //in performance or memory allocation was indetectable.
        foreach (var p in others.Select(other => other != null && other.transform != null ? other.transform.position : Vector3.zero))
        {
            x += p.x;
            y += p.y;
            z += p.z;
        }

        var cohesionVel = new Vector3(x, y, z);
        cohesionVel /= others.Count;
        cohesionCenter = cohesionVel;
        cohesionVel -= self.transform.position;

        return cohesionVel.normalized;
    }

    /*public static Vector3 Separation(GameObject self, ICollection<GameObject> others)
    {
        return -1f * Cohesion(self, others);
    }*/

    public static Vector3 Alignment<T>(GameObject self, ICollection<T> others)
        where T : Component
    {
        var avgVel = Vector3.zero;
        foreach (var obj in others)
        {
            Rigidbody rigidbody;

            try
            {
                //TODO: Some tests say we need this try/catch... some say we do not...
                //Unity is sometimes throwing an exception from it's own internal code here
                rigidbody = obj.GetComponent<Rigidbody>();
            }
            catch (NullReferenceException)
            {
                continue;
            }

            if (rigidbody == null)
                continue;

            avgVel = avgVel + rigidbody.velocity;
        }
        //avgVel /= others.Count;

        return avgVel.normalized;
    }

    public static Vector3 Wander(GameObject obj, ref WanderParameters parameters)
    {
        //Randomly adjust wanderTarget, and then constrain it to a circle of radius Radius
        parameters.WanderTarget += Random.onUnitSphere * parameters.Jitter;
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

        Debug.Assert(!float.IsNaN(t));

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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameObjectExtensions
{
    public static IEnumerable<GameObject> GetGameObjectsWithTags(params string[] tags)
    {
        IEnumerable<GameObject> objs = new HashSet<GameObject>();
        return tags.Aggregate(objs, (current, t) => current.Concat(GameObject.FindGameObjectsWithTag(t)));
    }

    public static IEnumerable<GameObject> GetChildrenWhere(this GameObject obj, Func<GameObject, bool> pred)
    {
        var children = new List<GameObject>();
        for (var i = 0; i < obj.transform.childCount; i++)
        {
            var child = obj.transform.GetChild(i).gameObject;

            if (pred(child))
                children.Add(child);

            children.AddRange(GetChildrenWhere(child, pred));
        }
        return children;
    }
}

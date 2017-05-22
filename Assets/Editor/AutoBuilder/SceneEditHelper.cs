using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneEditHelper : IDisposable
{
    private Scene m_Scene;

    public SceneEditHelper(string sceneName)
    {
        m_Scene = EditorSceneManager.OpenScene(sceneName, OpenSceneMode.Single);
    }

    public void Dispose()
    {
        EditorSceneManager.MarkSceneDirty(m_Scene);
        EditorSceneManager.SaveScene(m_Scene);
    }

    /// <summary>
    /// Gets the first component of the given type in the scene,
    /// or null if no component of the given type exists.
    /// </summary>
    public T GetFirstComponentOfType<T>()
        where T : MonoBehaviour 
    {
        var roots = m_Scene.GetRootGameObjects();
        var firstOrDefault = roots.FirstOrDefault(root => root.GetComponentInChildren<T>() != null);

        return firstOrDefault != null 
            ? firstOrDefault.GetComponentInChildren<T>() 
            : null;
    }

    /// <summary>
    /// Gets all component of the given type from the scene
    /// </summary>
    public IEnumerable<T> GetComponentsOfType<T>()
        where T : MonoBehaviour
    {
        var roots = m_Scene.GetRootGameObjects();
        var components = new HashSet<T>();

        foreach (var comp in roots.SelectMany(root => root.GetComponentsInChildren<T>()))
        {
            components.Add(comp);
        }

        return components;
    }

    /// <summary>
    /// Gets all GameObjects in the scene that fulfill the given predicate
    /// </summary>
    public IEnumerable<GameObject> GetGameObjectsWhere(Func<GameObject, bool> pred)
    {
        var roots = m_Scene.GetRootGameObjects();
        var objects = new List<GameObject>();

        foreach (var go in roots)
        {
            if (pred(go))
                objects.Add(go);

            objects.AddRange(go.GetChildrenWhere(pred));
        }

        return objects;
    }
}

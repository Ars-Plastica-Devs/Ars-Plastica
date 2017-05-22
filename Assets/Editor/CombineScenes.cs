using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;

public class CombineScenes
{
    [MenuItem("File/Combine Scenes")]
    static void Combine()
    {
        var objects = Selection.objects;

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        foreach (var item in objects)
        {
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(item), OpenSceneMode.Additive);
        }
    }


    [MenuItem("File/Combine Scenes", true)]
    static bool CanCombine()
    {
        if (Selection.objects.Length < 2)
        {
            return false;
        }

        foreach (var item in Selection.objects)
        {
            if (!Path.GetExtension(AssetDatabase.GetAssetPath(item)).ToLower().Equals(".unity"))
            {
                return false;
            }
        }

        return true;
    }
}
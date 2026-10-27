using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AutoSetup
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    static AutoSetup()
    {
        EditorApplication.delayCall += CheckScene;
    }

    static void CheckScene()
    {
        if (Application.isPlaying) return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (BuildArena.SceneHasCurrentLook()) return;

        BuildArena.BuildScene(true);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }
}

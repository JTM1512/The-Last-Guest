using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PropSettleSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";

    [MenuItem("The Last Guest/Settle Props")]
    public static void Add()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        var root = GameObject.Find("Blackwood Study");
        if (root == null)
        {
            Debug.LogError("SETTLE PROPS missing Blackwood Study");
            return;
        }

        if (root.GetComponent<KeepPropsStill>() == null)
            root.AddComponent<KeepPropsStill>();

        var frozen = 0;
        foreach (var body in Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            KeepPropsStill.Freeze(body);
            frozen++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("SETTLE PROPS OK frozen=" + frozen);
    }
}

[InitializeOnLoad]
static class PropSettleAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SettleProps.request"));

    static PropSettleAutoSetup()
    {
        if (File.Exists(Flag))
            EditorApplication.delayCall += RunWhenReady;
    }

    static void RunWhenReady()
    {
        if (!File.Exists(Flag))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunWhenReady;
            return;
        }

        File.Delete(Flag);
        PropSettleSetup.Add();
    }
}

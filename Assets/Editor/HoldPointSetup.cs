using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HoldPointSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";

    [MenuItem("The Last Guest/Add Hold Point")]
    public static void Add()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        var detective = GameObject.Find("Detective");
        if (detective == null)
        {
            Debug.LogError("HOLD POINT missing Detective");
            return;
        }

        var view = detective.GetComponentInChildren<Camera>().transform;
        var hold = view.Find("Hold Point");
        if (hold == null)
        {
            var point = new GameObject("Hold Point");
            point.transform.SetParent(view, false);
            point.transform.localPosition = new Vector3(0f, -0.05f, 0.55f);
            point.transform.localRotation = Quaternion.identity;
            hold = point.transform;
        }

        var itemHold = detective.GetComponent<ItemHold>();
        if (itemHold == null)
            itemHold = detective.AddComponent<ItemHold>();
        itemHold.holdPoint = hold;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("HOLD POINT OK");
    }
}

[InitializeOnLoad]
static class HoldPointAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "HoldPoint.request"));

    static HoldPointAutoSetup()
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
        HoldPointSetup.Add();
    }
}

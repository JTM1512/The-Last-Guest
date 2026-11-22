using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ExamineUiSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";

    [MenuItem("The Last Guest/Add Examine Text")]
    public static void Add()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        var hint = GameObject.Find("Hint");
        if (hint == null)
        {
            Debug.LogError("EXAMINE UI missing Hint canvas");
            return;
        }

        var text = Object.FindAnyObjectByType<Text>();
        var existing = GameObject.Find("Examine Text");
        Text readout;
        if (existing != null)
        {
            readout = existing.GetComponent<Text>();
        }
        else
        {
            var textObject = new GameObject("Examine Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(hint.transform, false);
            readout = textObject.GetComponent<Text>();
            readout.font = text != null ? text.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            readout.fontSize = 20;
            readout.alignment = TextAnchor.MiddleCenter;
            readout.color = new Color(0.94f, 0.88f, 0.75f, 1f);
            readout.horizontalOverflow = HorizontalWrapMode.Wrap;
            readout.verticalOverflow = VerticalWrapMode.Overflow;
            readout.raycastTarget = false;
            readout.text = "";

            var rect = readout.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 36f);
            rect.sizeDelta = new Vector2(900f, 120f);
        }

        var detective = GameObject.Find("Detective");
        if (detective == null)
        {
            Debug.LogError("EXAMINE UI missing Detective");
            return;
        }

        var examine = detective.GetComponent<Examine>();
        if (examine == null)
            examine = detective.AddComponent<Examine>();
        examine.readout = readout;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("EXAMINE UI OK");
    }
}

[InitializeOnLoad]
static class ExamineUiAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ExamineUi.request"));

    static ExamineUiAutoSetup()
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
        ExamineUiSetup.Add();
    }
}

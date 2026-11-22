using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ControlsUiPolish
{
    const string StudyScene = "Assets/Scenes/Study.unity";

    [MenuItem("The Last Guest/Polish Controls UI")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        LowerPauseButtons();
        BuildControlsPanel();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("CONTROLS UI OK");
    }

    static void LowerPauseButtons()
    {
        GameObject pause = null;
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == "Pause Menu")
            {
                pause = transform.gameObject;
                break;
            }
        }

        if (pause == null)
            return;

        MoveNamed(pause.transform, "Resume Game", new Vector2(0f, -150f));
        MoveNamed(pause.transform, "Restart", new Vector2(0f, -214f));
        MoveNamed(pause.transform, "Quit Game", new Vector2(0f, -278f));
    }

    static void MoveNamed(Transform root, string objectName, Vector2 position)
    {
        foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.gameObject.name != objectName)
                continue;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            EditorUtility.SetDirty(rect);
        }
    }

    static void BuildControlsPanel()
    {
        var hint = GameObject.Find("Hint");
        var detective = GameObject.Find("Detective");
        if (hint == null || detective == null)
            return;

        var sample = hint.GetComponentInChildren<Text>(true);
        var font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var hintText = GameObject.Find("Hint Text");
        if (hintText != null)
        {
            var text = hintText.GetComponent<Text>();
            var rect = hintText.GetComponent<RectTransform>();
            if (text != null)
            {
                text.fontSize = 16;
                text.alignment = TextAnchor.UpperLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.text =
                    "CONTROLS\n" +
                    "WASD  move\n" +
                    "Hold RMB  look\n" +
                    "LMB  pick up clue\n" +
                    "E  talk / analyse\n" +
                    "J  clue journal\n" +
                    "Enter  accuse\n" +
                    "P  pause";
            }
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(18f, -16f);
                rect.sizeDelta = new Vector2(260f, 190f);
            }
        }

        Text holdPrompt;
        var existing = GameObject.Find("Hold Prompt");
        if (existing != null)
        {
            holdPrompt = existing.GetComponent<Text>();
        }
        else
        {
            var promptObject = new GameObject("Hold Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            promptObject.transform.SetParent(hint.transform, false);
            holdPrompt = promptObject.GetComponent<Text>();
            holdPrompt.font = font;
            holdPrompt.fontSize = 18;
            holdPrompt.alignment = TextAnchor.MiddleCenter;
            holdPrompt.color = new Color(0.94f, 0.88f, 0.75f, 0.95f);
            holdPrompt.raycastTarget = false;
            holdPrompt.text = "";
            var rect = holdPrompt.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 150f);
            rect.sizeDelta = new Vector2(720f, 36f);
        }

        var examine = detective.GetComponent<Examine>();
        if (examine != null)
        {
            examine.holdPrompt = holdPrompt;
            EditorUtility.SetDirty(examine);
        }
    }
}

[InitializeOnLoad]
static class ControlsUiPolishAuto
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ControlsUi.request"));

    static ControlsUiPolishAuto()
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
        ControlsUiPolish.Run();
    }
}

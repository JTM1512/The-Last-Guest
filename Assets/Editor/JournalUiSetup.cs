using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class JournalUiSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";

    [MenuItem("The Last Guest/Add Clue Journal")]
    public static void Add()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        var hint = GameObject.Find("Hint");
        var detective = GameObject.Find("Detective");
        if (hint == null || detective == null)
        {
            Debug.LogError("JOURNAL UI missing Hint or Detective");
            return;
        }

        var sample = hint.GetComponentInChildren<Text>(true);
        var font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var panelTransform = FindChild(hint.transform, "Journal Panel");
        GameObject panel;
        Text list;
        if (panelTransform == null)
        {
            panel = new GameObject("Journal Panel", typeof(RectTransform));
            panel.transform.SetParent(hint.transform, false);
            Stretch(panel.GetComponent<RectTransform>());

            var card = new GameObject("Journal Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(panel.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 460f);
            var image = card.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.06f, 0.05f, 0.94f);

            var title = CreateText(card.transform, "Journal Title", font, 28, TextAnchor.UpperCenter, new Vector2(0f, -28f), new Vector2(480f, 40f));
            title.text = "Clues";

            list = CreateText(card.transform, "Journal List", font, 22, TextAnchor.UpperLeft, new Vector2(0f, -84f), new Vector2(480f, 300f));
            list.horizontalOverflow = HorizontalWrapMode.Wrap;
            list.verticalOverflow = VerticalWrapMode.Overflow;

            var close = CreateText(card.transform, "Journal Close", font, 16, TextAnchor.LowerCenter, new Vector2(0f, 24f), new Vector2(480f, 28f));
            close.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            close.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            close.rectTransform.pivot = new Vector2(0.5f, 0f);
            close.text = "J closes";
            close.color = new Color(0.94f, 0.88f, 0.75f, 0.7f);
        }
        else
        {
            panel = panelTransform.gameObject;
            list = null;
            foreach (var text in panel.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name == "Journal List")
                    list = text;
            }
        }

        panel.SetActive(false);

        var journal = detective.GetComponent<Journal>();
        if (journal == null)
            journal = detective.AddComponent<Journal>();
        journal.examine = detective.GetComponent<Examine>();
        journal.panel = panel;
        journal.list = list;

        var hintText = GameObject.Find("Hint Text");
        if (hintText != null)
        {
            var text = hintText.GetComponent<Text>();
            if (text != null)
                text.text = "Blackwood Manor  —  The Study\nWASD move     Hold right mouse to look     Left click a clue     J clues";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("JOURNAL UI OK");
    }

    static Transform FindChild(Transform parent, string childName)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    static Text CreateText(Transform parent, string objectName, Font font, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.88f, 0.75f, 1f);
        text.raycastTarget = false;
        text.text = "";
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        return text;
    }
}

[InitializeOnLoad]
static class JournalUiAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "JournalUi.request"));

    static JournalUiAutoSetup()
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
        JournalUiSetup.Add();
    }
}

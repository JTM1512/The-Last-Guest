using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class AccusationUiSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";

    [MenuItem("The Last Guest/Add Accusation")]
    public static void Add()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        var hint = GameObject.Find("Hint");
        var detective = GameObject.Find("Detective");
        if (hint == null || detective == null)
        {
            Debug.LogError("ACCUSATION UI missing Hint or Detective");
            return;
        }

        if (hint.GetComponent<GraphicRaycaster>() == null)
            hint.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(null);
        }

        var sample = hint.GetComponentInChildren<Text>(true);
        var font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        var existing = FindChild(hint.transform, "Accusation Panel");
        GameObject panel;
        Transform who;
        Transform why;
        Transform evidence;
        Text verdict;
        Button accuse;
        if (existing == null)
        {
            panel = new GameObject("Accusation Panel", typeof(RectTransform));
            panel.transform.SetParent(hint.transform, false);
            Stretch(panel.GetComponent<RectTransform>());

            var card = new GameObject("Accusation Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(panel.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(980f, 640f);
            var image = card.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.06f, 0.05f, 0.94f);

            var title = CreateText(card.transform, "Accusation Title", font, 28, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(900f, 40f));
            title.text = "Make your accusation";

            CreateText(card.transform, "Who Label", font, 18, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -72f), new Vector2(300f, 28f)).text = "Who";
            CreateText(card.transform, "Why Label", font, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(300f, 28f)).text = "Why";
            CreateText(card.transform, "Evidence Label", font, 18, TextAnchor.MiddleCenter, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -72f), new Vector2(300f, 28f)).text = "Evidence";

            who = Column(card.transform, "Who Choices", new Vector2(0f, 1f), new Vector2(40f, -108f), new Vector2(0f, 1f));
            why = Column(card.transform, "Why Choices", new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(0.5f, 1f));
            evidence = Column(card.transform, "Evidence Choices", new Vector2(1f, 1f), new Vector2(-40f, -108f), new Vector2(1f, 1f));

            verdict = CreateText(card.transform, "Accusation Verdict", font, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(860f, 56f));
            verdict.rectTransform.pivot = new Vector2(0.5f, 0f);

            var buttonObject = new GameObject("Accuse Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(card.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 22f);
            buttonRect.sizeDelta = new Vector2(180f, 42f);
            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.sprite = sprite;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = new Color(0.45f, 0.28f, 0.14f, 1f);
            accuse = buttonObject.GetComponent<Button>();
            var buttonLabel = CreateText(buttonObject.transform, "Label", font, 18, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(buttonLabel.rectTransform);
            buttonLabel.text = "Accuse";
        }
        else
        {
            panel = existing.gameObject;
            who = FindChild(existing, "Who Choices");
            if (who == null)
                who = FindNamed(existing, "Who Choices");
            why = FindNamed(existing, "Why Choices");
            evidence = FindNamed(existing, "Evidence Choices");
            verdict = null;
            accuse = null;
            foreach (var text in panel.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name == "Accusation Verdict")
                    verdict = text;
            }
            foreach (var button in panel.GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name == "Accuse Button")
                    accuse = button;
            }
        }

        panel.SetActive(false);

        Suspect killer = null;
        foreach (var suspect in Object.FindObjectsByType<Suspect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (suspect.suspectName == "Dr. Reed")
                killer = suspect;
        }

        Clue proof = null;
        foreach (var clue in Object.FindObjectsByType<Clue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (clue.clueName == "Torn Medical Report")
                proof = clue;
        }

        var accusation = detective.GetComponent<Accusation>();
        if (accusation == null)
            accusation = detective.AddComponent<Accusation>();
        accusation.examine = detective.GetComponent<Examine>();
        accusation.killer = killer;
        accusation.proof = proof;
        accusation.panel = panel;
        accusation.whoChoices = who;
        accusation.whyChoices = why;
        accusation.evidenceChoices = evidence;
        accusation.verdict = verdict;
        accusation.accuseButton = accuse;

        var hintText = GameObject.Find("Hint Text");
        if (hintText != null)
        {
            var text = hintText.GetComponent<Text>();
            if (text != null)
                text.text = "Blackwood Manor  —  The Study\nWASD move     Hold right mouse to look     Left click a clue     J clues     Enter accuses";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ACCUSATION UI OK killer=" + (killer != null) + " proof=" + (proof != null));
    }

    static Transform Column(Transform parent, string objectName, Vector2 anchor, Vector2 position, Vector2 pivot)
    {
        var column = new GameObject(objectName, typeof(RectTransform), typeof(VerticalLayoutGroup));
        column.transform.SetParent(parent, false);
        var rect = column.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300f, 420f);
        var layout = column.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 4, 4);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return column.transform;
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

    static Transform FindNamed(Transform parent, string childName)
    {
        var direct = FindChild(parent, childName);
        if (direct != null)
            return direct;
        foreach (var transform in parent.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == childName)
                return transform;
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

    static Text CreateText(Transform parent, string objectName, Font font, int size, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.88f, 0.75f, 1f);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        var rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        return text;
    }
}

[InitializeOnLoad]
static class AccusationUiAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AccusationUi.request"));

    static AccusationUiAutoSetup()
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
        AccusationUiSetup.Add();
    }
}

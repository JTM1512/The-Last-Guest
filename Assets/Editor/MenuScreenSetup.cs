using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MenuScreenSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";
    const string Folder = "Assets/Art/Screens/";

    [MenuItem("The Last Guest/Add Menu Screens")]
    public static void Add()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        var mainArt = LoadSprite(Folder + "MainMenu.png");
        var pauseArt = LoadSprite(Folder + "Pause.png");
        var solvedArt = LoadSprite(Folder + "EndingSolved.png");
        var partialArt = LoadSprite(Folder + "EndingPartial.png");
        var escapedArt = LoadSprite(Folder + "EndingEscaped.png");
        if (mainArt == null || pauseArt == null || solvedArt == null || partialArt == null || escapedArt == null)
        {
            Debug.LogError("MENU SCREENS missing artwork");
            return;
        }

        var hint = GameObject.Find("Hint");
        var sample = hint != null ? hint.GetComponentInChildren<Text>(true) : null;
        var font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        var existing = GameObject.Find("Menus");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var root = new GameObject("Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var screens = root.AddComponent<GameScreens>();
        screens.hint = hint;

        screens.mainMenu = Screen(root.transform, "Main Menu", mainArt);
        screens.pauseMenu = Screen(root.transform, "Pause Menu", pauseArt);
        screens.solvedScreen = Screen(root.transform, "Solved", solvedArt);
        screens.partialScreen = Screen(root.transform, "Partial", partialArt);
        screens.escapedScreen = Screen(root.transform, "Escaped", escapedArt);

        var play = Button(screens.mainMenu.transform, "Play Game", font, buttonSprite, new Vector2(0f, 118f));
        var quitMenu = Button(screens.mainMenu.transform, "Quit Game", font, buttonSprite, new Vector2(0f, 52f));
        UnityEventTools.AddPersistentListener(play.onClick, screens.PlayGame);
        UnityEventTools.AddPersistentListener(quitMenu.onClick, screens.QuitGame);

        var resume = CenterButton(screens.pauseMenu.transform, "Resume Game", font, buttonSprite, new Vector2(0f, 36f));
        var restart = CenterButton(screens.pauseMenu.transform, "Restart", font, buttonSprite, new Vector2(0f, -28f));
        var quitPause = CenterButton(screens.pauseMenu.transform, "Quit Game", font, buttonSprite, new Vector2(0f, -92f));
        UnityEventTools.AddPersistentListener(resume.onClick, screens.Resume);
        UnityEventTools.AddPersistentListener(restart.onClick, screens.Restart);
        UnityEventTools.AddPersistentListener(quitPause.onClick, screens.QuitGame);

        AddEndingButtons(screens.solvedScreen.transform, screens, font, buttonSprite);
        AddEndingButtons(screens.partialScreen.transform, screens, font, buttonSprite);
        AddEndingButtons(screens.escapedScreen.transform, screens, font, buttonSprite);

        screens.mainMenu.SetActive(true);
        screens.pauseMenu.SetActive(false);
        screens.solvedScreen.SetActive(false);
        screens.partialScreen.SetActive(false);
        screens.escapedScreen.SetActive(false);

        if (hint != null)
        {
            var hintText = hint.GetComponentInChildren<Text>(true);
            if (hintText != null && hintText.gameObject.name == "Hint Text")
                hintText.text = "Blackwood Manor  —  The Study\nWASD move     Hold right mouse to look     Left click a clue     J clues     Enter accuses     P pause";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MENU SCREENS OK");
    }

    static void AddEndingButtons(Transform parent, GameScreens screens, Font font, Sprite buttonSprite)
    {
        var menu = Button(parent, "Main Menu", font, buttonSprite, new Vector2(-150f, 110f));
        var quit = Button(parent, "Quit Game", font, buttonSprite, new Vector2(150f, 110f));
        UnityEventTools.AddPersistentListener(menu.onClick, screens.ReturnToMenu);
        UnityEventTools.AddPersistentListener(quit.onClick, screens.QuitGame);
    }

    static GameObject Screen(Transform parent, string objectName, Sprite art)
    {
        var screen = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        screen.transform.SetParent(parent, false);
        var rect = screen.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = screen.GetComponent<Image>();
        image.sprite = art;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
        return screen;
    }

    static Button Button(Transform parent, string label, Font font, Sprite sprite, Vector2 position)
    {
        var item = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        item.transform.SetParent(parent, false);
        var rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(280f, 52f);
        var image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.1f, 0.08f, 0.06f, 0.88f);
        var textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(item.transform, false);
        var text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.94f, 0.86f, 0.68f, 1f);
        text.text = label;
        text.raycastTarget = false;
        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return item.GetComponent<Button>();
    }

    static Button CenterButton(Transform parent, string label, Font font, Sprite sprite, Vector2 position)
    {
        var button = Button(parent, label, font, sprite, position);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        return button;
    }

    static Sprite LoadSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }
}

[InitializeOnLoad]
static class MenuScreenAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "MenuScreens.request"));

    static MenuScreenAutoSetup()
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
        MenuScreenSetup.Add();
    }
}

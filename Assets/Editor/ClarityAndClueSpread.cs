using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClarityAndClueSpread
{
    const string StudyScene = "Assets/Scenes/Study.unity";
    const string Folder = "Assets/Art/Screens/";

    [MenuItem("The Last Guest/Clarify Screens And Spread Clues")]
    public static void Run()
    {
        ClarifyScreens();
        SpreadClues();
        Debug.Log("CLARITY AND CLUES OK");
    }

    static void ClarifyScreens()
    {
        var names = new[]
        {
            "MainMenu.png",
            "Pause.png",
            "EndingSolved.png",
            "EndingPartial.png",
            "EndingEscaped.png"
        };

        foreach (var name in names)
        {
            var assetPath = Folder + name;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 0;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        foreach (var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image.sprite == null)
                continue;
            var path = AssetDatabase.GetAssetPath(image.sprite);
            if (string.IsNullOrEmpty(path) || !path.StartsWith(Folder))
                continue;
            image.preserveAspect = true;
            EditorUtility.SetDirty(image);
        }
    }

    static void SpreadClues()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        var watch = FindClue("Broken Pocket Watch");
        var report = FindClue("Torn Medical Report");
        var bottle = FindClue("Medication Bottle");
        var note = FindClue("Doctor's Appointment Note");
        var prints = FindClue("Muddy Footprints");

        var desk = GameObject.Find("photo_frame_desk");
        var tea = GameObject.Find("tea_table 1");
        var reading = GameObject.Find("Table01");
        var cupboard = GameObject.Find("WoodCupboard01");
        GameObject shelf = null;
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name == "bookshelf")
            {
                shelf = go.gameObject;
                break;
            }
        }

        if (watch != null && desk != null)
            SeatOn(watch, TopOf(desk) + new Vector3(-0.15f, 0f, 0.05f), 20f);

        if (report != null && reading != null)
            SeatOn(report, TopOf(reading) + new Vector3(0.05f, 0f, 0.1f), 40f);

        if (bottle != null && tea != null)
            SeatOn(bottle, TopOf(tea) + new Vector3(0.1f, 0f, -0.05f), -20f);

        if (note != null && shelf != null)
        {
            var bounds = BoundsOf(shelf);
            SeatOn(note, new Vector3(bounds.center.x, bounds.center.y + 0.15f, bounds.min.z + 0.2f), 180f);
        }
        else if (note != null && cupboard != null)
            SeatOn(note, TopOf(cupboard) + new Vector3(0f, 0.05f, 0f), 90f);

        if (prints != null)
        {
            prints.transform.position = new Vector3(10.05f, 0.02f, 1.4f);
            prints.transform.rotation = Quaternion.identity;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static Clue FindClue(string clueName)
    {
        foreach (var clue in Object.FindObjectsByType<Clue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (clue.clueName == clueName)
                return clue;
        }

        return null;
    }

    static void SeatOn(Clue clue, Vector3 surfacePoint, float yaw)
    {
        clue.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var hasBounds = false;
        var bounds = new Bounds(clue.transform.position, Vector3.zero);
        foreach (var renderer in clue.GetComponentsInChildren<Renderer>())
        {
            if (renderer.gameObject.name.Contains("Label") || renderer.gameObject.name.Contains("Glow"))
                continue;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            clue.transform.position = surfacePoint;
            return;
        }

        var delta = surfacePoint - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        clue.transform.position += delta;
        EditorUtility.SetDirty(clue);
    }

    static Vector3 TopOf(GameObject go)
    {
        var bounds = BoundsOf(go);
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one * 0.2f);
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}

[InitializeOnLoad]
static class ClarityAndClueSpreadAuto
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ClarityClues.request"));

    static ClarityAndClueSpreadAuto()
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
        ClarityAndClueSpread.Run();
    }
}

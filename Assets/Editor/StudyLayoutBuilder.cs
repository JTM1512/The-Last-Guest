using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StudyLayoutBuilder
{
    const string Pack = "Assets/Scenes/ZNS3D/Prefabs/";
    const string StudyScene = "Assets/Scenes/Study.unity";
    const string MaterialFolder = "Assets/Materials/Study";

    const string BottlePath = "Assets/BK_AlchemistHouse/Prefabs/Items/Bottles/Bottle01.prefab";
    const string ReportPath = "Assets/BK_AlchemistHouse/Prefabs/Items/Books/Book06.prefab";
    const string NotePath = "Assets/BK_AlchemistHouse/Prefabs/Items/Books/Book14.prefab";
    const string LampPath = "Assets/BK_AlchemistHouse/Prefabs/Items/Lights/Oillamp01_on.prefab";
    const string GlobePath = "Assets/BK_AlchemistHouse/Prefabs/Items/Misc/Globe.prefab";
    const string StovePath = "Assets/BK_AlchemistHouse/Prefabs/Furniture/StoveLit.prefab";

    struct ModuleInfo
    {
        public GameObject prefab;
        public bool longOnX;
        public float sizeX;
        public float sizeY;
        public float sizeZ;
        public float length;
        public float thickness;
    }

    [MenuItem("The Last Guest/Build Study Layout")]
    public static void Build()
    {
        try
        {
            BuildStudy();
            Debug.Log("STUDY LAYOUT OK");
        }
        catch (Exception exception)
        {
            Debug.LogError("STUDY LAYOUT FAILED\n" + exception);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    static void BuildStudy()
    {
        var openStudy = SceneManager.GetSceneByPath(StudyScene);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        if (openStudy.isLoaded)
            EditorSceneManager.CloseScene(openStudy, true);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Study");

        var floorModule = Measure(Pack + "floor_big.prefab");
        var ceilingModule = Measure(Pack + "ceiling_big.prefab");
        var wallModule = Measure(Pack + "wall_default 1.prefab");
        var windowModule = Measure(Pack + "wall_with_two_window.prefab");
        var doorModule = Measure(Pack + "wall_with_big_door.prefab");

        int tilesX = Mathf.Clamp(Mathf.RoundToInt(20f / floorModule.sizeX), 2, 12);
        int tilesZ = Mathf.Clamp(Mathf.RoundToInt(16f / floorModule.sizeZ), 2, 12);
        float width = tilesX * floorModule.sizeX;
        float depth = tilesZ * floorModule.sizeZ;
        float midX = width * 0.5f;
        float midZ = depth * 0.5f;
        const float aisle = 1.8f;

        var study = new GameObject("Blackwood Study");
        var architecture = new GameObject("Architecture").transform;
        architecture.SetParent(study.transform, false);
        var spawned = new List<GameObject>();

        for (var x = 0; x < tilesX; x++)
        {
            for (var z = 0; z < tilesZ; z++)
                spawned.Add(PlaceFloor(floorModule, architecture, new Vector3(x * floorModule.sizeX, 0f, z * floorModule.sizeZ)));
        }

        spawned.AddRange(TileWall(wallModule, windowModule, doorModule, architecture, Vector3.right, 0f, Vector3.forward, width, false, true));
        spawned.AddRange(TileWall(wallModule, windowModule, doorModule, architecture, Vector3.right, depth, Vector3.back, width, false, false));
        spawned.AddRange(TileWall(wallModule, windowModule, doorModule, architecture, Vector3.forward, 0f, Vector3.right, depth, true, false));
        spawned.AddRange(TileWall(wallModule, windowModule, doorModule, architecture, Vector3.forward, width, Vector3.left, depth, true, false));

        int ceilingX = Mathf.Max(1, Mathf.CeilToInt(width / ceilingModule.sizeX));
        int ceilingZ = Mathf.Max(1, Mathf.CeilToInt(depth / ceilingModule.sizeZ));
        for (var x = 0; x < ceilingX; x++)
        {
            for (var z = 0; z < ceilingZ; z++)
                spawned.Add(PlaceCeiling(ceilingModule, architecture, new Vector3(x * ceilingModule.sizeX, wallModule.sizeY - 0.02f, z * ceilingModule.sizeZ)));
        }

        AddOuterShell(study.transform, width, depth, Mathf.Max(3f, wallModule.sizeY));
        var floorCenter = new Vector3(midX, 0f, midZ);

        var clues = new GameObject("Clues").transform;
        clues.SetParent(study.transform, false);
        var people = new GameObject("People").transform;
        people.SetParent(study.transform, false);

        var desk = PlaceProp(Pack + "photo_frame_desk.prefab", architecture, new Vector3(midX + aisle + 2.6f, 0f, midZ - aisle - 2.4f), Vector3.left, 1.3f, 2.2f);
        var shelves = new List<GameObject>();
        for (var x = 2.4f; x < midX - aisle - 1.1f; x += 2.7f)
            shelves.Add(PlaceProp(Pack + "bookshelf.prefab", architecture, new Vector3(x, 0f, depth - 1.25f), Vector3.back, 2.5f, 1.7f));
        for (var x = midX + aisle + 1.8f; x < width - 2.2f; x += 2.7f)
            shelves.Add(PlaceProp(Pack + "bookshelf.prefab", architecture, new Vector3(x, 0f, depth - 1.25f), Vector3.back, 2.5f, 1.7f));

        PlaceProp(Pack + "sofa_big.prefab", architecture, new Vector3(2.3f, 0f, 2.4f), Vector3.right, 1.3f, 2.6f);
        PlaceProp(Pack + "sofa_small.prefab", architecture, new Vector3(2.3f, 0f, 4.8f), Vector3.right, 1.3f, 2.2f);
        var teaTable = PlaceProp(Pack + "tea_table 1.prefab", architecture, new Vector3(4.2f, 0f, 3.5f), Vector3.right, 0.8f, 1.4f);
        StripColliders(PlaceProp(Pack + "carpet_1.prefab", architecture, new Vector3(3.4f, 0f, 3.6f), Vector3.forward, 0.08f, 3.4f));

        var hearth = PlaceProp(StovePath, architecture, new Vector3(width - 1.6f, 0f, midZ + aisle + 2.3f), Vector3.left, 1.45f, 1.7f);
        hearth.name = "Hearth";
        var clock = PlaceProp(Pack + "clock_wall.prefab", architecture, new Vector3(width - 0.35f, 0f, midZ + aisle + 2.3f), Vector3.left, 0.7f, 0.7f);
        var clockBounds = BoundsOf(clock);
        clock.transform.position += new Vector3(0f, 1.85f - clockBounds.center.y, 0f);
        StripColliders(clock);
        Hang(Pack + "painting_1.prefab", architecture, new Vector3(width - 0.2f, 0f, midZ + aisle + 4.2f), Vector3.left);
        Hang(Pack + "painting_4.prefab", architecture, new Vector3(midX, 0f, depth - 0.15f), Vector3.back);

        var readingTable = PlaceProp("Assets/BK_AlchemistHouse/Prefabs/Furniture/Table01.prefab", architecture, new Vector3(width - 3.1f, 0f, depth - 2.3f), Vector3.left, 0.9f, 1.5f);
        var readingTop = TopOf(readingTable);
        PlaceProp("Assets/BK_AlchemistHouse/Prefabs/Items/Books/Book02.prefab", clues, readingTop + new Vector3(-0.18f, 0f, 0f), Vector3.forward, 0.16f, 0.4f);
        PlaceProp("Assets/BK_AlchemistHouse/Prefabs/Items/Books/Book08.prefab", clues, readingTop + new Vector3(0.16f, 0f, 0.05f), Vector3.right, 0.16f, 0.4f);
        PlaceProp(GlobePath, clues, readingTop + new Vector3(0f, 0f, -0.2f), Vector3.forward, 0.35f, 0.4f);

        PlaceProp("Assets/BK_AlchemistHouse/Prefabs/Furniture/WoodCupboard01.prefab", architecture, new Vector3(1.45f, 0f, midZ + aisle + 2.4f), Vector3.right, 2.1f, 1.4f);
        PlaceProp("Assets/BK_AlchemistHouse/Prefabs/Furniture/Chair01.prefab", architecture, new Vector3(midX + aisle + 1.1f, 0f, midZ - aisle - 2.4f), Vector3.right, 1.1f, 0.7f);
        PlaceProp(Pack + "gramophone.prefab", architecture, new Vector3(midX - aisle - 1.8f, 0f, midZ + aisle + 1.7f), Vector3.forward, 1.2f, 0.9f);
        StripColliders(PlaceProp(Pack + "vase_1.prefab", architecture, new Vector3(width - 1.4f, 0f, 1.6f), Vector3.back, 0.7f, 0.5f));
        StripColliders(PlaceProp(Pack + "carpet_2.prefab", architecture, new Vector3(midX + aisle + 2.6f, 0f, midZ - aisle - 2.4f), Vector3.forward, 0.08f, 3.2f));

        var deskBounds = BoundsOf(desk);
        var deskTop = new Vector3(deskBounds.center.x, deskBounds.max.y, deskBounds.center.z);
        var along = desk.transform.right;
        along.y = 0f;
        if (along.sqrMagnitude < 0.01f)
            along = Vector3.right;
        along.Normalize();

        var watch = CreatePocketWatch(clues);
        SeatExisting(watch, deskTop + along * 0.05f, 20f, 0.12f);
        AddClue(watch, "Broken Pocket Watch", "The hands stopped at 8:30. Whatever happened in this room, it happened then.", font);

        SeatPrefab(LampPath, clues, deskTop - along * 0.55f, 10f, 0.42f);

        var report = SeatPrefab(ReportPath, clues, readingTop + new Vector3(0.05f, 0f, 0.12f), 40f, 0.16f);
        AddClue(report, "Torn Medical Report", "Victor tore into a report about his own health records. Someone had been changing them.", font);

        var bottle = SeatPrefab(BottlePath, clues, TopOf(teaTable) + new Vector3(0.1f, 0f, -0.05f), -20f, 0.22f);
        AddClue(bottle, "Medication Bottle", "Victor's medicine. The seal is broken, and what remains does not match the dose on the label.", font);

        if (shelves.Count > 0)
        {
            var shelfBounds = BoundsOf(shelves[0]);
            var notePoint = new Vector3(shelfBounds.center.x, shelfBounds.center.y + 0.15f, shelfBounds.min.z + 0.2f);
            var note = SeatPrefab(NotePath, clues, notePoint, 180f, 0.2f);
            AddClue(note, "Doctor's Appointment Note", "A note in Victor's hand. He meant to confront Dr. Reed here at 8:30.", font);
        }

        var trailStart = new Vector3(midX, 0.02f, 0.7f);
        var prints = new GameObject("Muddy Footprints");
        prints.transform.SetParent(clues, false);
        prints.transform.position = trailStart + Vector3.forward * 1.2f;
        var mud = MakeMaterial("Mud", new Color(0.18f, 0.13f, 0.08f));
        for (var i = 0; i < 6; i++)
        {
            var print = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            print.name = "Print";
            print.transform.SetParent(prints.transform, true);
            print.transform.position = trailStart + Vector3.forward * (i * 0.55f) + Vector3.right * ((i % 2 == 0) ? 0.16f : -0.16f);
            print.transform.rotation = Quaternion.Euler(0f, i * 16f, 0f);
            print.transform.localScale = new Vector3(0.22f, 0.008f, 0.4f);
            print.GetComponent<Renderer>().sharedMaterial = mud;
            UnityEngine.Object.DestroyImmediate(print.GetComponent<Collider>());
        }
        AddClue(prints, "Muddy Footprints", "Wet prints come in from the conservatory door and stop inside the study.", font, 0.35f);

        foreach (Transform child in architecture)
        {
            if (!spawned.Contains(child.gameObject))
                spawned.Add(child.gameObject);
        }

        var blocked = spawned.Where(IsLargeFurniture).Select(BoundsOf).ToList();
        PlaceSuspect(people, font, "Evelyn Hale",
            "She says she was in the dining room the whole time.",
            "Victor was going to cut her out of the will.",
            new Color(0.45f, 0.16f, 0.22f),
            ClearSpot(new Vector3(midX - aisle - 1.2f, 0f, midZ - 0.4f), floorCenter, blocked),
            blocked);
        PlaceSuspect(people, font, "Arthur",
            "He says he was polishing silver in the kitchen.",
            "Victor told him earlier this evening that he was finished at the manor.",
            new Color(0.22f, 0.22f, 0.24f),
            ClearSpot(new Vector3(midX + 1.4f, 0f, 2.6f), floorCenter, blocked),
            blocked);
        PlaceSuspect(people, font, "Thomas Hale",
            "He says he was in his bedroom.",
            "He is badly in debt, and he needed his father's money.",
            new Color(0.16f, 0.24f, 0.42f),
            ClearSpot(new Vector3(midX, 0f, depth - 3.2f), floorCenter, blocked),
            blocked);
        PlaceSuspect(people, font, "Dr. Reed",
            "He says he was in the conservatory.",
            "He had been falsifying Victor's medical records, and Victor had found out.",
            new Color(0.18f, 0.34f, 0.28f),
            ClearSpot(new Vector3(midX + aisle + 0.6f, 0f, midZ - aisle + 0.4f), floorCenter, blocked),
            blocked);

        var victimSpot = ClearSpot(new Vector3(deskTop.x, 0f, deskTop.z + 1.5f), floorCenter, blocked);
        var victim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        victim.name = "Victor Hale";
        victim.transform.SetParent(people, false);
        victim.transform.localScale = new Vector3(0.38f, 0.85f, 0.38f);
        victim.transform.rotation = Quaternion.Euler(0f, 25f, 90f);
        victim.transform.position = new Vector3(victimSpot.x, 0.2f, victimSpot.z);
        victim.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Victim", new Color(0.12f, 0.1f, 0.1f));
        CreateLabel(victim.transform, "Victor Hale", font, new Color(0.85f, 0.8f, 0.74f), 1.1f);

        AddColliders(study.transform, spawned);
        CreatePlayer(new Vector3(midX, 0.05f, 3.4f), deskTop);
        CreateLighting(study.transform, new Bounds(floorCenter, new Vector3(width, 0.2f, depth)));
        CreateHint(font);
        DressAtmosphere();

        var studyScene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(studyScene, StudyScene);
        var buildScenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(StudyScene, true)
        };
        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing.path != StudyScene)
                buildScenes.Add(new EditorBuildSettingsScene(existing.path, false));
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();

        Debug.Log("STUDY SIZE " + width.ToString("0.0") + " x " + depth.ToString("0.0") + " meters, wall height " + wallModule.sizeY.ToString("0.0"));
    }

    static ModuleInfo Measure(string path)
    {
        var prefab = LoadPrefab(path);
        var sample = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        sample.transform.rotation = Quaternion.identity;
        var bounds = BoundsOf(sample);
        UnityEngine.Object.DestroyImmediate(sample);
        if (bounds.size.x < 0.05f || bounds.size.z < 0.05f)
            throw new InvalidOperationException("Could not measure " + path + " size " + bounds.size);

        return new ModuleInfo
        {
            prefab = prefab,
            longOnX = bounds.size.x >= bounds.size.z,
            sizeX = bounds.size.x,
            sizeY = bounds.size.y,
            sizeZ = bounds.size.z,
            length = Mathf.Max(bounds.size.x, bounds.size.z),
            thickness = Mathf.Min(bounds.size.x, bounds.size.z)
        };
    }

    static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Missing prefab " + path);
        return prefab;
    }

    static GameObject PlaceFloor(ModuleInfo module, Transform parent, Vector3 desiredMin)
    {
        var tile = (GameObject)PrefabUtility.InstantiatePrefab(module.prefab);
        tile.name = "Floor Tile";
        tile.transform.SetParent(parent, false);
        tile.transform.rotation = Quaternion.identity;
        var bounds = BoundsOf(tile);
        tile.transform.position += new Vector3(desiredMin.x, 0f, desiredMin.z) - new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        return tile;
    }

    static GameObject PlaceCeiling(ModuleInfo module, Transform parent, Vector3 desiredMin)
    {
        var tile = (GameObject)PrefabUtility.InstantiatePrefab(module.prefab);
        tile.name = "Ceiling Tile";
        tile.transform.SetParent(parent, false);
        tile.transform.rotation = Quaternion.identity;
        var bounds = BoundsOf(tile);
        tile.transform.position += desiredMin - bounds.min;
        return tile;
    }

    static List<GameObject> TileWall(ModuleInfo solid, ModuleInfo window, ModuleInfo door, Transform parent, Vector3 along, float edge, Vector3 inward, float span, bool windows, bool doorway)
    {
        var placed = new List<GameObject>();
        var cursor = -solid.thickness;
        var end = span + solid.thickness;
        var index = 0;
        var doorUsed = false;
        while (cursor < end - 0.08f && index < 48)
        {
            var placeDoor = doorway && !doorUsed && cursor > span * 0.38f;
            var placeWindow = windows && !placeDoor && index % 2 == 1;
            var module = placeDoor ? door : placeWindow ? window : solid;
            var go = PlaceWall(module, parent, placeDoor ? "Door Wall" : placeWindow ? "Window Wall" : "Wall", along, cursor, edge, inward);
            placed.Add(go);
            var bounds = BoundsOf(go);
            var used = Mathf.Abs(along.x) > 0.5f ? bounds.size.x : bounds.size.z;
            cursor += Mathf.Max(0.45f, used) * 0.99f;
            if (placeDoor)
                doorUsed = true;
            index++;
        }
        return placed;
    }

    static GameObject PlaceWall(ModuleInfo module, Transform parent, string objectName, Vector3 along, float alongMin, float edge, Vector3 inward)
    {
        var wantX = Mathf.Abs(along.x) > Mathf.Abs(along.z);
        var yaw = module.longOnX == wantX ? 0f : 90f;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(module.prefab);
        go.name = objectName;
        go.transform.SetParent(parent, false);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        var bounds = BoundsOf(go);
        var shift = new Vector3(0f, -bounds.min.y, 0f);
        if (inward.z > 0.5f)
            shift.z = edge - bounds.max.z;
        else if (inward.z < -0.5f)
            shift.z = edge - bounds.min.z;
        else if (inward.x > 0.5f)
            shift.x = edge - bounds.max.x;
        else
            shift.x = edge - bounds.min.x;
        go.transform.position += shift;

        bounds = BoundsOf(go);
        if (wantX)
            go.transform.position += new Vector3(alongMin - bounds.min.x, 0f, 0f);
        else
            go.transform.position += new Vector3(0f, 0f, alongMin - bounds.min.z);
        return go;
    }

    static GameObject PlaceProp(string path, Transform parent, Vector3 floorPoint, Vector3 face, float maxHeight, float maxFootprint)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(path));
        go.transform.SetParent(parent, false);
        var forward = go.transform.forward;
        forward.y = 0f;
        face.y = 0f;
        if (forward.sqrMagnitude > 0.001f && face.sqrMagnitude > 0.001f)
            go.transform.Rotate(0f, Vector3.SignedAngle(forward, face.normalized, Vector3.up), 0f, Space.World);

        var bounds = BoundsOf(go);
        var scale = 1f;
        var wide = Mathf.Max(bounds.size.x, bounds.size.z);
        if (maxFootprint > 0f && wide > maxFootprint)
            scale = Mathf.Min(scale, maxFootprint / wide);
        if (maxHeight > 0f && bounds.size.y > maxHeight)
            scale = Mathf.Min(scale, maxHeight / bounds.size.y);
        if (scale < 0.999f)
            go.transform.localScale *= scale;

        bounds = BoundsOf(go);
        go.transform.position += floorPoint - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return go;
    }

    static void Hang(string path, Transform parent, Vector3 floorPoint, Vector3 face)
    {
        var painting = PlaceProp(path, parent, floorPoint, face, 1.2f, 1.5f);
        var bounds = BoundsOf(painting);
        painting.transform.position += new Vector3(0f, 1.75f - bounds.center.y, 0f);
        painting.name = "Painting";
        StripColliders(painting);
    }

    static Vector3 TopOf(GameObject go)
    {
        var bounds = BoundsOf(go);
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    static void StripColliders(GameObject go)
    {
        foreach (var collider in go.GetComponentsInChildren<Collider>())
            UnityEngine.Object.DestroyImmediate(collider);
    }

    static GameObject SeatPrefab(string path, Transform parent, Vector3 surfacePoint, float yaw, float maxHeight)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Missing prefab: " + path);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, true);
        SeatExisting(go, surfacePoint, yaw, maxHeight);
        return go;
    }

    static void SeatExisting(GameObject go, Vector3 surfacePoint, float yaw, float maxHeight)
    {
        go.transform.Rotate(0f, yaw, 0f, Space.World);
        if (maxHeight > 0f)
        {
            var measured = BoundsOf(go);
            if (measured.size.y > maxHeight)
                go.transform.localScale *= maxHeight / measured.size.y;
        }

        var bounds = BoundsOf(go);
        var delta = surfacePoint - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        go.transform.position += delta;
    }

    static void AddClue(GameObject go, string clueName, string description, Font font, float labelHeight = 0.45f)
    {
        go.name = clueName;
        var clue = go.GetComponent<Clue>() ?? go.AddComponent<Clue>();
        clue.clueName = clueName;
        clue.description = description;
        CreateLabel(go.transform, clueName, font, new Color(0.95f, 0.84f, 0.55f), labelHeight);

        var glow = new GameObject("Clue Glow");
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        var light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.78f, 0.45f);
        light.intensity = 0.7f;
        light.range = 1.6f;
        light.shadows = LightShadows.None;
    }

    static void PlaceSuspect(Transform parent, Font font, string suspectName, string claim, string motive, Color color, Vector3 spot, List<Bounds> blocked)
    {
        var root = new GameObject(suspectName);
        root.transform.SetParent(parent, false);
        root.transform.position = spot;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.42f, 0.82f, 0.42f);
        body.transform.localPosition = new Vector3(0f, 0.82f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = MakeMaterial(suspectName + " Cloth", color);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localScale = Vector3.one * 0.28f;
        head.transform.localPosition = new Vector3(0f, 1.78f, 0f);
        head.GetComponent<Renderer>().sharedMaterial = MakeMaterial(suspectName + " Skin", new Color(0.72f, 0.58f, 0.46f));

        var suspect = root.AddComponent<Suspect>();
        suspect.suspectName = suspectName;
        suspect.claim = claim;
        suspect.motive = motive;
        CreateLabel(root.transform, suspectName, font, Color.white, 2.15f);

        blocked.Add(new Bounds(spot + Vector3.up * 0.9f, new Vector3(0.7f, 1.8f, 0.7f)));
    }

    static GameObject CreatePocketWatch(Transform parent)
    {
        var watch = new GameObject("Pocket Watch");
        watch.transform.SetParent(parent, false);
        var gold = MakeMaterial("Watch Gold", new Color(0.72f, 0.55f, 0.22f));
        var face = MakeMaterial("Watch Face", new Color(0.9f, 0.86f, 0.75f));

        var caseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        caseDisc.name = "Case";
        caseDisc.transform.SetParent(watch.transform, false);
        caseDisc.transform.localScale = new Vector3(0.09f, 0.012f, 0.09f);
        caseDisc.GetComponent<Renderer>().sharedMaterial = gold;

        var faceDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        faceDisc.name = "Face";
        faceDisc.transform.SetParent(watch.transform, false);
        faceDisc.transform.localPosition = new Vector3(0f, 0.014f, 0f);
        faceDisc.transform.localScale = new Vector3(0.06f, 0.006f, 0.06f);
        faceDisc.GetComponent<Renderer>().sharedMaterial = face;

        return watch;
    }

    static void CreateLabel(Transform parent, string text, Font font, Color color, float height)
    {
        if (font == null)
            return;

        var label = new GameObject(text + " Label");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, height, 0f);
        var mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = font;
        mesh.fontSize = 48;
        mesh.characterSize = 0.035f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        var renderer = label.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        label.AddComponent<FaceCamera>();
    }

    static void CreatePlayer(Vector3 spawn, Vector3 lookTarget)
    {
        var player = new GameObject("Detective");
        spawn.y = 0.05f;
        player.transform.position = spawn;
        var look = new Vector3(lookTarget.x, spawn.y, lookTarget.z);
        if ((look - spawn).sqrMagnitude > 0.01f)
            player.transform.LookAt(look);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.28f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.28f;
        player.AddComponent<StudyWalker>();

        var cameraObject = new GameObject("View");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.08f;
        camera.fieldOfView = 68f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.035f, 0.03f);
        if (cameraObject.GetComponent<UniversalAdditionalCameraData>() == null)
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraObject.AddComponent<AudioListener>();
    }

    static void CreateLighting(Transform parent, Bounds floorBounds)
    {
        var sunObject = new GameObject("Moonlight");
        sunObject.transform.SetParent(parent, false);
        sunObject.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.75f, 0.8f, 0.95f);
        sun.intensity = 0.28f;
        sun.shadows = LightShadows.Soft;

        var lamps = new GameObject("Room Lamps").transform;
        lamps.SetParent(parent, false);
        for (var x = floorBounds.min.x + 3f; x < floorBounds.max.x - 1f; x += 6.5f)
        {
            for (var z = floorBounds.min.z + 3f; z < floorBounds.max.z - 1f; z += 6.5f)
            {
                var lamp = new GameObject("Lamp");
                lamp.transform.SetParent(lamps, false);
                lamp.transform.position = new Vector3(x, 2.4f, z);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.74f, 0.5f);
                light.intensity = 1.7f;
                light.range = 11f;
                light.shadows = LightShadows.None;
            }
        }
    }

    static void CreateHint(Font font)
    {
        var canvasObject = new GameObject("Hint");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();

        var textObject = new GameObject("Hint Text");
        textObject.transform.SetParent(canvasObject.transform, false);
        var text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 18;
        text.color = new Color(0.94f, 0.88f, 0.76f, 0.92f);
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        text.text = "Blackwood Manor  —  The Study\nWASD move     Mouse look     Esc unlocks the cursor";
        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -18f);
        rect.sizeDelta = new Vector2(760f, 70f);
    }

    static void DressAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.2f, 0.18f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.1f, 0.08f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.03f, 0.025f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.07f, 0.06f, 0.05f);
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.skybox = null;
    }

    static void AddColliders(Transform parent, List<GameObject> spawned)
    {
        var collision = new GameObject("Collision");
        collision.transform.SetParent(parent, false);
        foreach (var go in spawned)
        {
            var n = go.name.ToLowerInvariant();
            var architectural = n.Contains("floor") || n.Contains("wall");
            if (!architectural && !IsLargeFurniture(go))
                continue;
            AddBox(collision.transform, BoundsOf(go));
        }
    }

    static void AddOuterShell(Transform parent, float width, float depth, float height)
    {
        var shell = new GameObject("Boundary");
        shell.transform.SetParent(parent, false);
        const float thickness = 0.45f;
        var y = height * 0.5f;
        AddBox(shell.transform, new Bounds(new Vector3(width * 0.5f, y, -thickness * 0.5f), new Vector3(width + thickness * 2f, height, thickness)));
        AddBox(shell.transform, new Bounds(new Vector3(width * 0.5f, y, depth + thickness * 0.5f), new Vector3(width + thickness * 2f, height, thickness)));
        AddBox(shell.transform, new Bounds(new Vector3(-thickness * 0.5f, y, depth * 0.5f), new Vector3(thickness, height, depth)));
        AddBox(shell.transform, new Bounds(new Vector3(width + thickness * 0.5f, y, depth * 0.5f), new Vector3(thickness, height, depth)));
    }

    static void AddBox(Transform parent, Bounds worldBounds)
    {
        if (worldBounds.size.sqrMagnitude < 0.0001f)
            return;

        var boxObject = new GameObject("Collider");
        boxObject.transform.SetParent(parent, false);
        boxObject.transform.position = worldBounds.center;
        var box = boxObject.AddComponent<BoxCollider>();
        box.size = worldBounds.size;
    }

    static bool IsLargeFurniture(GameObject go)
    {
        var n = go.name.ToLowerInvariant();
        if (n.Contains("light"))
            return false;
        return n.Contains("sofa") || n.Contains("bookshelf") || n.Contains("desk")
            || n.Contains("table") || n.Contains("stove") || n.Contains("cupboard")
            || n.Contains("shelf") || n.Contains("chair") || n.Contains("hearth");
    }

    static GameObject FindNamed(List<GameObject> objects, string include, string exclude = null)
    {
        include = include.ToLowerInvariant();
        exclude = exclude?.ToLowerInvariant();
        return objects.FirstOrDefault(go =>
        {
            var n = go.name.ToLowerInvariant();
            return n.Contains(include) && (exclude == null || !n.Contains(exclude));
        });
    }

    static Vector3 Beside(GameObject go, Vector3 floorCenter, float extra)
    {
        var bounds = BoundsOf(go);
        var center = new Vector3(bounds.center.x, 0f, bounds.center.z);
        var direction = floorCenter - center;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;
        direction.Normalize();
        return center + direction * (Mathf.Max(bounds.extents.x, bounds.extents.z) + extra);
    }

    static Vector3 ClearSpot(Vector3 desired, Vector3 floorCenter, List<Bounds> blocked)
    {
        var point = desired;
        point.y = 0f;
        for (var i = 0; i < 10; i++)
        {
            var footprint = new Bounds(point + Vector3.up * 0.9f, new Vector3(0.8f, 1.8f, 0.8f));
            if (!blocked.Any(bounds => bounds.Intersects(footprint)))
                return point;

            var direction = floorCenter - point;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector3.forward;
            point += direction.normalized * 0.4f;
        }
        return point;
    }

    static Bounds Encapsulate(IEnumerable<GameObject> objects)
    {
        var combined = new Bounds();
        var started = false;
        foreach (var go in objects)
        {
            var bounds = BoundsOf(go);
            if (!started)
            {
                combined = bounds;
                started = true;
            }
            else
            {
                combined.Encapsulate(bounds);
            }
        }
        return combined;
    }

    static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<MeshRenderer>();
        var started = false;
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var renderer in renderers)
        {
            if (!started)
            {
                bounds = renderer.bounds;
                started = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return bounds;
    }

    static Material MakeMaterial(string assetName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        var material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        var path = MaterialFolder + "/" + assetName.Replace(" ", "") + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}

[InitializeOnLoad]
static class StudyLayoutAutoBuild
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "StudyBuild.request"));

    static StudyLayoutAutoBuild()
    {
        if (File.Exists(Flag))
            EditorApplication.delayCall += RunWhenReady;
    }

    static void RunWhenReady()
    {
        if (!File.Exists(Flag))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunWhenReady;
            return;
        }

        File.Delete(Flag);
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            StudyLayoutBuilder.Build();
    }
}

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
    const string DemoScene = "Assets/Scenes/ZNS3D/Vintage Living Room Game Pack/Demo/Demo_Scene.unity";
    const string StudyScene = "Assets/Scenes/Study.unity";
    const string MaterialFolder = "Assets/Materials/Study";

    const string BottlePath = "Assets/BK_AlchemistHouse/Prefabs/Items/Bottles/Bottle01.prefab";
    const string ReportPath = "Assets/BK_AlchemistHouse/Prefabs/Items/Books/Book06.prefab";
    const string NotePath = "Assets/BK_AlchemistHouse/Prefabs/Items/Books/Book14.prefab";
    const string LampPath = "Assets/BK_AlchemistHouse/Prefabs/Items/Lights/Oillamp01_on.prefab";
    const string GlobePath = "Assets/BK_AlchemistHouse/Prefabs/Items/Misc/Globe.prefab";
    const string StovePath = "Assets/BK_AlchemistHouse/Prefabs/Furniture/StoveLit.prefab";

    struct Piece
    {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string group;
        public string name;
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
        var pieces = CaptureDemoPieces();
        if (pieces.Count < 10)
            throw new InvalidOperationException("Only found " + pieces.Count + " pieces in the vintage demo scene.");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "Study");

        var study = new GameObject("Blackwood Study");
        var architecture = new GameObject("Architecture").transform;
        architecture.SetParent(study.transform, false);
        var spawned = new List<GameObject>();

        foreach (var piece in pieces)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(piece.prefab);
            instance.name = piece.name;
            instance.transform.SetPositionAndRotation(piece.position, piece.rotation);
            instance.transform.localScale = piece.scale;
            instance.transform.SetParent(architecture, true);
            spawned.Add(instance);
        }

        var floorPieces = spawned.Where(go => go.name.ToLowerInvariant().Contains("floor")).ToList();
        if (floorPieces.Count == 0)
            throw new InvalidOperationException("The vintage room has no floor pieces.");

        var floorBounds = Encapsulate(floorPieces);
        architecture.position += new Vector3(-floorBounds.center.x, -floorBounds.max.y, -floorBounds.center.z);
        floorBounds = Encapsulate(floorPieces);
        var floorCenter = new Vector3(floorBounds.center.x, 0f, floorBounds.center.z);

        AddColliders(study.transform, spawned);
        AddRoomBoundary(study.transform, floorBounds);

        var clues = new GameObject("Clues").transform;
        clues.SetParent(study.transform, false);
        var people = new GameObject("People").transform;
        people.SetParent(study.transform, false);

        var desk = FindNamed(spawned, "photo_frame_desk") ?? FindNamed(spawned, "desk", "light");
        var shelf = FindNamed(spawned, "bookshelf");
        var door = FindNamed(spawned, "big_door") ?? FindNamed(spawned, "small_door") ?? FindNamed(spawned, "door");
        var hearthWall = spawned
            .Where(go =>
            {
                var n = go.name.ToLowerInvariant();
                return n.Contains("wall") && n.Contains("default");
            })
            .OrderByDescending(go => BoundsOf(go).size.sqrMagnitude)
            .FirstOrDefault();

        if (desk == null)
            throw new InvalidOperationException("Could not find the desk.");

        var deskBounds = BoundsOf(desk);
        var deskTop = new Vector3(deskBounds.center.x, deskBounds.max.y, deskBounds.center.z);
        var along = desk.transform.right;
        along.y = 0f;
        if (along.sqrMagnitude < 0.01f)
            along = Vector3.right;
        along.Normalize();

        var watch = CreatePocketWatch(clues);
        SeatExisting(watch, deskTop - along * 0.28f, 20f, 0.12f);
        AddClue(watch, "Broken Pocket Watch", "The hands stopped at 8:30. Whatever happened in this room, it happened then.", font);

        var report = SeatPrefab(ReportPath, clues, deskTop + along * 0.05f, 35f, 0.16f);
        AddClue(report, "Torn Medical Report", "Victor tore into a report about his own health records. Someone had been changing them.", font);

        var bottle = SeatPrefab(BottlePath, clues, deskTop + along * 0.34f, -15f, 0.22f);
        AddClue(bottle, "Medication Bottle", "Victor's medicine. The seal is broken, and what remains does not match the dose on the label.", font);

        SeatPrefab(LampPath, clues, deskTop - along * 0.62f, 10f, 0.42f);
        SeatPrefab(GlobePath, clues, deskTop + along * 0.62f + Vector3.Cross(along, Vector3.up) * 0.15f, 40f, 0.38f);

        if (shelf != null)
        {
            var shelfBounds = BoundsOf(shelf);
            var outDir = floorCenter - shelfBounds.center;
            outDir.y = 0f;
            if (outDir.sqrMagnitude < 0.01f)
                outDir = Vector3.forward;
            outDir.Normalize();
            var notePoint = shelfBounds.center + outDir * (shelfBounds.extents.magnitude * 0.15f + 0.15f);
            notePoint.y = shelfBounds.center.y;
            var note = SeatPrefab(NotePath, clues, notePoint, 80f, 0.2f);
            AddClue(note, "Doctor's Appointment Note", "A note in Victor's hand. He meant to confront Dr. Reed here at 8:30.", font);
        }

        if (door != null)
        {
            var doorBounds = BoundsOf(door);
            var start = new Vector3(doorBounds.center.x, 0.02f, doorBounds.center.z);
            var inward = floorCenter - start;
            inward.y = 0f;
            if (inward.sqrMagnitude < 0.01f)
                inward = Vector3.forward;
            inward.Normalize();
            var prints = new GameObject("Muddy Footprints");
            prints.transform.SetParent(clues, false);
            prints.transform.position = start + inward * 1.5f;
            var mud = MakeMaterial("Mud", new Color(0.18f, 0.13f, 0.08f));
            for (var i = 0; i < 5; i++)
            {
                var print = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                print.name = "Print";
                print.transform.SetParent(prints.transform, false);
                var side = Vector3.Cross(inward, Vector3.up) * ((i % 2 == 0) ? 0.16f : -0.16f);
                print.transform.position = start + inward * (0.7f + i * 0.42f) + side;
                print.transform.rotation = Quaternion.Euler(0f, i * 18f, 0f);
                print.transform.localScale = new Vector3(0.22f, 0.008f, 0.4f);
                print.GetComponent<Renderer>().sharedMaterial = mud;
            }
            AddClue(prints, "Muddy Footprints", "Wet prints come in from the conservatory door and stop inside the study.", font, 0.35f);
        }

        if (hearthWall != null)
        {
            var wallBounds = BoundsOf(hearthWall);
            var inward = floorCenter - wallBounds.center;
            inward.y = 0f;
            inward.Normalize();
            var hearthPoint = new Vector3(wallBounds.center.x, 0f, wallBounds.center.z) + inward * 1.15f;
            var stove = SeatPrefab(StovePath, architecture, hearthPoint, 0f, 1.35f);
            var forward = stove.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f)
            {
                var turn = Vector3.SignedAngle(forward, inward, Vector3.up);
                stove.transform.Rotate(0f, turn, 0f, Space.World);
                SeatExisting(stove, hearthPoint, 0f, 1.35f);
            }
            stove.name = "Hearth";
        }

        var blocked = spawned
            .Where(IsLargeFurniture)
            .Select(BoundsOf)
            .ToList();

        PlaceSuspect(people, font, "Evelyn Hale",
            "She says she was in the dining room the whole time.",
            "Victor was going to cut her out of the will.",
            new Color(0.45f, 0.16f, 0.22f),
            ClearSpot(Beside(desk, floorCenter, 1.6f) + new Vector3(-1.4f, 0f, 0.4f), floorCenter, blocked),
            blocked);
        PlaceSuspect(people, font, "Arthur",
            "He says he was polishing silver in the kitchen.",
            "Victor told him earlier this evening that he was finished at the manor.",
            new Color(0.22f, 0.22f, 0.24f),
            ClearSpot(door != null ? Beside(door, floorCenter, 1.3f) : floorCenter + new Vector3(2f, 0f, -1f), floorCenter, blocked),
            blocked);
        PlaceSuspect(people, font, "Thomas Hale",
            "He says he was in his bedroom.",
            "He is badly in debt, and he needed his father's money.",
            new Color(0.16f, 0.24f, 0.42f),
            ClearSpot(shelf != null ? Beside(shelf, floorCenter, 1.2f) : floorCenter + new Vector3(-2f, 0f, 1.5f), floorCenter, blocked),
            blocked);
        PlaceSuspect(people, font, "Dr. Reed",
            "He says he was in the conservatory.",
            "He had been falsifying Victor's medical records, and Victor had found out.",
            new Color(0.18f, 0.34f, 0.28f),
            ClearSpot(Beside(desk, floorCenter, 1.5f), floorCenter, blocked),
            blocked);

        var victimSpot = ClearSpot(Beside(desk, floorCenter, 0.9f) + Vector3.Cross(along, Vector3.up) * 0.8f, floorCenter, blocked);
        var victim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        victim.name = "Victor Hale";
        victim.transform.SetParent(people, false);
        victim.transform.localScale = new Vector3(0.38f, 0.85f, 0.38f);
        victim.transform.rotation = Quaternion.Euler(0f, 70f, 90f);
        victim.transform.position = new Vector3(victimSpot.x, 0.2f, victimSpot.z);
        victim.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Victim", new Color(0.12f, 0.1f, 0.1f));
        CreateLabel(victim.transform, "Victor Hale", font, new Color(0.85f, 0.8f, 0.74f), 1.1f);

        CreatePlayer(floorCenter, deskTop);
        CreateLighting(study.transform, floorBounds);
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

        Debug.Log("Study pieces: " + spawned.Count + " floor size: " + floorBounds.size + " desk: " + desk.name);
    }

    static List<Piece> CaptureDemoPieces()
    {
        var demo = SceneManager.GetSceneByPath(DemoScene);
        var openedHere = !demo.IsValid() || !demo.isLoaded;
        if (openedHere)
            demo = EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Additive);

        try
        {
            var wanted = new HashSet<string> { "Floors", "Walls", "Ceiling", "Assets", "Indoor Lighting" };
            var pieces = new List<Piece>();
            foreach (var root in demo.GetRootGameObjects())
            {
                foreach (Transform group in root.transform)
                {
                    if (!wanted.Contains(group.name))
                        continue;
                    Collect(group, group.name, pieces);
                }
            }
            return pieces;
        }
        finally
        {
            if (openedHere)
                EditorSceneManager.CloseScene(demo, true);
        }
    }

    static void Collect(Transform parent, string group, List<Piece> pieces)
    {
        foreach (Transform child in parent)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
            {
                if (SkipPiece(child.name))
                    continue;

                var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(child.gameObject);
                var path = source != null ? AssetDatabase.GetAssetPath(source) : null;
                var prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                pieces.Add(new Piece
                {
                    prefab = prefab,
                    position = child.position,
                    rotation = child.rotation,
                    scale = child.localScale,
                    group = group,
                    name = child.name
                });
            }
            else
            {
                Collect(child, group, pieces);
            }
        }
    }

    static bool SkipPiece(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("tv") || n.Contains("phone") || n.Contains("camera") || n.Contains("volume");
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

    static void CreatePlayer(Vector3 floorCenter, Vector3 lookTarget)
    {
        var player = new GameObject("Detective");
        var spawn = floorCenter + new Vector3(0f, 0.05f, -1.2f);
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
        var corners = new[]
        {
            new Vector3(floorBounds.min.x + 1.2f, 2.15f, floorBounds.min.z + 1.2f),
            new Vector3(floorBounds.max.x - 1.2f, 2.15f, floorBounds.min.z + 1.2f),
            new Vector3(floorBounds.min.x + 1.2f, 2.15f, floorBounds.max.z - 1.2f),
            new Vector3(floorBounds.max.x - 1.2f, 2.15f, floorBounds.max.z - 1.2f)
        };
        foreach (var corner in corners)
        {
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(lamps, false);
            lamp.transform.position = corner;
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.46f);
            light.intensity = 2.4f;
            light.range = 7.5f;
            light.shadows = LightShadows.Soft;
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
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.skybox = null;
    }

    static void AddColliders(Transform parent, List<GameObject> spawned)
    {
        var collision = new GameObject("Collision");
        collision.transform.SetParent(parent, false);
        foreach (var go in spawned)
        {
            var n = go.name.ToLowerInvariant();
            var architectural = n.Contains("floor") || n.Contains("wall") || n.Contains("ceiling");
            if (!architectural && !IsLargeFurniture(go))
                continue;
            AddBox(collision.transform, BoundsOf(go));
        }
    }

    static void AddRoomBoundary(Transform parent, Bounds floorBounds)
    {
        var boundary = new GameObject("Boundary");
        boundary.transform.SetParent(parent, false);
        const float height = 3.2f;
        const float thickness = 0.3f;
        var centerY = height * 0.5f;
        AddBox(boundary.transform, new Bounds(
            new Vector3(floorBounds.center.x, centerY, floorBounds.max.z + 0.2f),
            new Vector3(floorBounds.size.x + 1.2f, height, thickness)));
        AddBox(boundary.transform, new Bounds(
            new Vector3(floorBounds.center.x, centerY, floorBounds.min.z - 0.2f),
            new Vector3(floorBounds.size.x + 1.2f, height, thickness)));
        AddBox(boundary.transform, new Bounds(
            new Vector3(floorBounds.max.x + 0.2f, centerY, floorBounds.center.z),
            new Vector3(thickness, height, floorBounds.size.z + 1.2f)));
        AddBox(boundary.transform, new Bounds(
            new Vector3(floorBounds.min.x - 0.2f, centerY, floorBounds.center.z),
            new Vector3(thickness, height, floorBounds.size.z + 1.2f)));
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

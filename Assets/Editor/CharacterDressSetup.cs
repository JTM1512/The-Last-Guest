using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CharacterDressSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";
    const string EvelynPath = "Assets/Bandit/Prefabs/BanditMain.prefab";
    const string ArthurPath = "Assets/Homeless_people/Free_Scavenger/Prefab/Scavenger Variant.prefab";
    const string ThomasPath = "Assets/Survivalist/Prefab/Survivalist (1).prefab";
    const string ReedPath = "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/PT_Plague_Doctor.prefab";
    const string BodyPath = "Assets/DeadBody LITE/Prefabs/Body_Only/DeadBody (1).prefab";
    const string BottlePath = "Assets/Hash Game studios/Herbal & Medic Pack/Resources/Prefabs/Medic pack/Medical First Aid Bottle 1/Medical First Aid Bottle 1_collider.prefab";

    [MenuItem("The Last Guest/Dress Characters And Props")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        DressSuspect("Evelyn Hale", EvelynPath, 1.7f, 180f);
        DressSuspect("Arthur", ArthurPath, 1.7f, 200f);
        DressSuspect("Thomas Hale", ThomasPath, 1.75f, 160f);
        DressSuspect("Dr. Reed", ReedPath, 1.75f, 220f);
        ReplaceVictim();
        ReplaceMedicationBottle();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("CHARACTER DRESS OK");
    }

    static void DressSuspect(string suspectName, string prefabPath, float targetHeight, float yaw)
    {
        var root = GameObject.Find(suspectName);
        if (root == null)
        {
            Debug.LogWarning("Missing suspect " + suspectName);
            return;
        }

        ClearVisualChildren(root.transform);
        var model = SpawnModel(prefabPath, root.transform, "Model");
        if (model == null)
            return;

        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (suspectName == "Thomas Hale")
            PrepareSurvivalist(model);
        FitStanding(model, targetHeight);
        EnsureHitbox(root);
        foreach (var body in model.GetComponentsInChildren<Rigidbody>())
            KeepPropsStill.Freeze(body);
    }

    static void PrepareSurvivalist(GameObject model)
    {
        foreach (var transform in model.GetComponentsInChildren<Transform>(true))
        {
            var n = transform.name;
            if (n.Contains("FPS_HANDS") || n.Contains("FPS_Arms") || n.Contains("FPS_Shirt") || n.Contains("Backpack") || n.Contains("Cap1") || n.Contains("Vest1"))
                transform.gameObject.SetActive(false);
            if (n.Contains("Head1") || n.Contains("Shirt3") || n.Contains("Pants2") || n.Contains("Boots3") || (n.Contains("Male_Arms1") && !n.Contains("Gloves")))
                transform.gameObject.SetActive(true);
        }

        var urpMats = new System.Collections.Generic.Dictionary<string, Material>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Survivalist/Materials URP" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                urpMats[mat.name] = mat;
        }

        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.sharedMaterials;
            for (var i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null)
                    continue;
                var key = "URP_" + mats[i].name;
                if (urpMats.TryGetValue(key, out var urp))
                    mats[i] = urp;
                else if (mats[i].name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0 && urpMats.TryGetValue("URP_Head1", out var head))
                    mats[i] = head;
                else if (mats[i].name.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0 && urpMats.TryGetValue("URP_Eyes1", out var eyes))
                    mats[i] = eyes;
                else if (mats[i].name.IndexOf("Hand", System.StringComparison.OrdinalIgnoreCase) >= 0 && urpMats.TryGetValue("URP_Hands1_Skin1", out var hands))
                    mats[i] = hands;
            }
            renderer.sharedMaterials = mats;
        }
    }

    static void ReplaceVictim()
    {
        var old = GameObject.Find("Victor Hale");
        Vector3 spot = new Vector3(14.4f, 0f, 4.8f);
        Transform parent = null;
        Font font = null;
        if (old != null)
        {
            spot = old.transform.position;
            spot.y = 0f;
            parent = old.transform.parent;
            var label = old.GetComponentInChildren<TextMesh>();
            if (label != null)
                font = label.font;
            Object.DestroyImmediate(old);
        }

        if (parent == null)
        {
            var people = GameObject.Find("People");
            parent = people != null ? people.transform : null;
        }

        var root = new GameObject("Victor Hale");
        root.transform.SetParent(parent, false);
        root.transform.position = spot;

        var model = SpawnModel(BodyPath, root.transform, "Body");
        if (model != null)
        {
            model.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            FitLying(model, 1.85f);
            foreach (var body in model.GetComponentsInChildren<Rigidbody>())
                KeepPropsStill.Freeze(body);
        }

        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateLabel(root.transform, "Victor Hale", font, new Color(0.85f, 0.8f, 0.74f), 1.05f);
    }

    static void ReplaceMedicationBottle()
    {
        Clue bottleClue = null;
        foreach (var clue in Object.FindObjectsByType<Clue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (clue.clueName == "Medication Bottle")
            {
                bottleClue = clue;
                break;
            }
        }

        if (bottleClue == null)
        {
            Debug.LogWarning("Missing Medication Bottle clue");
            return;
        }

        var root = bottleClue.gameObject;
        var worldPos = root.transform.position;
        var worldRot = root.transform.rotation;
        ClearVisualChildren(root.transform, keepLabel: true);

        var model = SpawnModel(BottlePath, root.transform, "Bottle");
        if (model == null)
            return;

        foreach (var body in model.GetComponentsInChildren<Rigidbody>())
            KeepPropsStill.Freeze(body);

        FitProp(model, 0.22f);
        SeatProp(root, worldPos);
        root.transform.rotation = worldRot;
    }

    static GameObject SpawnModel(string path, Transform parent, string childName)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Missing prefab " + path);
            return null;
        }

        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        model.name = childName;
        model.transform.SetParent(parent, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        StripGameplay(model);
        return model;
    }

    static void StripGameplay(GameObject model)
    {
        foreach (var agent in model.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            Object.DestroyImmediate(agent);
        foreach (var controller in model.GetComponentsInChildren<CharacterController>(true))
            Object.DestroyImmediate(controller);
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
                continue;
            var typeName = behaviour.GetType().Name;
            if (typeName.Contains("Camera") || typeName.Contains("Input") || typeName.Contains("Player") || typeName.Contains("Movement") || typeName == "ThirdPersonController")
                behaviour.enabled = false;
        }
    }

    static void ClearVisualChildren(Transform root, bool keepLabel = true)
    {
        for (var i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            if (keepLabel && (child.name.Contains("Label") || child.name.Contains("Glow")))
                continue;
            Object.DestroyImmediate(child);
        }

        foreach (var renderer in root.GetComponents<Renderer>())
            Object.DestroyImmediate(renderer);
        foreach (var filter in root.GetComponents<MeshFilter>())
            Object.DestroyImmediate(filter);
        foreach (var collider in root.GetComponents<Collider>())
            Object.DestroyImmediate(collider);
    }

    static void FitStanding(GameObject model, float targetHeight)
    {
        var bounds = RendererBounds(model);
        if (bounds.size.y < 0.01f)
            return;
        var scale = targetHeight / bounds.size.y;
        model.transform.localScale *= scale;
        bounds = RendererBounds(model);
        var lift = -bounds.min.y;
        model.transform.position += Vector3.up * lift;
    }

    static void FitLying(GameObject model, float targetLength)
    {
        var bounds = RendererBounds(model);
        var longAxis = Mathf.Max(bounds.size.x, bounds.size.z, bounds.size.y);
        if (longAxis < 0.01f)
            return;
        var scale = targetLength / longAxis;
        model.transform.localScale *= scale;
        bounds = RendererBounds(model);
        model.transform.position += new Vector3(0f, -bounds.min.y + 0.01f, 0f);
    }

    static void FitProp(GameObject model, float targetHeight)
    {
        var bounds = RendererBounds(model);
        if (bounds.size.y < 0.01f)
            return;
        var scale = targetHeight / bounds.size.y;
        model.transform.localScale *= scale;
    }

    static void SeatProp(GameObject root, Vector3 surfacePoint)
    {
        var bounds = RendererBounds(root);
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            root.transform.position = surfacePoint;
            return;
        }

        var delta = surfacePoint - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        root.transform.position += delta;
    }

    static void EnsureHitbox(GameObject root)
    {
        var box = root.GetComponent<CapsuleCollider>();
        if (box == null)
            box = root.AddComponent<CapsuleCollider>();
        box.height = 1.8f;
        box.radius = 0.28f;
        box.center = new Vector3(0f, 0.9f, 0f);
        box.isTrigger = false;
    }

    static Bounds RendererBounds(GameObject go)
    {
        Bounds bounds = default;
        var has = false;
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            if (!has)
            {
                bounds = renderer.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return has ? bounds : new Bounds(go.transform.position, Vector3.zero);
    }

    static void CreateLabel(Transform parent, string text, Font font, Color color, float height)
    {
        var label = new GameObject(text + " Label");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = new Vector3(0f, height, 0f);
        var mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = font;
        mesh.fontSize = 48;
        mesh.characterSize = 0.03f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        if (font != null)
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        label.AddComponent<FaceCamera>();
    }
}

[InitializeOnLoad]
static class CharacterDressAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "DressCharacters.request"));

    static CharacterDressAutoSetup()
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
        CharacterDressSetup.Run();
    }
}

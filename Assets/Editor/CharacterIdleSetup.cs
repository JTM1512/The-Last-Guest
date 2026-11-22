using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterIdleSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";
    const string IdleControllerPath = "Assets/Animations/StandingIdle.controller";
    const string IdleClipPath = "Assets/Survivalist/StarterAssets/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx";
    const string ScavengerControllerPath = "Assets/Homeless_people/Free_Scavenger/Animation/Free_scavenger.controller";

    [MenuItem("The Last Guest/Fix Character Idle And Materials")]
    public static void Run()
    {
        UrpMaterialFix.Fix();
        MakeIdleLoopOnScavenger();
        var idle = EnsureIdleController();
        var scavenger = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ScavengerControllerPath);

        ApplyIdle("Evelyn Hale", idle);
        ApplyIdle("Arthur", scavenger != null ? scavenger : idle);
        ApplyIdle("Thomas Hale", idle);
        ApplyIdle("Dr. Reed", idle);

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path == StudyScene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("CHARACTER IDLE OK");
    }

    static void MakeIdleLoopOnScavenger()
    {
        var path = "Assets/Homeless_people/Free_Scavenger/FBX/Scavenger.fbx";
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;

        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
            return;

        var changed = false;
        for (var i = 0; i < clips.Length; i++)
        {
            if (!clips[i].loopTime)
            {
                clips[i].loopTime = true;
                changed = true;
            }
        }

        if (!changed)
            return;

        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    static RuntimeAnimatorController EnsureIdleController()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleControllerPath);
        var idleClip = FindIdleClip();
        if (idleClip == null)
        {
            Debug.LogWarning("Standing idle clip missing");
            return existing;
        }

        if (existing == null)
        {
            existing = AnimatorController.CreateAnimatorControllerAtPath(IdleControllerPath);
            var root = existing.layers[0].stateMachine;
            var state = root.AddState("Idle");
            state.motion = idleClip;
            root.defaultState = state;
        }
        else
        {
            var root = existing.layers[0].stateMachine;
            if (root.defaultState != null)
                root.defaultState.motion = idleClip;
        }

        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssets();
        return existing;
    }

    static AnimationClip FindIdleClip()
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(IdleClipPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview"))
                return clip;
        }

        return null;
    }

    static void ApplyIdle(string suspectName, RuntimeAnimatorController controller)
    {
        var root = GameObject.Find(suspectName);
        if (root == null || controller == null)
            return;

        var model = root.transform.Find("Model");
        if (model == null)
            return;

        var animator = model.GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = model.gameObject.AddComponent<Animator>();

        animator.enabled = true;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;
        EditorUtility.SetDirty(animator);
    }
}

[InitializeOnLoad]
static class CharacterIdleAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CharacterIdle.request"));

    static CharacterIdleAutoSetup()
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
        CharacterIdleSetup.Run();
    }
}

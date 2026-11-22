using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TimerAudioSetup
{
    const string StudyScene = "Assets/Scenes/Study.unity";

    [MenuItem("The Last Guest/Add Timer And Audio")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != StudyScene)
            scene = EditorSceneManager.OpenScene(StudyScene);

        ImportAudio("Assets/Audio/EerieAmbience.wav");
        ImportAudio("Assets/Audio/CluePickup.wav");
        ImportAudio("Assets/Audio/ClueAnalyse.wav");
        ImportAudio("Assets/Audio/SuspectTalk.wav");

        var ambience = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/EerieAmbience.wav");
        var pickup = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/CluePickup.wav");
        var analyse = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ClueAnalyse.wav");
        var talk = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SuspectTalk.wav");

        var hint = GameObject.Find("Hint");
        var sample = hint != null ? hint.GetComponentInChildren<Text>(true) : null;
        var font = sample != null ? sample.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text timerText = null;
        var existingTimer = GameObject.Find("Timer Text");
        if (existingTimer != null)
        {
            timerText = existingTimer.GetComponent<Text>();
        }
        else if (hint != null)
        {
            var timerObject = new GameObject("Timer Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timerObject.transform.SetParent(hint.transform, false);
            timerText = timerObject.GetComponent<Text>();
            timerText.font = font;
            timerText.fontSize = 28;
            timerText.alignment = TextAnchor.UpperRight;
            timerText.color = new Color(0.94f, 0.86f, 0.68f, 1f);
            timerText.raycastTarget = false;
            timerText.text = "5:00";
            var rect = timerText.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -18f);
            rect.sizeDelta = new Vector2(160f, 40f);
        }

        var study = GameObject.Find("Blackwood Study");
        if (study == null)
            study = new GameObject("Blackwood Study");

        var timer = study.GetComponent<CaseTimer>();
        if (timer == null)
            timer = study.AddComponent<CaseTimer>();
        timer.seconds = 300f;
        timer.readout = timerText;

        var audioHost = GameObject.Find("Game Audio");
        if (audioHost == null)
            audioHost = new GameObject("Game Audio");
        var audio = audioHost.GetComponent<GameAudio>();
        if (audio == null)
            audio = audioHost.AddComponent<GameAudio>();
        audio.ambience = ambience;
        audio.cluePickup = pickup;
        audio.clueAnalyse = analyse;
        audio.suspectTalk = talk;

        var music = audioHost.GetComponents<AudioSource>();
        if (music.Length == 0)
        {
            var musicSource = audioHost.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.28f;
            audio.music = musicSource;
            var effects = audioHost.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.volume = 0.7f;
            audio.effects = effects;
        }
        else
        {
            audio.music = music[0];
            audio.music.loop = true;
            audio.music.playOnAwake = false;
            if (music.Length > 1)
                audio.effects = music[1];
            else
            {
                var effects = audioHost.AddComponent<AudioSource>();
                effects.playOnAwake = false;
                audio.effects = effects;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("TIMER AUDIO OK");
    }

    static void ImportAudio(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null)
            return;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.SaveAndReimport();
    }
}

[InitializeOnLoad]
static class TimerAudioAutoSetup
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TimerAudio.request"));

    static TimerAudioAutoSetup()
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
        TimerAudioSetup.Run();
    }
}

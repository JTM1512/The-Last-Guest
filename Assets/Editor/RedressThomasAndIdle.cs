using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class RedressThomasAndIdle
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "RedressThomas.request"));

    static RedressThomasAndIdle()
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
        CharacterIdleSetup.Run();
        Debug.Log("REDRESS THOMAS OK");
    }
}

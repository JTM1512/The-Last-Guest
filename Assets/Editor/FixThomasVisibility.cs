using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class FixThomasVisibility
{
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "FixThomas.request"));

    static FixThomasVisibility()
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
        Debug.Log("FIX THOMAS OK");
    }
}

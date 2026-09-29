using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class CopySteamAppId
{
    [PostProcessBuild]
    private static void OnPostBuild(BuildTarget target, string exePath)
    {
        if (target != BuildTarget.StandaloneWindows64) return;
        string src = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "steam_appid.txt");
        string dst = Path.Combine(Path.GetDirectoryName(exePath), "steam_appid.txt");
        if (File.Exists(src)) File.Copy(src, dst, true);
        else Debug.LogWarning("steam_appid.txt not found in project root.");
    }
}
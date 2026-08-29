using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class ServerConfig
{
    public string DatabaseMode; // "Local" or "Remote"
    public string RemoteServerURL;
    public string LocalPythonScriptPath;
}

public class DrawingConfig
{
    public bool DestroyLineOnErase = false;
    public List<string> DrawingBlockedScenes = new() { "Login" };
}

public class ConfigurationManager : MonoBehaviour
{
    // TODO: ConfigurationManager could be used for store Game settings, user preferences, and other configuration data in the future.
    //       Create a simple but poverful nested class structure to store all the configuration data in a single JSON file, and load it at runtime.
    //       And PlayerPrefs also could be used for store user preferences.

    public static ServerConfig CurrentConfig;
    public static DrawingConfig CurrentDrawingConfig;

    private void Awake()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "config.json");

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            CurrentConfig = JsonUtility.FromJson<ServerConfig>(json);
            Debug.Log($"Loaded Config. Mode: {CurrentConfig.DatabaseMode}");
        }
        else
        {
            Debug.LogError("config.json not found!");
        }

        CurrentDrawingConfig = new DrawingConfig();
    }
}
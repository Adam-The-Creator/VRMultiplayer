using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single named server configuration. Keep as many of these around as you like
/// (e.g. "Local", "Ngrok", "Staging", "Production") and switch between them via
/// NetworkSettings.ActiveProfile instead of editing a URL by hand every time.
/// </summary>
[System.Serializable]
public class ServerProfile
{
    public string Name;
    public string ServerURL;
    public DatabaseMode Mode = DatabaseMode.Local;

    [Tooltip("Only used when Mode == Local. Path to the Python server script, relative to the project/build.")]
    public string LocalPythonScriptPath = "../DatabaseServer/server.py";

    [Tooltip("Seconds to wait for a ping/response before treating the server as unreachable.")]
    public int TimeoutSeconds = 5;

    [Tooltip("Extra HTTP headers to send with every request on this profile. Useful e.g. for ngrok's " +
             "free tier, which needs 'ngrok-skip-browser-warning: true' or it returns an HTML " +
             "interstitial page instead of JSON. Not wired into DatabaseManager yet - available for " +
             "when you need it.")]
    public Dictionary<string, string> ExtraHeaders = new();

    public ServerProfile() { }

    public ServerProfile(string name, string serverUrl, DatabaseMode mode)
    {
        Name = name;
        ServerURL = serverUrl;
        Mode = mode;
    }

    /// <summary>Server URL with any trailing slash trimmed, so endpoint concatenation never produces "//".</summary>
    public string GetNormalizedUrl()
    {
        return string.IsNullOrEmpty(ServerURL) ? ServerURL : ServerURL.TrimEnd('/');
    }
}

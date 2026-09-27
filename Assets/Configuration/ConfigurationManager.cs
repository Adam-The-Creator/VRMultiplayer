using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

/// <summary>
/// Loads/saves the game's Preferences (preferences.json).
///
/// IMPORTANT: the live file lives in Application.persistentDataPath, NOT StreamingAssets.
/// On Android/Quest, StreamingAssets is packed inside the APK and can't be read with plain
/// System.IO (it needs an async UnityWebRequest, and even then it's read-only). persistentDataPath
/// is a real writable folder on every platform Unity targets, so it's the only place that works
/// for both reading AND writing settings across Editor, Windows, and Quest builds.
///
/// Resilience: if preferences.json is missing, corrupted, or missing individual fields, it fall
/// back to the defaults defined on Preferences/NetworkSettings/etc instead of failing to start.
/// A single malformed field doesn't take down the whole file either (see the Error handler below) -
/// so adding new settings later, or someone hand-editing the file badly, can never break the game.
/// </summary>
public class ConfigurationManager : MonoBehaviour
{
    private const string PreferencesFileName = "preferences.json";

    private static Preferences _current;
    public static Preferences Current
    {
        get
        {
            if (_current == null) Load();
            return _current;
        }
    }

    public static event Action OnPreferencesLoaded;
    public static event Action OnPreferencesSaved;

    private static string PreferencesPath => Path.Combine(Application.persistentDataPath, PreferencesFileName);

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,  // unknown fields in the file are ignored, not fatal
        Converters = { new StringEnumConverter() },            // writes "Mode": "Local" instead of "Mode": 0
        Error = (sender, args) =>
        {
            // A single bad field (wrong type, typo'd enum, etc) is skipped instead of aborting
            // the whole parse and throwing away every other good setting in the file.
            Debug.LogWarning($"[ConfigurationManager] Ignoring bad field in preferences.json: {args.ErrorContext.Error.Message}");
            args.ErrorContext.Handled = true;
        }
    };

    private static bool s_initialized;

    private void Awake()
    {
        // Same singleton guard pattern used by the other *Manager singletons in this project.
        if (s_initialized)
        {
            Destroy(gameObject);
            return;
        }
        s_initialized = true;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    /// <summary>Loads Current from disk, or creates it from defaults if that fails for any reason.</summary>
    public static void Load()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                string json = File.ReadAllText(PreferencesPath);
                // Deserializing onto a fresh Preferences: any section/field missing from the JSON
                // keeps the C# default declared on the class; any field the JSON has that we don't
                // know about yet is silently ignored (MissingMemberHandling.Ignore above).
                _current = JsonConvert.DeserializeObject<Preferences>(json, JsonSettings) ?? new Preferences();
                Debug.Log($"[ConfigurationManager] Loaded preferences from {PreferencesPath}");
            }
            else
            {
                Debug.Log($"[ConfigurationManager] No preferences.json found at {PreferencesPath}, creating one with defaults.");
                _current = new Preferences();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigurationManager] preferences.json failed to load ({e.Message}). Falling back to defaults.");
            _current = new Preferences();
        }

        // Guard against ActiveProfile pointing at a profile that no longer exists
        // (e.g. someone hand-edited the file and typo'd or removed it).
        if (_current.Network.GetActiveProfile() == null && _current.Network.Profiles.Count > 0)
        {
            Debug.LogWarning($"[ConfigurationManager] ActiveProfile '{_current.Network.ActiveProfile}' not found, " +
                              $"falling back to '{_current.Network.Profiles[0].Name}'.");
            _current.Network.ActiveProfile = _current.Network.Profiles[0].Name;
        }

        Save(); // normalizes the file on disk: creates it on first run, fills in any newly-added fields
        OnPreferencesLoaded?.Invoke();
    }

    public static void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(PreferencesPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(PreferencesPath, JsonConvert.SerializeObject(Current, JsonSettings));
            OnPreferencesSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigurationManager] Failed to save preferences.json: {e.Message}");
        }
    }

    // --- Convenience passthroughs for network settings, since that's what currently reads this ---

    public static ServerProfile ActiveServerProfile => Current.Network.GetActiveProfile();

    public static void SetActiveServerProfile(string profileName)
    {
        Current.Network.ActiveProfile = profileName;
        Save();
    }

    public static void AddOrUpdateServerProfile(ServerProfile profile)
    {
        var existing = Current.Network.GetProfile(profile.Name);
        if (existing != null) Current.Network.Profiles.Remove(existing);
        Current.Network.Profiles.Add(profile);
        Save();
    }

    // Quest/Android apps are far more often backgrounded than cleanly quit, so save on pause too.
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}

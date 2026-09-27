[System.Serializable]
public class Preferences
{
    /// <summary>
    /// Bump this if a future field rename/restructure ever needs to migrate old preferences.json
    /// files on load. ConfigurationManager doesn't enforce anything on it yet - it's just a hook
    /// for later.
    /// </summary>
    public int SchemaVersion = 1;

    public NetworkSettings Network = new();
    public AudioSettings Audio = new();
    public DrawingSettings Drawing = new();

    // Add new *Settings classes here as they're needed, e.g.:
    // public GraphicsSettings Graphics = new();
    // public ControlSettings Controls = new();
}

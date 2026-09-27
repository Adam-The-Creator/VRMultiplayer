/// <summary>
/// Placeholder for future audio preferences. Not wired to MusicManager/Brush yet -
/// ConfigurationManager will already load/save these fields correctly once something reads them.
/// </summary>
[System.Serializable]
public class AudioSettings
{
    public float MasterVolume = 1f;
    public float MusicVolume = 0.5f;
    public bool Muted = false;
}

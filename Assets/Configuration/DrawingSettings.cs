using System.Collections.Generic;

/// <summary>Replaces the old DrawingConfig class - same fields, now nested under Preferences.Drawing.</summary>
[System.Serializable]
public class DrawingSettings
{
    public bool DestroyLineOnErase = false;
    public List<string> DrawingBlockedScenes = new() { "Login" };
}

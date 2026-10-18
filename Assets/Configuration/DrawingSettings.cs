using System.Collections.Generic;

/// <summary>Replaces the old DrawingConfig class - same fields, now nested under Preferences.Drawing.</summary>
[System.Serializable]
public class DrawingSettings
{
    public bool DestroyLineOnErase = false;
    public List<string> DrawingBlockedScenes = new() { "Login" };

    /// <summary>
    /// Records what the players do while they draw: head, hand and rig poses plus menu/mode/color/width changes.
    /// The data is saved with the drawing (TrackedBehavior and events) and shared between the players of a session.
    /// </summary>
    public bool TrackingEnabled = true;

    /// <summary>
    /// Pose samples per second (BehaviorTracker clamps it to 1-30). Higher means more data per drawing: if a drawing
    /// would exceed MongoDB's document limit the server keeps only every Nth sample (Metadata.trackingDecimation).
    /// </summary>

    public float TrackingSampleRateHz = 10f;
}

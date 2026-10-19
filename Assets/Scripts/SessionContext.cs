using System.Threading.Tasks;

/// <summary>
/// The session that the drawings of the open game scene belong to. It is fixed when the scene opens: a loaded drawing
/// keeps its own session, a new drawing uses the session that is active on the server at that moment. Switching the
/// active session afterwards only affects the scenes opened later.
/// </summary>
public static class SessionContext
{
    public static string SceneSessionId { get; set; }

    /// <summary>
    /// The session of the open scene. Falls back to the server's active session when the scene has not resolved one
    /// yet (e.g. a save is requested while the scene is still loading).
    /// </summary>
    public static async Task<string> GetSceneSessionIdAsync()
    {
        if (string.IsNullOrEmpty(SceneSessionId)) SceneSessionId = await DataAnalysisManager.GetCurrentSessionIDAsync();
        return SceneSessionId;
    }
}

using System;
using System.Threading.Tasks;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager; // FishNet also has a SceneManager type

/// <summary>
/// Single place that moves the player between the (local-only) lobby scene and the game scenes.
///
/// Why this exists: FishNet only makes clients load a scene when the SERVER loads it through FishNet's own
/// SceneManager as a *global* scene. The lobby used to load game scenes with Unity's SceneManager, so a
/// remote joiner was never told to load the host's scene and JoinRoomPanel timed out waiting for it.
///
/// Rules:
///  - Core and Login stay plain Unity scenes (never networked). They are never replaced (ReplaceOption.None).
///  - Game scenes are loaded as FishNet global scenes when the server is running (always, in the sandbox too),
///    so every connected AND future client loads them automatically.
///  - If networking is unavailable, everything falls back to plain additive Unity loads.
/// </summary>
public static class NetworkSceneFlow
{
    public const string LobbyScene = "Login";
    private const float LoadTimeoutSeconds = 30f;

    private static bool _entering;
    private static bool _leaving;
    private static bool _watching;

    // ------------------------------------------------------------------ enter

    /// <summary>Loads a game scene additively and unloads the lobby once it is ready.</summary>
    public static async Task EnterGameSceneAsync(string sceneName, string sceneToUnload = LobbyScene)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (_entering || UnitySceneManager.GetSceneByName(sceneName).isLoaded) return;
        _entering = true;
        try
        {
            NetworkManager nm = InstanceFinder.NetworkManager;
            if (nm != null && nm.IsServerStarted && nm.SceneManager != null)
            {
                // Global scene: loaded on the server and on every current and future client.
                // ReplaceOption.None keeps Core/Login untouched (All would unload every scene, even unmanaged ones).
                var sld = new SceneLoadData(sceneName) { ReplaceScenes = ReplaceOption.None };
                nm.SceneManager.LoadGlobalScenes(sld);
                Debug.Log($"[SceneFlow] Loading '{sceneName}' as a FishNet global scene.");
            }
            else
            {
                UnitySceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                Debug.LogWarning($"[SceneFlow] No running FishNet server - loading '{sceneName}' as a plain Unity scene.");
            }

            float start = Time.realtimeSinceStartup;
            while (!UnitySceneManager.GetSceneByName(sceneName).isLoaded)
            {
                if (Time.realtimeSinceStartup - start > LoadTimeoutSeconds)
                {
                    Debug.LogError($"[SceneFlow] Timed out loading '{sceneName}'. Staying in the lobby.");
                    return;
                }
                await Task.Yield();
            }

            UnloadIfLoaded(sceneToUnload);
        }
        catch (Exception e) { Debug.LogException(e); }
        finally { _entering = false; }
    }

    // ------------------------------------------------------------------ leave

    /// <summary>
    /// Returns to the lobby from a game scene.
    /// Host: unloads the global scene for everybody.
    /// Joined client (or lost connection): drops the remote session, unloads the scene locally and
    /// brings the local sandbox (own host + avatar) back.
    /// </summary>
    public static async Task LeaveGameSceneAsync(string sceneName, string lobbyScene = LobbyScene)
    {
        if (string.IsNullOrEmpty(sceneName) || _leaving) return;
        _leaving = true;
        try
        {
            // 1. Bring the lobby back first so the player is never left without a UI.
            if (!UnitySceneManager.GetSceneByName(lobbyScene).isLoaded)
            {
                AsyncOperation op = UnitySceneManager.LoadSceneAsync(lobbyScene, LoadSceneMode.Additive);
                while (op != null && !op.isDone) await Task.Yield();
            }
            Scene lobby = UnitySceneManager.GetSceneByName(lobbyScene);
            if (lobby.isLoaded) UnitySceneManager.SetActiveScene(lobby);

            // 2. Unload the game scene.
            NetworkManager nm = InstanceFinder.NetworkManager;
            if (nm != null && nm.IsServerStarted && nm.SceneManager != null)
            {
                // Host: global unload - connected clients unload it as well.
                nm.SceneManager.UnloadGlobalScenes(new SceneUnloadData(sceneName));

                // Fallback if FishNet did not manage this scene (e.g. it was loaded without a server).
                float t = Time.realtimeSinceStartup;
                while (UnitySceneManager.GetSceneByName(sceneName).isLoaded && Time.realtimeSinceStartup - t < 5f)
                    await Task.Yield();
                UnloadIfLoaded(sceneName);
            }
            else
            {
                // Client of somebody else's room (or the connection is already gone).
                // Drop the connection and remove the scene first, then rebuild the sandbox.
                VRNetworkManager net = VRNetworkManager.Instance;
                if (net != null) net.StopSession();
                UnloadIfLoaded(sceneName); // FishNet may already have unloaded it when the client stopped
                if (net != null) await net.LeaveRemoteSessionAsync();
            }
        }
        catch (Exception e) { Debug.LogException(e); }
        finally { _leaving = false; }
    }

    // ------------------------------------------------------------------ watcher (joined clients)

    /// <summary>
    /// Call once a joined client is inside the host's scene. If the host unloads the scene or the connection
    /// drops, the player is sent back to the lobby instead of being stranded in the Core scene.
    /// </summary>
    public static async void WatchRemoteSession(string gameScene)
    {
        if (_watching || string.IsNullOrEmpty(gameScene)) return;
        _watching = true;
        try
        {
            VRNetworkManager net = VRNetworkManager.Instance;
            while (net != null && net.IsRemoteClient)
            {
                if (!UnitySceneManager.GetSceneByName(gameScene).isLoaded) break; // host unloaded the scene
                await Task.Delay(500);
            }

            bool lobbyLoaded = UnitySceneManager.GetSceneByName(LobbyScene).isLoaded;
            bool gameLoaded = UnitySceneManager.GetSceneByName(gameScene).isLoaded;
            if (!_leaving && (gameLoaded || !lobbyLoaded))
            {
                Debug.Log("[SceneFlow] The host closed the session or the connection was lost - returning to the lobby.");
                await LeaveGameSceneAsync(gameScene);
            }
        }
        catch (Exception e) { Debug.LogException(e); }
        finally { _watching = false; }
    }

    private static void UnloadIfLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        Scene s = UnitySceneManager.GetSceneByName(sceneName);
        if (s.isLoaded) UnitySceneManager.UnloadSceneAsync(s);
    }
}
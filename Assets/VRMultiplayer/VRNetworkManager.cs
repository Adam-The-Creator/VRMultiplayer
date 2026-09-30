using System;
using System.Threading.Tasks;
using Assets.Database.DatabaseManagement;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using Steamworks;
using UnityEngine;
using MultipassTransport = FishNet.Transporting.Multipass.Multipass;
using SteamTransport = FishySteamworks.FishySteamworks;
using TugboatTransport = FishNet.Transporting.Tugboat.Tugboat;
using YakTransport = FishNet.Transporting.Yak.Yak;

public class VRNetworkManager : MonoBehaviour
{
    public enum SessionTier { None, Steam, Tugboat, Offline }

    public static VRNetworkManager Instance { get; private set; }

    [Tooltip("Seconds to wait for the local client to authenticate on each transport before falling back.")]
    [SerializeField] private float tierTimeoutSeconds = 5f;

    [Tooltip("Seconds to wait for the Steam connection to a remote host before giving up.")]
    [SerializeField] private float joinTimeoutSeconds = 20f;

    public string serverAddress = "localhost";
    public SessionTier CurrentTier { get; private set; } = SessionTier.None;
    public event Action<SessionTier> OnSessionReady;

    /// <summary>True while a join to a remote room is in progress.</summary>
    public bool IsJoining { get; private set; }

    /// <summary>Human-readable reason of the last failed join (for the UI).</summary>
    public string LastJoinError { get; private set; }

    private bool _starting;
    private bool _quitting;

    // True when the local client is connected and authenticated (also true on the Offline/Yak tier,
    // so the RPC-based drawing path keeps working in single-player).
    public bool IsMultiplayerActive
    {
        get
        {
            NetworkManager nm = InstanceFinder.NetworkManager;
            return nm != null && nm.IsClientStarted
                   && nm.ClientManager.Connection != null
                   && nm.ClientManager.Connection.IsAuthenticated;
        }
    }

    /// <summary>True when this instance is only a client of somebody else's room (no local server).</summary>
    public bool IsRemoteClient
    {
        get
        {
            NetworkManager nm = InstanceFinder.NetworkManager;
            return nm != null && nm.IsClientStarted && !nm.IsServerStarted;
        }
    }

    public void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private async void Start()
    {
        // The DB is not needed for networking or the avatar, so never block on it.
        try
        {
            await DatabaseManager.Instance.StartDBServerIfNeeded();
            if (DatabaseManager.Instance.CurrentStatus != DatabaseManager.ServerStatus.Online)
                Debug.LogError("[Network] Database is not online - login/drawings won't work, session still starts.");
        }
        catch (Exception e) { Debug.LogException(e); }

        await StartSandboxSessionAsync();
    }

    // Kept for existing callers. Starts the sandbox in the background.
    public bool StartHostSession()
    {
        if (_starting || IsMultiplayerActive) return false;
        _ = StartSandboxSessionAsync();
        return true;
    }

    private async Task<bool> StartSandboxSessionAsync()
    {
        if (_starting) return false;
        _starting = true;
        try
        {
            NetworkManager nm = InstanceFinder.NetworkManager;
            if (nm == null) { Debug.LogError("[Network] No NetworkManager found."); return false; }
            if (!(nm.TransportManager.Transport is MultipassTransport))
            {
                Debug.LogError("[Network] TransportManager.Transport must be the Multipass component.");
                return false;
            }
            if (!await StartServerAsync(nm))
            {
                Debug.LogError("[Network] No server transport started.");
                return false;
            }

            foreach (SessionTier tier in new[] { SessionTier.Steam, SessionTier.Tugboat, SessionTier.Offline })
            {
                if (_quitting) return false;
                if (await TryClientTierAsync(nm, tier, null, tierTimeoutSeconds))
                {
                    CurrentTier = tier;
                    Debug.Log($"[Network] Session ready via {tier}. ClientId={nm.ClientManager.Connection.ClientId}");
                    OnSessionReady?.Invoke(tier);
                    return true;
                }
                Debug.LogWarning($"[Network] Tier '{tier}' failed - trying next.");
            }

            CurrentTier = SessionTier.None;
            Debug.LogError("[Network] All transports failed. Running without networking.");
            return false;
        }
        finally { _starting = false; }
    }

    private async Task<bool> StartServerAsync(NetworkManager nm)
    {
        if (nm.IsServerStarted) return true;

        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnServerState(ServerConnectionStateArgs a)
        {
            if (a.ConnectionState == LocalConnectionState.Started) started.TrySetResult(true);
        }

        nm.ServerManager.OnServerConnectionState += OnServerState;
        try
        {
            nm.ServerManager.StartConnection(); // Multipass starts every transport; Steam may fail, the others still start
            Task winner = await Task.WhenAny(started.Task, Task.Delay(TimeSpan.FromSeconds(3)));
            return winner == started.Task;
        }
        finally { nm.ServerManager.OnServerConnectionState -= OnServerState; }
    }

    // Starts the local client on one transport and waits until it is authenticated (or kicked/timed out).
    private async Task<bool> TryClientTierAsync(NetworkManager nm, SessionTier tier, string address, float timeout)
    {
        if (tier == SessionTier.Steam && !SteamManager.Initialized)
        {
            Debug.LogWarning("[Network] Steam not initialised - skipping Steam tier.");
            return false;
        }
        if (!SelectClientTransport(nm, tier)) return false;

        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnAuthenticated() => result.TrySetResult(true);
        void OnClientState(ClientConnectionStateArgs a)
        {
            if (a.ConnectionState == LocalConnectionState.Stopped) result.TrySetResult(false); // kicked or refused
        }

        nm.ClientManager.OnAuthenticated += OnAuthenticated;
        nm.ClientManager.OnClientConnectionState += OnClientState;
        try
        {
            bool requested = string.IsNullOrEmpty(address)
                ? nm.ClientManager.StartConnection()
                : nm.ClientManager.StartConnection(address);
            if (!requested) return false;

            Task winner = await Task.WhenAny(result.Task, Task.Delay(TimeSpan.FromSeconds(timeout)));
            bool ok = winner == result.Task && result.Task.Result;
            if (!ok)
            {
                Debug.LogWarning($"[Network] {tier} client failed " +
                                 $"({(winner == result.Task ? "refused or kicked" : $"timed out after {timeout}s")}) " +
                                 $"address='{address}'");
                nm.ClientManager.StopConnection();
                await Task.Delay(300); // let FishNet finish stopping before the next tier starts
            }
            return ok;
        }
        finally
        {
            nm.ClientManager.OnAuthenticated -= OnAuthenticated;
            nm.ClientManager.OnClientConnectionState -= OnClientState;
        }
    }

    private static bool SelectClientTransport(NetworkManager nm, SessionTier tier)
    {
        if (!(nm.TransportManager.Transport is MultipassTransport mp)) return false;
        switch (tier)
        {
            case SessionTier.Steam: mp.SetClientTransport<SteamTransport>(); return true;
            case SessionTier.Tugboat: mp.SetClientTransport<TugboatTransport>(); return true;
            case SessionTier.Offline: mp.SetClientTransport<YakTransport>(); return true;
            default: return false;
        }
    }

    public void StopSession()
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm == null) return;
        nm.ClientManager.StopConnection();
        nm.ServerManager.StopConnection(true);
        CurrentTier = SessionTier.None;
    }

    // ------------------------------------------------------------------ remote rooms

    /// <summary>Fire-and-forget variant kept for existing callers. Prefer <see cref="JoinRemoteSessionAsync"/>.</summary>
    public async void JoinRemoteSession(string address)
    {
        try { await JoinRemoteSessionAsync(address); }
        catch (Exception e) { Debug.LogException(e); }
    }

    /// <summary>
    /// Leaves the local sandbox and connects to a remote host (address = the host's SteamID64).
    /// Returns true once the client is authenticated. On failure the local sandbox is restarted and
    /// <see cref="LastJoinError"/> says why.
    /// </summary>
    public async Task<bool> JoinRemoteSessionAsync(string address)
    {
        if (IsJoining)
        {
            LastJoinError = "A join is already in progress.";
            return false;
        }

        IsJoining = true;
        LastJoinError = null;
        try
        {
            NetworkManager nm = InstanceFinder.NetworkManager;
            if (nm == null) { LastJoinError = "No network manager."; return false; }
            if (string.IsNullOrWhiteSpace(address)) { LastJoinError = "The room has no host address."; return false; }
            address = address.Trim();

            if (!SteamManager.Initialized)
            {
                LastJoinError = "Steam is not running.";
                Debug.LogError("[Network] Cannot join a Steam room: Steam is not initialised.");
                return false;
            }
            if (SteamUser.GetSteamID().m_SteamID.ToString() == address)
            {
                LastJoinError = "This room is hosted by your own Steam account.";
                Debug.LogWarning("[Network] That room is hosted by this Steam account - you can't join your own room. " +
                                 "Use a second machine and account.");
                return false;
            }

            Debug.Log($"[Network] Leaving local sandbox to join remote room at '{address}'...");
            StopSession();
            await Task.Delay(500); // let FishNet clean up local network objects

            serverAddress = address;
            if (await TryClientTierAsync(nm, SessionTier.Steam, address, joinTimeoutSeconds))
            {
                CurrentTier = SessionTier.Steam;
                Debug.Log($"[Network] Joined remote host at: {address}");
                OnSessionReady?.Invoke(CurrentTier);
                return true;
            }

            LastJoinError = "Could not connect to the host. Is the host online with Steam running?";
            Debug.LogError($"[Network] Could not join the room at '{address}' - returning to local sandbox.");
            await StartSandboxSessionAsync();
            return false;
        }
        finally { IsJoining = false; }
    }

    /// <summary>Drops the remote connection and goes back to the local sandbox (own host + avatar).</summary>
    public async Task LeaveRemoteSessionAsync()
    {
        if (_quitting) return;
        Debug.Log("[Network] Leaving remote session - returning to local sandbox.");
        StopSession();
        await Task.Delay(500);
        await StartSandboxSessionAsync();
    }

    // Stop networking before Unity destroys SteamManager, which fixes the
    // "Steamworks is not initialized" exception at quit.
    private void OnApplicationQuit()
    {
        _quitting = true;
        try
        {
            NetworkManager nm = InstanceFinder.NetworkManager;
            if (nm != null)
            {
                nm.ServerManager.StopConnection(true);
                nm.ClientManager.StopConnection();
            }
            CurrentTier = SessionTier.None;
        }
        catch (Exception e) { Debug.LogException(e); }
    }
}
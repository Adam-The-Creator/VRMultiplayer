using FishNet;
using UnityEngine;
using Assets.Database.DatabaseManagement;
using System.Threading.Tasks;

public class VRNetworkManager : MonoBehaviour
{
    public static VRNetworkManager Instance { get; private set; }

    [Tooltip("The IP address to connect to. Use 'localhost' if running on the same machine.")]
    public string serverAddress = "localhost";
    public bool IsMultiplayerActive =>
        InstanceFinder.NetworkManager != null &&
        (InstanceFinder.ServerManager.Started || InstanceFinder.ClientManager.Started);

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    async void Start()
    {
        // 1. Wait for the database server to start and verify it's online
        await DatabaseManager.Instance.StartDBServerIfNeeded();

        if (DatabaseManager.Instance.CurrentStatus != DatabaseManager.ServerStatus.Online)
        {
            UnityEngine.Debug.LogError("[Network] Aborting network start: Database is not online.");
            return;
        }

        if (InstanceFinder.ServerManager == null || InstanceFinder.ClientManager == null)
        {
            UnityEngine.Debug.LogError("FishNet ServerManager or ClientManager is not initialized. Please ensure FishNet is set up correctly.");
            return; // Halt further execution if FishNet is missing
        }

        // Everyone starts as a local Host in their own sandbox immediately
        StartHostSession();
    }

    public bool StartHostSession()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[Network] Steam unavailable - staying offline (single-player sandbox).");
            return false;
        }
        var nm = InstanceFinder.NetworkManager;
        if (nm == null) return false;

        bool server = nm.ServerManager.StartConnection();
        bool client = nm.ClientManager.StartConnection();
        if (!server || !client)
        {
            Debug.LogError("[Network] Host start failed - rolling back.");
            nm.ServerManager.StopConnection(true);
            nm.ClientManager.StopConnection();
            return false;
        }
        Debug.Log("[Network] Sandbox Host session started.");
        return true;
    }

    // Called when the player joins a Room Code
    public async void JoinRemoteSession(string address)
    {
        Debug.Log("[Network] Leaving local sandbox to join remote room...");

        // 1. Stop local sandbox
        InstanceFinder.ServerManager.StopConnection(true);
        InstanceFinder.ClientManager.StopConnection();

        // Wait a brief moment for FishNet to clean up local network objects
        await Task.Delay(500);

        // 2. Connect to the remote Host
        serverAddress = address;
        InstanceFinder.ClientManager.StartConnection(serverAddress);
        Debug.Log($"[Network] Connecting to remote host at: {serverAddress}");
    }
}
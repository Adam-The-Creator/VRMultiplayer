using FishNet;
using UnityEngine;
using Assets.Database.DatabaseManagement;

public class VRNetworkManager : MonoBehaviour
{
    public static VRNetworkManager Instance { get; private set; }

    [Tooltip("The IP address to connect to. Use 'localhost' if running on the same machine.")]
    public string serverAddress = "localhost";

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

        // Ensure that the ServerManager and ClientManager are initialized
        if (InstanceFinder.ServerManager == null || InstanceFinder.ClientManager == null)
        {
            UnityEngine.Debug.LogError("FishNet ServerManager or ClientManager is not initialized. Please ensure FishNet is set up correctly.");
            return; // Halt further execution if FishNet is missing
        }
    }

    public void StartHostSession()
    {
        if (InstanceFinder.ServerManager != null && InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
            Debug.Log("[Network] Host session started.");
        }
    }

    public void JoinSession(string address = null)
    {
        if (InstanceFinder.ClientManager != null)
        {
            if (!string.IsNullOrEmpty(address)) serverAddress = address;

            InstanceFinder.ClientManager.StartConnection(serverAddress);
            Debug.Log($"[Network] Client connecting to {serverAddress}...");
        }
    }
}
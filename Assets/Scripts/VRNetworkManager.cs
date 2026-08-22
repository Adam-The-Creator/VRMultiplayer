using FishNet;
using UnityEngine;
using Assets.Database.DatabaseManagement;

public class VRNetworkManager : MonoBehaviour
{
    [Tooltip("The IP address to connect to. Use 'localhost' if running on the same machine.")]
    public string serverAddress = "localhost";

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

        // TODO: Implement a UI for the user to choose between hosting and/or joining a session.
        //       Create roomcode (and optionally password/pincode) input field(s) for joining a session.
        StartHostSession();
        JoinSession();
    }

    public void StartHostSession()
    {
        // TODO: Host a Session with a Roomcode (and optionally password/pincode) for others to join.


        // Start the Server and the Client locally (Hosting)
        if (InstanceFinder.ServerManager != null && InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();

            gameObject.SetActive(false);
        }
    }

    public void JoinSession()
    {
        // TODO: Join a Session with a Roomcode (and optionally password/pincode) provided by the user.

        // Start the Client only, connecting to the specified IP address
        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.StartConnection(serverAddress);

            gameObject.SetActive(false);
        }
    }
}
using FishNet;
using FishNet.Object;
using UnityEngine;

public class SessionSpawner : NetworkBehaviour
{
    [Tooltip("Drawing prefab")]
    public GameObject drawingPrefab;

    private GameObject spawnedDrawing;

    private void Start()
    {
        // If a Drawing object is already active in the scene, abort to prevent duplicates
        if (FindFirstObjectByType<VRDrawing>() != null) return;

        if (VRNetworkManager.Instance.IsMultiplayerActive)
        {
            // Online/MultiplayerPlayer mode
            // Only the server/host should spawn networked objects
            if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started)
            {
                if (drawingPrefab != null)
                {
                    spawnedDrawing = Instantiate(drawingPrefab, Vector3.zero, Quaternion.identity);
                    InstanceFinder.ServerManager.Spawn(spawnedDrawing);
                    Debug.Log("[SessionSpawner] Spawned authoritative network Drawing object.");
                }
            }
        }
        else
        {
            // Offline/SinglePlayer mode
            if (drawingPrefab != null)
            {
                spawnedDrawing = Instantiate(drawingPrefab, Vector3.zero, Quaternion.identity);
                Debug.Log("[SessionSpawner] Spawned local SinglePlayer Drawing object.");
            }
        }
    }

    private void OnDestroy()
    {
        if (spawnedDrawing != null)
        {
            // Clean up network instance if server is initialized
            if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started)
            {
                InstanceFinder.ServerManager.Despawn(spawnedDrawing, DespawnType.Destroy);
            }
            else
            {
                // Local cleanup
                Destroy(spawnedDrawing);
            }
        }
    }
}
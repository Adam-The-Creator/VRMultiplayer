using FishNet.Object;
using UnityEngine;

public class SessionSpawner : NetworkBehaviour
{
    [Tooltip("Drawing prefab")]
    public GameObject drawingPrefab;

    private GameObject spawnedDrawing;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (drawingPrefab != null)
        {
            spawnedDrawing = Instantiate(drawingPrefab, Vector3.zero, Quaternion.identity);
            ServerManager.Spawn(spawnedDrawing);
            Debug.Log("[Network] Authoritative Drawing object spawned strictly at Origin (0,0,0).");
        }
    }

    private void OnDestroy()
    {
        if (IsServerInitialized && spawnedDrawing != null)
        {
            ServerManager.Despawn(spawnedDrawing, DespawnType.Destroy);
            Debug.Log("[Network] Authoritative Drawing object destroyed.");
        }
    }
}
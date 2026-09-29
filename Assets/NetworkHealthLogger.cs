using System.Collections;
using FishNet;
using FishNet.Managing;
using UnityEngine;

public class NetworkHealthLogger : MonoBehaviour
{
    private IEnumerator Start()
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm == null) { Debug.LogError("[NetHealth] No NetworkManager found!"); yield break; }

        nm.ServerManager.OnServerConnectionState += a => Debug.Log($"[NetHealth] Server: {a.ConnectionState}");
        nm.ClientManager.OnClientConnectionState += a => Debug.Log($"[NetHealth] Client: {a.ConnectionState}");
        nm.ServerManager.OnRemoteConnectionState += (conn, a) => Debug.Log($"[NetHealth] Remote client {conn.ClientId}: {a.ConnectionState}");
        nm.ClientManager.OnAuthenticated += () =>
            Debug.Log($"[NetHealth] Client authenticated, id={nm.ClientManager.Connection.ClientId}");

        yield return new WaitForSeconds(8f);
        var c = nm.ClientManager.Connection;
        Debug.Log($"[NetHealth] server={nm.IsServerStarted} client={nm.IsClientStarted} " +
                  $"authenticated={c.IsAuthenticated} ownedObjects={c.Objects.Count} " +
                  $"spawned={nm.ServerManager.Objects.Spawned.Count}");

        foreach (var kv in nm.ServerManager.Objects.Spawned)
            Debug.Log($"[NetHealth] spawned: {kv.Value.name} scene={kv.Value.gameObject.scene.name} owner={kv.Value.OwnerId}");
    }
}
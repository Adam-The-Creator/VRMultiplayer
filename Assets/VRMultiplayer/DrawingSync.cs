using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Network relay for the drawing. It carries strokes between the players and nothing else; the data and the
/// visuals live in <see cref="VRDrawing"/> (a plain object in Core).
///
/// This object is a PREFAB that the server spawns into the game scene once that scene is loaded
/// (see VRNetworkManager.SpawnDrawingSync). Because the scene is loaded as a FishNet global scene, every
/// client - the host's own client and joiners - is "in" that scene and therefore observes this object.
///
/// Flow: local player draws -> VRDrawing applies it locally -> SendXxx -> server -> RpcXxx to everybody ->
/// every client except the sender applies it through VRDrawing.ApplyRemoteXxx.
/// </summary>
public class DrawingSync : NetworkBehaviour
{
    private static DrawingSync _live;
    private VRDrawing _drawing;

    /// <summary>True when the relay exists and this machine is connected to it.</summary>
    public static bool TryGetLive(out DrawingSync sync)
    {
        sync = _live;
        return sync != null && sync.IsSpawned && sync.IsClientInitialized;
    }

    public static bool ExistsInScene(Scene scene)
    {
        foreach (DrawingSync s in FindObjectsOfType<DrawingSync>())
        {
            if (s.gameObject.scene == scene) return true;
        }
        return false;
    }

    // Lifecycle logs: they show whether the relay really went live on this machine.
    public override void OnStartServer() =>
        Debug.Log($"[DrawingSync] OnStartServer (scene='{gameObject.scene.name}')");

    public override void OnStopServer() =>
        Debug.Log("[DrawingSync] OnStopServer");

    public override void OnStartClient()
    {
        _live = this;
        Debug.Log($"[DrawingSync] OnStartClient (host={IsServerInitialized}, clientId={LocalConnection.ClientId}, " +
                  $"scene='{gameObject.scene.name}')");
    }

    public override void OnStopClient()
    {
        if (_live == this) _live = null;
        Debug.Log("[DrawingSync] OnStopClient");
    }

    private VRDrawing Drawing
    {
        get
        {
            if (_drawing == null) _drawing = FindObjectOfType<VRDrawing>();
            return _drawing;
        }
    }

    // ------------------------------------------------------------------ outgoing (called by VRDrawing)

    public void SendNewLine(Line line) => CmdNewLine(line);
    public void SendPoint(string lineId, Point point) => CmdPoint(lineId, point);
    public void SendErase(string lineId, string playerId, string timestamp, Hand hand) =>
        CmdErase(lineId, playerId, timestamp, hand);

    // ------------------------------------------------------------------ line creation

    [ServerRpc(RequireOwnership = false)]
    private void CmdNewLine(Line line, NetworkConnection sender = null)
    {
        RpcNewLine(line, sender != null ? sender.ClientId : -1);
    }

    [ObserversRpc]
    private void RpcNewLine(Line line, int senderId)
    {
        if (senderId == LocalConnection.ClientId) return; // the sender already has it
        if (Drawing != null) Drawing.ApplyRemoteNewLine(line);
    }

    // ------------------------------------------------------------------ points

    [ServerRpc(RequireOwnership = false)]
    private void CmdPoint(string lineId, Point point, NetworkConnection sender = null)
    {
        RpcPoint(lineId, point, sender != null ? sender.ClientId : -1);
    }

    [ObserversRpc]
    private void RpcPoint(string lineId, Point point, int senderId)
    {
        if (senderId == LocalConnection.ClientId) return;
        if (Drawing != null) Drawing.ApplyRemotePoint(lineId, point);
    }

    // ------------------------------------------------------------------ erasure

    [ServerRpc(RequireOwnership = false)]
    private void CmdErase(string lineId, string playerId, string timestamp, Hand hand, NetworkConnection sender = null)
    {
        RpcErase(lineId, playerId, timestamp, hand, sender != null ? sender.ClientId : -1);
    }

    [ObserversRpc]
    private void RpcErase(string lineId, string playerId, string timestamp, Hand hand, int senderId)
    {
        if (senderId == LocalConnection.ClientId) return;
        if (Drawing != null) Drawing.ApplyRemoteErase(lineId, playerId, timestamp, hand);
    }
}
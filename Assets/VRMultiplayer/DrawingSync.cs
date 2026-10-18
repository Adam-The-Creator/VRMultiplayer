using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Network relay for the drawing. It carries strokes and the behaviour data (tracked poses, tool events) between
/// the players and nothing else; the data and the visuals live in <see cref="VRDrawing"/> (a plain object in Core).
///
/// This object is a PREFAB that the server spawns into the game scene once that scene is loaded
/// (see VRNetworkManager.SpawnDrawingSync). Because the scene is loaded as a FishNet global scene, every
/// client - the host's own client and joiners - is "in" that scene and therefore observes this object.
///
/// Flow: local player draws -> VRDrawing applies it locally -> SendXxx -> server -> RpcXxx to everybody ->
/// every client except the sender applies it through VRDrawing.ApplyRemoteXxx.
///
/// Strokes are small and travel as ordinary RPCs. Bulk data (batches of behaviour data, the snapshot) is encoded by
/// <see cref="DrawingWire"/> into a blob that is cut into small fragments, one RPC each, and put together again by
/// the receiver. RPCs of several KB are split by FishNet, and the clients then reported misparsed packets, so
/// nothing sent here is bigger than a fragment.
///
/// Observer RPCs are not buffered, so a player who joins later gets a snapshot from the host instead: the joiner
/// asks for it when the relay starts, the host streams the strokes and the behaviour data over a few frames (a
/// burst of everything would overflow the send buffer of the connection). The joiner holds back what arrives live
/// until the snapshot is complete, so that it is applied after, and never before, the older state.
/// </summary>
public class DrawingSync : NetworkBehaviour
{
    // The kinds of blob.
    private const byte BlobTelemetry = 1; // DrawingWire.EncodeTelemetry
    private const byte BlobLines = 2;     // DrawingWire.EncodeLines

    // Payload of one fragment. A fragment RPC is this plus ~30 bytes, far below the transport MTU (~1200 bytes).
    private const int FragmentBytes = 800;

    // Size of one snapshot blob (a point is ~50 bytes, a sample of all four channels ~150 bytes).
    private const int SnapshotPointsPerChunk = 2000;
    private const int SnapshotSamplesPerChunk = 300;

    // The host sends this many fragments per frame (~4 KB, a few hundred KB/s).
    private const int SnapshotFragmentsPerFrame = 5;

    // The joiner gives up on the snapshot when nothing of it arrived for this long, and goes on with live data.
    private const float SnapshotStallSeconds = 10f;

    // Limits for what is received. Live data held back during a snapshot is capped as well.
    private const int MaxFragmentsPerTransfer = 4096;
    private const int MaxTransferBytes = 4 * 1024 * 1024;
    private const int MaxDeferredUpdates = 50000;
    private const float StaleTransferSeconds = 60f;

    // Sender of everything that comes from the host as a snapshot (live senders are client ids, 0 or more).
    private const int SnapshotSender = -1;

    private static DrawingSync _live;
    private VRDrawing _drawing;

    private ushort _nextTransferId;
    private readonly Dictionary<long, Transfer> _incoming = new();
    private float _nextPruneTime;

    private bool _awaitingSnapshot;
    private float _snapshotDeadline;
    private int _snapshotBlobs;
    private int _snapshotBytes;
    private readonly List<Action> _deferred = new();

    // A blob that is being received.
    private sealed class Transfer
    {
        public byte Kind;
        public bool Live;
        public byte[][] Parts;
        public int Received;
        public int Bytes;
        public float LastTime;
    }

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

        _incoming.Clear();
        _deferred.Clear();

        // The host's own client has seen everything. A joiner only knows the saved drawing, so it asks the host.
        if (!IsServerInitialized)
        {
            _awaitingSnapshot = true;
            _snapshotDeadline = Time.unscaledTime + SnapshotStallSeconds;
            _snapshotBlobs = 0;
            _snapshotBytes = 0;
            CmdRequestSnapshot();
        }
    }

    public override void OnStopClient()
    {
        if (_live == this) _live = null;
        _awaitingSnapshot = false;
        _incoming.Clear();
        _deferred.Clear();
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

    private void Update()
    {
        if (!IsClientInitialized) return;

        float now = Time.unscaledTime;

        if (_awaitingSnapshot && now > _snapshotDeadline)
        {
            Debug.LogWarning("[DrawingSync] The drawing snapshot did not arrive completely; continuing with live data only.");
            FinishSnapshot();
        }

        // A sender that left in the middle of a blob leaves a partial transfer behind.
        if (_incoming.Count > 0 && now >= _nextPruneTime)
        {
            _nextPruneTime = now + StaleTransferSeconds / 4f;

            List<long> stale = null;
            foreach (KeyValuePair<long, Transfer> pair in _incoming)
            {
                if (now - pair.Value.LastTime > StaleTransferSeconds) (stale ??= new List<long>()).Add(pair.Key);
            }
            if (stale != null)
            {
                foreach (long key in stale) _incoming.Remove(key);
            }
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
        HandleNewLine(line);
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
        HandlePoint(lineId, point);
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
        HandleErase(lineId, playerId, timestamp, hand);
    }

    // ------------------------------------------------------------------ behaviour data (tracked poses, tool events)

    public void SendTelemetry(TrackedBehavior poses, List<DrawingEvent> events) =>
        SendBlob(BlobTelemetry, DrawingWire.EncodeTelemetry(poses, events));

    // ------------------------------------------------------------------ blobs (cut into fragments)

    // Client -> server: sends a blob of this machine's own to everybody else.
    private void SendBlob(byte kind, byte[] blob)
    {
        int count = FragmentCount(blob.Length);
        if (count > MaxFragmentsPerTransfer)
        {
            Debug.LogError($"[DrawingSync] A blob of {blob.Length} bytes is too big to be relayed.");
            return;
        }

        ushort id = _nextTransferId++;
        for (int i = 0; i < count; i++) CmdBlob(kind, id, (ushort)i, (ushort)count, Fragment(blob, i));
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdBlob(byte kind, ushort transferId, ushort index, ushort count, byte[] data, NetworkConnection sender = null)
    {
        RpcBlob(kind, transferId, index, count, data, sender != null ? sender.ClientId : -1);
    }

    [ObserversRpc]
    private void RpcBlob(byte kind, ushort transferId, ushort index, ushort count, byte[] data, int senderId)
    {
        if (senderId == LocalConnection.ClientId) return;
        ReceiveFragment(senderId, kind, transferId, index, count, data, live: true);
    }

    private static int FragmentCount(int length) => Math.Max(1, (length + FragmentBytes - 1) / FragmentBytes);

    private static byte[] Fragment(byte[] blob, int index)
    {
        int offset = index * FragmentBytes;
        byte[] part = new byte[Math.Max(0, Math.Min(FragmentBytes, blob.Length - offset))];
        Buffer.BlockCopy(blob, offset, part, 0, part.Length);
        return part;
    }

    private void ReceiveFragment(int senderId, byte kind, ushort transferId, ushort index, ushort count, byte[] data, bool live)
    {
        if (data == null || count == 0 || count > MaxFragmentsPerTransfer || index >= count) return;

        long key = ((long)(senderId + 1) << 32) | ((long)kind << 16) | transferId;
        if (!_incoming.TryGetValue(key, out Transfer transfer))
        {
            transfer = new Transfer { Kind = kind, Live = live, Parts = new byte[count][] };
            _incoming[key] = transfer;
        }

        // Not what the transfer started with, or a fragment that was already received.
        if (transfer.Parts.Length != count || transfer.Parts[index] != null) return;

        transfer.Bytes += data.Length;
        if (transfer.Bytes > MaxTransferBytes)
        {
            _incoming.Remove(key);
            return;
        }

        transfer.Parts[index] = data;
        transfer.LastTime = Time.unscaledTime;
        if (++transfer.Received < count) return;

        _incoming.Remove(key);

        byte[] blob = new byte[transfer.Bytes];
        int position = 0;
        foreach (byte[] part in transfer.Parts)
        {
            Buffer.BlockCopy(part, 0, blob, position, part.Length);
            position += part.Length;
        }

        Deliver(transfer.Kind, transfer.Live, blob);
    }

    // A blob is complete: decode it and hand the data to the drawing.
    private void Deliver(byte kind, bool live, byte[] blob)
    {
        if (!live)
        {
            _snapshotBlobs++;
            _snapshotBytes += blob.Length;
        }

        if (kind == BlobTelemetry)
        {
            if (!DrawingWire.TryDecodeTelemetry(blob, out TrackedBehavior poses, out List<DrawingEvent> events)) return;

            // A snapshot part is the state to start from; only live data waits for the snapshot.
            if (live)
            {
                HandleTelemetry(poses, events);
            }
            else if (Drawing != null)
            {
                Drawing.ApplyRemoteTelemetry(poses, events);
            }
        }
        else if (kind == BlobLines)
        {
            if (!DrawingWire.TryDecodeLines(blob, out List<Line> lines)) return;

            if (Drawing != null) Drawing.ApplyRemoteSnapshotLines(lines);
        }
        else
        {
            Debug.LogWarning($"[DrawingSync] Received a blob of unknown kind {kind}.");
        }
    }

    // ------------------------------------------------------------------ live data while the snapshot is on its way

    // What arrives live while the snapshot is still being received is applied afterwards: the snapshot is the older
    // state, and strokes it has not delivered yet would otherwise miss these points. Each Handle... method defers
    // itself while the snapshot is awaited and runs again once it is complete. (The RPC methods only call these:
    // FishNet rewrites the bodies of RPC methods, so they are kept free of lambdas.)
    private void HandleNewLine(Line line)
    {
        if (_awaitingSnapshot)
        {
            Defer(() => HandleNewLine(line));
            return;
        }

        if (Drawing != null) Drawing.ApplyRemoteNewLine(line);
    }

    private void HandlePoint(string lineId, Point point)
    {
        if (_awaitingSnapshot)
        {
            Defer(() => HandlePoint(lineId, point));
            return;
        }

        if (Drawing != null) Drawing.ApplyRemotePoint(lineId, point);
    }

    private void HandleErase(string lineId, string playerId, string timestamp, Hand hand)
    {
        if (_awaitingSnapshot)
        {
            Defer(() => HandleErase(lineId, playerId, timestamp, hand));
            return;
        }

        if (Drawing != null) Drawing.ApplyRemoteErase(lineId, playerId, timestamp, hand);
    }

    private void HandleTelemetry(TrackedBehavior poses, List<DrawingEvent> events)
    {
        if (_awaitingSnapshot)
        {
            Defer(() => HandleTelemetry(poses, events));
            return;
        }

        if (Drawing != null) Drawing.ApplyRemoteTelemetry(poses, events);
    }

    private void Defer(Action apply)
    {
        if (_deferred.Count < MaxDeferredUpdates) _deferred.Add(apply);
    }

    private void FinishSnapshot()
    {
        _awaitingSnapshot = false;
        if (_deferred.Count == 0) return;

        List<Action> queued = new(_deferred);
        _deferred.Clear();
        foreach (Action apply in queued)
        {
            try
            {
                apply();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    // ------------------------------------------------------------------ snapshot for late joiners

    // Runs on the host (the server is always a host in this game, so it has the drawing).
    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestSnapshot(NetworkConnection sender = null)
    {
        if (sender == null) return;
        BeginSnapshot(sender);
    }

    private void BeginSnapshot(NetworkConnection conn) => StartCoroutine(StreamSnapshot(conn));

    private IEnumerator StreamSnapshot(NetworkConnection conn)
    {
        // The begin and end messages always go out, so that the joiner stops waiting even if there is nothing to send.
        TargetSnapshotBegin(conn);

        // What is sent: the strokes, the tracked poses, then the events of all players. The poses and events are
        // copies of the moment of the request; the strokes are encoded when their turn comes.
        List<(byte kind, Func<byte[]> encode)> parts = new();

        VRDrawing drawing = Drawing;
        if (drawing != null)
        {
            foreach (List<Line> lines in drawing.BuildLineChunks(SnapshotPointsPerChunk))
                parts.Add((BlobLines, () => DrawingWire.EncodeLines(lines)));

            foreach (TrackedBehavior poses in drawing.BuildPoseChunks(SnapshotSamplesPerChunk))
                parts.Add((BlobTelemetry, () => DrawingWire.EncodeTelemetry(poses, null)));

            List<DrawingEvent> events = drawing.GetEvents();
            parts.Add((BlobTelemetry, () => DrawingWire.EncodeTelemetry(null, events)));
        }

        int sentBytes = 0;
        int sentInFrame = 0;

        foreach ((byte kind, Func<byte[]> encode) part in parts)
        {
            byte[] blob = part.encode();
            int count = FragmentCount(blob.Length);
            ushort id = _nextTransferId++;
            sentBytes += blob.Length;

            for (int i = 0; i < count; i++)
            {
                if (!conn.IsActive) yield break; // the joiner left meanwhile

                TargetBlob(conn, part.kind, id, (ushort)i, (ushort)count, Fragment(blob, i));

                if (++sentInFrame >= SnapshotFragmentsPerFrame)
                {
                    sentInFrame = 0;
                    yield return null;
                }
            }
        }

        if (!conn.IsActive) yield break;

        TargetSnapshotEnd(conn);
        Debug.Log($"[DrawingSync] Sent the drawing snapshot to client {conn.ClientId} ({parts.Count} parts, {sentBytes} bytes).");
    }

    [TargetRpc]
    private void TargetSnapshotBegin(NetworkConnection conn)
    {
        _snapshotDeadline = Time.unscaledTime + SnapshotStallSeconds;
    }

    [TargetRpc]
    private void TargetBlob(NetworkConnection conn, byte kind, ushort transferId, ushort index, ushort count, byte[] data)
    {
        _snapshotDeadline = Time.unscaledTime + SnapshotStallSeconds;
        ReceiveFragment(SnapshotSender, kind, transferId, index, count, data, live: false);
    }

    [TargetRpc]
    private void TargetSnapshotEnd(NetworkConnection conn)
    {
        Debug.Log($"[DrawingSync] Received the drawing snapshot ({_snapshotBlobs} parts, {_snapshotBytes} bytes, " +
                  $"{_deferred.Count} live updates were held back meanwhile).");
        FinishSnapshot();
    }
}

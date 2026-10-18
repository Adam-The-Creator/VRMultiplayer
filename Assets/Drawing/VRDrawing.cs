using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;
using UnityEngine;

// UnityEngine has a Pose as well.
using PoseData = Assets.Database.DatabaseManagement.MongoDB.Pose;

/// <summary>
/// The drawing of THIS machine: its data (Drawing) and the line objects of remote players.
///
/// This is deliberately a plain MonoBehaviour that lives in Core and is always active. It used to be a
/// NetworkBehaviour scene object, but FishNet keeps scene objects disabled for clients that are not
/// "in" the object's scene, so a joining client never had a usable Drawing (no strokes, NullReference when
/// loading the saved drawing).
///
/// Local-first: every local action is applied here immediately (the same as offline single-player) and is then
/// relayed to the other players through <see cref="DrawingSync"/> when a network session is live.
/// Actions that arrive from other players are applied with the ApplyRemote... methods.
///
/// Besides the strokes this holds the behaviour data of the session (tracked poses and tool events). Every machine
/// holds the data of all players, the same as the strokes, so a drawing can be saved from any of them.
/// </summary>
public class VRDrawing : MonoBehaviour
{
    [SerializeField] private Material defaultLineMaterial;
    public Drawing drawing;

    // Strokes of other players only get an erase collider once they stop receiving points: the relay has no
    // "end of stroke" message, and re-baking after every point would be quadratic. Local strokes get their
    // collider from the Brush, saved ones when they are loaded.
    private const float ColliderIdleSeconds = 0.5f;
    private readonly Dictionary<string, float> _pendingColliders = new(); // lineId -> time of the last remote point
    private readonly List<string> _readyColliders = new();

    // The behaviour data of the LOCAL player that the others have not received yet. The same objects are also in
    // drawing.trackedBehaviors / drawing.events. Capped so a session that never goes live does not grow it forever.
    private const int MaxUnsentSamples = 3000;
    private const int MaxUnsentEvents = 1000;
    private TrackedBehavior _unsentPoses;
    private readonly List<DrawingEvent> _unsentEvents = new();

    // What drawing.trackedBehaviors / drawing.events already contain, so that a sample is never added twice: a
    // snapshot or the database copy can hold what already arrived through the relay.
    private readonly Dictionary<string, HashSet<long>> _seenPoseTimes = new(); // playerId -> utcMs of the samples
    private readonly HashSet<string> _seenEvents = new();
    private bool _eventsIndexed;

    private Material _fallbackMaterial;

    private Material LineMaterial
    {
        get
        {
            if (defaultLineMaterial != null) return defaultLineMaterial;
            // One material for all strokes: LineRenderer.material would make a copy per stroke that is never destroyed.
            return _fallbackMaterial ??= new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        }
    }

    private void Awake()
    {
        drawing ??= new Drawing();

        // Records the behaviour of the local player (poses). It lives next to the drawing it fills.
        if (!TryGetComponent<BehaviorTracker>(out _)) gameObject.AddComponent<BehaviorTracker>();
    }

    private void Update()
    {
        if (_pendingColliders.Count == 0) return;

        float now = Time.unscaledTime;
        _readyColliders.Clear();
        foreach (KeyValuePair<string, float> pending in _pendingColliders)
        {
            if (now - pending.Value >= ColliderIdleSeconds) _readyColliders.Add(pending.Key);
        }

        foreach (string lineId in _readyColliders)
        {
            _pendingColliders.Remove(lineId);
            BakeCollider(lineId);
        }
    }

    /// <summary>
    /// Removes everything of the drawing (objects and data, including the behaviour data). Called when the player
    /// leaves a game scene, so the strokes of a finished session never stay visible in the lobby and the data of
    /// one session is never saved into the next drawing.
    /// </summary>
    public void ClearAll()
    {
        _pendingColliders.Clear();
        foreach (Transform child in transform) DestroyLine(child.gameObject);

        drawing = new Drawing();
        ResetBehaviorBookkeeping();
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>The current time as UTC Unix milliseconds, the time base of everything the drawing records.</summary>
    public static long UtcNowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // A baked collider mesh is not destroyed together with its GameObject, so it is destroyed explicitly.
    private static void DestroyLine(GameObject lineObject)
    {
        if (lineObject.TryGetComponent<MeshCollider>(out var meshCollider) && meshCollider.sharedMesh != null)
            Destroy(meshCollider.sharedMesh);
        Destroy(lineObject);
    }

    private Line FindLine(string lineId)
    {
        if (drawing?.lines == null || string.IsNullOrEmpty(lineId)) return null;
        // The line being drawn is almost always the newest one, so search from the end.
        for (int i = drawing.lines.Count - 1; i >= 0; --i)
        {
            if (drawing.lines[i].id == lineId) return drawing.lines[i];
        }
        return null;
    }

    private void BakeCollider(string lineId)
    {
        Line line = FindLine(lineId);
        if (line == null) return;

        BakeCollider(line, transform.Find(lineId));
    }

    // Gives a stroke a MeshCollider (the same way the Brush does for local strokes) so the eraser can hit it.
    private void BakeCollider(Line line, Transform lineObj)
    {
        if (line == null || line.status == Status.ERASED) return;
        if (lineObj == null || !lineObj.TryGetComponent<LineRenderer>(out var lr) || lr.positionCount < 2) return;

        // Rebuild when the other player kept drawing after an earlier bake. An existing collider is never
        // re-enabled here, so an erased stroke stays erased.
        if (lineObj.TryGetComponent<MeshCollider>(out var meshCollider))
        {
            if (meshCollider.sharedMesh != null) Destroy(meshCollider.sharedMesh);
        }
        else
        {
            meshCollider = lineObj.gameObject.AddComponent<MeshCollider>();
        }

        Mesh bakedMesh = new();
        lr.BakeMesh(bakedMesh, true);
        meshCollider.sharedMesh = bakedMesh;
    }

    // Makes the scene object of a stroke match its data: it is created when it is missing, points that were added
    // meanwhile are taken over and the erased state is applied. For strokes that came from outside (relay, snapshot,
    // database). mayExist = false skips the lookup when it is known that no object exists yet.
    private void SyncLineObject(Line line, bool mayExist, bool bakeNow)
    {
        bool erased = line.status == Status.ERASED;
        bool changed = false;

        Transform lineObj = mayExist ? transform.Find(line.id) : null;
        LineRenderer lr = null;

        if (lineObj == null)
        {
            GameObject lineObject = new GameObject(line.id);
            lineObject.transform.SetParent(transform);
            lineObject.tag = "Line";

            lr = lineObject.AddComponent<LineRenderer>();
            lr.sharedMaterial = LineMaterial;
            lr.useWorldSpace = true;
            lr.startWidth = line.startWidth;
            lr.endWidth = line.endWidth;
            lr.startColor = line.startColor != null ? line.startColor.ToColor() : Color.black;
            lr.endColor = line.endColor != null ? line.endColor.ToColor() : Color.black;

            lineObj = lineObject.transform;
            changed = true;
        }
        else if (!lineObj.TryGetComponent(out lr)) return;

        int count = line.points.Count;
        if (lr.positionCount != count)
        {
            Vector3[] positions = new Vector3[count];
            for (int i = 0; i < count; i++) positions[i] = line.points[i].ToVector3();
            lr.positionCount = count;
            lr.SetPositions(positions);
            changed = true;
        }

        lr.enabled = !erased;

        if (erased)
        {
            _pendingColliders.Remove(line.id);
            if (lineObj.TryGetComponent<Collider>(out var col)) col.enabled = false;
        }
        else if (changed)
        {
            if (bakeNow) BakeCollider(line, lineObj);
            else _pendingColliders[line.id] = Time.unscaledTime;
        }
    }

    // Merges a stroke that did not originate from this machine's brush into what is already known: new strokes
    // are added, a known one takes over the longer point list (a snapshot is a later state of the same stroke)
    // and an erasure.
    private void MergeLine(Line incoming, bool mayExist, bool bakeNow)
    {
        if (incoming == null || string.IsNullOrEmpty(incoming.id)) return;

        incoming.points ??= new List<Point>();
        incoming.history ??= new List<LineEvent>();

        Line known = FindLine(incoming.id);
        if (known == null)
        {
            drawing.lines.Add(incoming);
            known = incoming;
        }
        else if (!ReferenceEquals(known, incoming))
        {
            if (incoming.points.Count > known.points.Count) known.points = incoming.points;
            if (incoming.status == Status.ERASED && known.status != Status.ERASED)
            {
                known.status = Status.ERASED;
                if (incoming.history.Count > known.history.Count) known.history = incoming.history;
            }
        }

        SyncLineObject(known, mayExist, bakeNow);
    }

    // ------------------------------------------------------------------ LOADING

    /// <summary>
    /// Shows a drawing that was loaded from the database. Whatever arrived while the request was in flight (strokes
    /// and behaviour data of the other players, the local player's own samples) is merged into it, not thrown away.
    /// </summary>
    public void LoadDrawing(Drawing loaded)
    {
        if (loaded == null) return;

        loaded.lines ??= new List<Line>();
        loaded.trackedBehaviors ??= new List<TrackedBehavior>();
        loaded.placedModels ??= new List<PlacedModel>();
        loaded.events ??= new List<DrawingEvent>();

        Drawing arrived = drawing;
        bool mayExist = transform.childCount > 0;

        drawing = loaded;
        _seenPoseTimes.Clear();
        _seenEvents.Clear();
        _eventsIndexed = false;

        // The stored strokes are known by definition (and unique), so they only need their objects and colliders.
        foreach (Line line in loaded.lines)
        {
            line.points ??= new List<Point>();
            line.history ??= new List<LineEvent>();
            SyncLineObject(line, mayExist, bakeNow: true);
        }

        if (arrived == null) return;

        // What arrived meanwhile is merged against the stored strokes.
        foreach (Line line in arrived.lines) MergeLine(line, true, bakeNow: false);
        foreach (TrackedBehavior track in arrived.trackedBehaviors) AddTrack(track);
        AddEvents(arrived.events);
    }

    // ------------------------------------------------------------------ LINE CREATION

    // Called by the local drawer (Brush). The local LineRenderer is created by the Brush itself.
    public void AddNewLine(Line newLineData)
    {
        drawing.lines.Add(newLineData);
        if (DrawingSync.TryGetLive(out DrawingSync sync)) sync.SendNewLine(newLineData);
    }

    // Called by DrawingSync when another player started a line.
    public void ApplyRemoteNewLine(Line newLineData)
    {
        MergeLine(newLineData, mayExist: true, bakeNow: false);
    }

    // ------------------------------------------------------------------ POINT ADDITION

    public void AddNewPointToLine(string lineId, Point newPoint)
    {
        Line line = FindLine(lineId);
        if (line != null) line.points.Add(newPoint);

        if (DrawingSync.TryGetLive(out DrawingSync sync)) sync.SendPoint(lineId, newPoint);
    }

    public void ApplyRemotePoint(string lineId, Point newPoint)
    {
        Line line = FindLine(lineId);
        if (line == null) return;

        if (line.points == null) line.points = new List<Point>();

        // The points of a stroke come from one clock and are in order, so a point that is not newer than the last one
        // is already known (the snapshot of a joiner can contain what the relay delivers once more afterwards).
        int known = line.points.Count;
        if (newPoint.utcMs != 0 && known > 0 && line.points[known - 1].utcMs >= newPoint.utcMs) return;

        line.points.Add(newPoint);

        Transform lineObj = transform.Find(lineId);
        if (lineObj != null && lineObj.TryGetComponent<LineRenderer>(out var lr))
        {
            lr.positionCount++;
            lr.SetPosition(lr.positionCount - 1, newPoint.ToVector3());
            _pendingColliders[lineId] = Time.unscaledTime; // (re)bake the erase collider once the stroke goes idle
        }
    }

    // ------------------------------------------------------------------ ERASURE

    public void EraseLine(string lineId, string playerID, string timestamp, Hand hand)
    {
        ApplyErase(lineId, playerID, timestamp, hand);
        if (DrawingSync.TryGetLive(out DrawingSync sync)) sync.SendErase(lineId, playerID, timestamp, hand);
    }

    public void ApplyRemoteErase(string lineId, string playerID, string timestamp, Hand hand)
    {
        ApplyErase(lineId, playerID, timestamp, hand);
    }

    private void ApplyErase(string lineId, string playerID, string timestamp, Hand hand)
    {
        _pendingColliders.Remove(lineId); // an erased stroke must not get a (hittable) collider afterwards

        Line line = FindLine(lineId);
        if (line == null) return;

        line.status = Status.ERASED;
        if (line.history == null) line.history = new List<LineEvent>();
        line.history.Add(new LineEvent(LineEventType.ERASE, playerID, timestamp, hand));

        // Find the physical GameObject with this lineId and disable its renderer/collider
        Transform lineObj = transform.Find(lineId);
        if (lineObj != null)
        {
            if (lineObj.TryGetComponent<Renderer>(out var ren)) ren.enabled = false;
            if (lineObj.TryGetComponent<Collider>(out var col)) col.enabled = false;
        }
    }

    // ------------------------------------------------------------------ BEHAVIOUR DATA (tracked poses, events)

    private static TrackedBehavior NewTrack(string playerId) => new(playerId, new(), new(), new(), new());

    private TrackedBehavior GetOrCreateTrack(string playerId)
    {
        drawing.trackedBehaviors ??= new List<TrackedBehavior>();

        foreach (TrackedBehavior track in drawing.trackedBehaviors)
        {
            if (track.playerID != playerId) continue;

            // A stored track may come without some of the lists.
            track.head ??= new List<PoseData>();
            track.rightHand ??= new List<PoseData>();
            track.leftHand ??= new List<PoseData>();
            track.player ??= new List<PoseData>();
            return track;
        }

        TrackedBehavior created = NewTrack(playerId);
        drawing.trackedBehaviors.Add(created);
        return created;
    }

    private HashSet<long> SeenPoseTimes(string playerId, TrackedBehavior track)
    {
        if (!_seenPoseTimes.TryGetValue(playerId, out HashSet<long> seen))
        {
            seen = new HashSet<long>();
            foreach (PoseData pose in track.head)
            {
                if (pose != null) seen.Add(pose.utcMs);
            }
            _seenPoseTimes[playerId] = seen;
        }
        return seen;
    }

    private static string EventKey(DrawingEvent e) => $"{e.playerID}|{e.utcMs}|{(int)e.type}|{(int)e.hand}|{e.value}";

    private void EnsureEventIndex()
    {
        if (_eventsIndexed) return;

        drawing.events ??= new List<DrawingEvent>();
        foreach (DrawingEvent e in drawing.events)
        {
            if (e != null) _seenEvents.Add(EventKey(e));
        }
        _eventsIndexed = true;
    }

    // Adds the samples of a track that are not known yet (the channels of a track are index-aligned).
    private void AddTrack(TrackedBehavior source)
    {
        if (source == null || string.IsNullOrEmpty(source.playerID) || source.head == null) return;

        TrackedBehavior target = GetOrCreateTrack(source.playerID);
        HashSet<long> seen = SeenPoseTimes(source.playerID, target);

        for (int i = 0; i < source.head.Count; i++)
        {
            PoseData head = source.head[i];
            if (head == null) continue;
            if (head.utcMs != 0 && !seen.Add(head.utcMs)) continue; // already known

            target.head.Add(head);
            if (source.rightHand != null && i < source.rightHand.Count) target.rightHand.Add(source.rightHand[i]);
            if (source.leftHand != null && i < source.leftHand.Count) target.leftHand.Add(source.leftHand[i]);
            if (source.player != null && i < source.player.Count) target.player.Add(source.player[i]);
        }
    }

    private void AddEvents(List<DrawingEvent> events)
    {
        if (events == null || events.Count == 0) return;

        EnsureEventIndex();
        foreach (DrawingEvent e in events)
        {
            if (e != null && _seenEvents.Add(EventKey(e))) drawing.events.Add(e);
        }
    }

    private void ResetBehaviorBookkeeping()
    {
        _unsentPoses = null;
        _unsentEvents.Clear();
        _seenPoseTimes.Clear();
        _seenEvents.Clear();
        _eventsIndexed = false;
    }

    /// <summary>Called by the BehaviorTracker: one sample of the local player, relayed with the next flush.</summary>
    public void RecordPoses(string playerId, PoseData head, PoseData rightHand, PoseData leftHand, PoseData rig)
    {
        TrackedBehavior track = GetOrCreateTrack(playerId);
        track.head.Add(head);
        track.rightHand.Add(rightHand);
        track.leftHand.Add(leftHand);
        track.player.Add(rig);
        SeenPoseTimes(playerId, track).Add(head.utcMs);

        _unsentPoses ??= NewTrack(playerId);
        _unsentPoses.head.Add(head);
        _unsentPoses.rightHand.Add(rightHand);
        _unsentPoses.leftHand.Add(leftHand);
        _unsentPoses.player.Add(rig);
    }

    /// <summary>Records a change of the local player's tools or menus (when tracking is enabled).</summary>
    public void RecordEvent(DrawingEventType type, Hand hand, string value = null)
    {
        if (!ConfigurationManager.Current.Drawing.TrackingEnabled) return;

        string playerId = AuthManager.GetCurrentUserID();
        if (string.IsNullOrEmpty(playerId)) return;

        DrawingEvent e = new(type, playerId, hand, UtcNowMs(), value);
        EnsureEventIndex();
        _seenEvents.Add(EventKey(e));
        drawing.events.Add(e);
        _unsentEvents.Add(e);
    }

    /// <summary>
    /// Relays the behaviour data of the local player that the others have not received yet. Called periodically by
    /// the BehaviorTracker. Without a live session the data only stays local (and the unsent buffer is capped).
    /// </summary>
    public void FlushTelemetry()
    {
        bool hasPoses = _unsentPoses != null && _unsentPoses.head.Count > 0;
        if (!hasPoses && _unsentEvents.Count == 0) return;

        if (!DrawingSync.TryGetLive(out DrawingSync sync))
        {
            if (hasPoses && _unsentPoses.head.Count > MaxUnsentSamples)
            {
                int excess = _unsentPoses.head.Count - MaxUnsentSamples;
                _unsentPoses.head.RemoveRange(0, excess);
                _unsentPoses.rightHand.RemoveRange(0, excess);
                _unsentPoses.leftHand.RemoveRange(0, excess);
                _unsentPoses.player.RemoveRange(0, excess);
            }
            if (_unsentEvents.Count > MaxUnsentEvents) _unsentEvents.RemoveRange(0, _unsentEvents.Count - MaxUnsentEvents);
            return;
        }

        TrackedBehavior poses = hasPoses ? _unsentPoses : NewTrack(string.Empty);
        List<DrawingEvent> events = new(_unsentEvents);
        _unsentPoses = null;
        _unsentEvents.Clear();

        sync.SendTelemetry(poses, events);
    }

    // Called by DrawingSync with the behaviour data of another player (live batch or part of a snapshot).
    public void ApplyRemoteTelemetry(TrackedBehavior poses, List<DrawingEvent> events)
    {
        AddTrack(poses);
        AddEvents(events);
    }

    // ------------------------------------------------------------------ SNAPSHOT (for a player who joins late)

    /// <summary>The strokes in chunks of at most maxPoints points (a stroke is never split), one blob each.</summary>
    public List<List<Line>> BuildLineChunks(int maxPoints)
    {
        List<List<Line>> chunks = new();
        List<Line> current = new();
        int points = 0;

        foreach (Line line in drawing.lines)
        {
            int count = line.points?.Count ?? 0;
            if (current.Count > 0 && points + count > maxPoints)
            {
                chunks.Add(current);
                current = new List<Line>();
                points = 0;
            }
            current.Add(line);
            points += count;
        }

        if (current.Count > 0) chunks.Add(current);
        return chunks;
    }

    /// <summary>The tracked poses of all players in chunks of at most maxSamples samples per player.</summary>
    public List<TrackedBehavior> BuildPoseChunks(int maxSamples)
    {
        List<TrackedBehavior> chunks = new();
        if (drawing.trackedBehaviors == null) return chunks;

        foreach (TrackedBehavior track in drawing.trackedBehaviors)
        {
            if (string.IsNullOrEmpty(track.playerID)) continue;

            int samples = track.head?.Count ?? 0;
            for (int start = 0; start < samples; start += maxSamples)
            {
                chunks.Add(new TrackedBehavior(
                    track.playerID,
                    Slice(track.head, start, maxSamples),
                    Slice(track.rightHand, start, maxSamples),
                    Slice(track.leftHand, start, maxSamples),
                    Slice(track.player, start, maxSamples)));
            }
        }

        return chunks;
    }

    /// <summary>A copy of the events of all players.</summary>
    public List<DrawingEvent> GetEvents() => new(drawing.events ?? new List<DrawingEvent>());

    private static List<PoseData> Slice(List<PoseData> list, int start, int count)
    {
        if (list == null || start >= list.Count) return new List<PoseData>();
        return list.GetRange(start, Mathf.Min(count, list.Count - start));
    }

    // Called by DrawingSync with a part of the snapshot of the strokes.
    public void ApplyRemoteSnapshotLines(List<Line> lines)
    {
        if (lines == null) return;
        foreach (Line line in lines) MergeLine(line, mayExist: true, bakeNow: false);
    }

    // ------------------------------------------------------------------ DATABASE SAVE (NEW RECORD)

    // The other players that left a trace in the drawing (a stroke, a tracked sample or an event), with the time of
    // the first trace as "joined". The relay does not carry the moment somebody entered the room.
    private List<Collaborator> BuildCollaborators(string ownerId)
    {
        Dictionary<string, long> firstSeen = new();

        void See(string playerId, long utcMs)
        {
            if (string.IsNullOrEmpty(playerId) || playerId == ownerId) return;
            if (!firstSeen.TryGetValue(playerId, out long known) || (utcMs > 0 && (known == 0 || utcMs < known)))
                firstSeen[playerId] = utcMs;
        }

        foreach (Line line in drawing.lines)
            See(line.userID, line.points != null && line.points.Count > 0 ? line.points[0].utcMs : 0);

        if (drawing.trackedBehaviors != null)
        {
            foreach (TrackedBehavior track in drawing.trackedBehaviors)
                See(track.playerID, track.head != null && track.head.Count > 0 ? track.head[0].utcMs : 0);
        }

        if (drawing.events != null)
        {
            foreach (DrawingEvent e in drawing.events) See(e.playerID, e.utcMs);
        }

        List<Collaborator> collaborators = new();
        foreach (KeyValuePair<string, long> seen in firstSeen)
        {
            string joined = seen.Value > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(seen.Value).ToString("o") : "";
            collaborators.Add(new Collaborator(seen.Key, joined));
        }
        return collaborators;
    }

    // Called by a UI Button click. Every machine holds the complete drawing (its own strokes plus the strokes
    // relayed by the other players), so it can be saved locally without any network round trip.
    public void RequestSaveDrawing(string drawingName, string ownerId, GameType gameType, string sessionId)
    {
        _ = SaveDrawingTaskAsync(drawingName, ownerId, gameType, sessionId);
    }

    // 3. The actual async logic happens here, safely isolated from FishNet's code generator
    private async Task SaveDrawingTaskAsync(string drawingName, string ownerId, GameType gameType, string sessionId)
    {
        // Only the Server executes this database call
        DatabaseManager.DrawingSaveMessage payload = new()
        {
            name = drawingName,
            owner = ownerId,
            collaborators = BuildCollaborators(ownerId),
            version = "1.0",
            gameType = gameType,
            sessionID = sessionId,
            lines = this.drawing.lines,
            trackedBehaviors = this.drawing.trackedBehaviors,
            placedModels = this.drawing.placedModels,
            events = this.drawing.events,
            appVersion = Application.version,
            device = SystemInfo.deviceModel,
        };

        bool success = await DatabaseManager.Instance.SaveDrawing(payload);

        if (success) Debug.Log($"[DB] Drawing '{drawingName}' successfully committed to MongoDB and SQLite.");
        else Debug.LogError($"[DB] Failed to save drawing '{drawingName}'.");
    }

    // ------------------------------------------------------------------ DATABASE UPDATE (EXISTING RECORD)

    public void RequestUpdateDrawing(string drawingId, string drawingName)
    {
        _ = UpdateDrawingTaskAsync(drawingId, drawingName);
    }

    private async Task UpdateDrawingTaskAsync(string drawingId, string drawingName)
    {
        string ownerId = this.drawing.metadata?.owner;

        DatabaseManager.DrawingUpdateMessage payload = new()
        {
            name = drawingName,
            lines = this.drawing.lines,
            trackedBehaviors = this.drawing.trackedBehaviors,
            placedModels = this.drawing.placedModels,
            events = this.drawing.events,
            collaborators = BuildCollaborators(ownerId)
        };

        var updatedData = await DatabaseManager.Instance.UpdateDrawing(drawingId, payload);

        if (updatedData != null) Debug.Log($"[DB] Drawing '{drawingName}' successfully updated in MongoDB and SQLite.");
        else Debug.LogError($"[DB] Failed to update drawing '{drawingName}'.");
    }
}

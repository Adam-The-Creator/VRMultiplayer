using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;
using UnityEngine;

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
/// </summary>
public class VRDrawing : MonoBehaviour
{
    [SerializeField] private Material defaultLineMaterial;
    public Drawing drawing;

    private void Awake()
    {
        drawing ??= new Drawing();
    }

    // ------------------------------------------------------------------ helpers

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
        if (newLineData == null || string.IsNullOrEmpty(newLineData.id)) return;
        if (FindLine(newLineData.id) != null) return; // already known

        if (newLineData.points == null) newLineData.points = new List<Point>();
        if (newLineData.history == null) newLineData.history = new List<LineEvent>();
        drawing.lines.Add(newLineData);

        if (transform.Find(newLineData.id) != null) return;

        GameObject lineObject = new GameObject(newLineData.id);
        lineObject.transform.SetParent(transform);

        LineRenderer lr = lineObject.AddComponent<LineRenderer>();
        if (defaultLineMaterial != null) lr.material = defaultLineMaterial;
        else lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        lr.useWorldSpace = true;
        lr.startWidth = newLineData.startWidth;
        lr.endWidth = newLineData.endWidth;
        lr.startColor = newLineData.startColor != null ? newLineData.startColor.ToColor() : Color.black;
        lr.endColor = newLineData.endColor != null ? newLineData.endColor.ToColor() : Color.black;

        lr.positionCount = newLineData.points.Count;
        for (int i = 0; i < newLineData.points.Count; i++)
        {
            lr.SetPosition(i, newLineData.points[i].ToVector3());
        }

        lineObject.tag = "Line";
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
        line.points.Add(newPoint);

        Transform lineObj = transform.Find(lineId);
        if (lineObj != null && lineObj.TryGetComponent<LineRenderer>(out var lr))
        {
            lr.positionCount++;
            lr.SetPosition(lr.positionCount - 1, newPoint.ToVector3());
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

    // ------------------------------------------------------------------ DATABASE SAVE (NEW RECORD)

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
            collaborators = new List<Collaborator>(), // TODO: Get the collaborators
            version = "1.0",
            gameType = gameType,
            sessionID = sessionId,
            lines = this.drawing.lines,
            trackedBehaviors = this.drawing.trackedBehaviors,
            placedModels = this.drawing.placedModels,
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
        DatabaseManager.DrawingUpdateMessage payload = new()
        {
            name = drawingName,
            lines = this.drawing.lines,
            trackedBehaviors = this.drawing.trackedBehaviors,
            placedModels = this.drawing.placedModels,
            collaborators = new List<Collaborator>() // TODO: Get the collaborators
        };

        var updatedData = await DatabaseManager.Instance.UpdateDrawing(drawingId, payload);

        if (updatedData != null) Debug.Log($"[DB] Drawing '{drawingName}' successfully updated in MongoDB and SQLite.");
        else Debug.LogError($"[DB] Failed to update drawing '{drawingName}'.");
    }
}
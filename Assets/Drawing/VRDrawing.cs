using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;
using FishNet.Object;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class VRDrawing : NetworkBehaviour
{
    [SerializeField] private Material defaultLineMaterial;
    public Drawing drawing;

    private void Awake()
    {
        // Guarantee the object and its lists are instantiated before FishNet starts
        drawing ??= new Drawing();
    }

    private void Start()
    {
        Debug.Log($"[VRDrawing] '{name}' scene={gameObject.scene.name} sceneObject={NetworkObject.IsSceneObject} spawned={IsSpawned}");
    }
    public override void OnStartServer() => Debug.Log($"[VRDrawing] '{name}' OnStartServer");
    public override void OnStartClient() => Debug.Log($"[VRDrawing] '{name}' OnStartClient");

    // True only when THIS object is live on the network
    private bool NetActive => IsSpawned && IsClientInitialized;

    // --- LINE CREATION ---

    public void AddNewLine(Line newLineData)
    {
        if (NetActive)
        {
            CmdStartNewLine(newLineData);
        }
        else
        {
            // Local fallback for single-player
            drawing.lines.Add(newLineData);
        }
    }

    // 1. Client tells the Server they started a line
    [ServerRpc(RequireOwnership = false)]
    public void CmdStartNewLine(Line newLineData)
    {
        // Server updates its master data
        drawing.lines.Add(newLineData);
        // Server tells all clients to do the same
        RpcStartNewLine(newLineData);
    }

    // 2. Server tells all Clients to spawn the line
    [ObserversRpc]
    private void RpcStartNewLine(Line newLineData)
    {
        // Don't add it twice on the server if the server is also a host/client
        if (!IsServerInitialized)
        {
            drawing.lines.Add(newLineData);
        }

        // Only spawn visually for remote players. The local drawer already has the line.
        if (newLineData.userID != AuthManager.GetCurrentUserID() && transform.Find(newLineData.id) == null)
        {
            GameObject lineObject = new(newLineData.id);
            lineObject.transform.SetParent(this.transform);

            LineRenderer lr = lineObject.AddComponent<LineRenderer>();
            if (defaultLineMaterial != null) lr.material = defaultLineMaterial;
            else lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lr.useWorldSpace = true;
            lr.startWidth = newLineData.startWidth;
            lr.endWidth = newLineData.endWidth;
            lr.startColor = newLineData.startColor.ToColor();
            lr.endColor = newLineData.endColor.ToColor();

            lr.positionCount = newLineData.points.Count;
            for (int i = 0; i < newLineData.points.Count; i++)
            {
                lr.SetPosition(i, newLineData.points[i].ToVector3());
            }

            lineObject.tag = "Line";
        }
    }

    // --- POINT ADDITION ---

    public void AddNewPointToLine(string lineId, Point newPoint)
    {
        if (NetActive)
        {
            CmdAddPointToLine(lineId, newPoint);
        }
        else
        {
            // Local fallback for single-player
            for (int i = 0; i < drawing.lines.Count; i++)
            {
                if (drawing.lines[i].id == lineId)
                {
                    drawing.lines[i].points.Add(newPoint);
                    break;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdAddPointToLine(string lineId, Point newPoint)
    {
        RpcAddPointToLine(lineId, newPoint);
    }

    [ObserversRpc]
    private void RpcAddPointToLine(string lineId, Point newPoint)
    {
        // Find the line in the local database and add the point
        for (int i = 0; i < drawing.lines.Count; i++)
        {
            if (drawing.lines[i].id == lineId)
            {
                drawing.lines[i].points.Add(newPoint);

                // Update physical LineRenderer for remote clients
                if (drawing.lines[i].userID != AuthManager.GetCurrentUserID())
                {
                    Transform lineObj = transform.Find(lineId);
                    if (lineObj != null && lineObj.TryGetComponent<LineRenderer>(out var lr))
                    {
                        lr.positionCount++;
                        lr.SetPosition(lr.positionCount - 1, newPoint.ToVector3());
                    }
                }
                break;
            }
        }
    }

    // --- ERASURE ---

    public void EraseLine(string lineId, string playerID, string timestamp, Hand hand)
    {
        if (NetActive)
        {
            CmdEraseLine(lineId, playerID, timestamp, hand);
        }
        else
        {
            // Local fallback for single-player
            for (int idx = 0; idx < drawing.lines.Count; ++idx)
            {
                if (drawing.lines[idx].id == lineId)
                {
                    drawing.lines[idx].status = Status.ERASED;
                    drawing.lines[idx].history.Add(new LineEvent(LineEventType.ERASE, playerID, timestamp, hand));
                    // Find the physical GameObject with this lineId and disable its renderer/collider
                    Transform lineObj = transform.Find(lineId);
                    if (lineObj != null)
                    {
                        if (lineObj.TryGetComponent<Renderer>(out var ren)) ren.enabled = false;
                        if (lineObj.TryGetComponent<Collider>(out var col)) col.enabled = false;
                    }
                    break;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdEraseLine(string lineId, string playerID, string timestamp, Hand hand)
    {
        RpcEraseLine(lineId, playerID, timestamp, hand);
    }

    [ObserversRpc]
    private void RpcEraseLine(string lineId, string playerID, string timestamp, Hand hand)
    {
        for (int idx = 0; idx < drawing.lines.Count; ++idx)
        {
            if (drawing.lines[idx].id == lineId)
            {
                drawing.lines[idx].status = Status.ERASED;
                drawing.lines[idx].history.Add(new LineEvent(LineEventType.ERASE, playerID, timestamp, hand));

                // Find the physical GameObject with this lineId and disable its renderer/collider
                Transform lineObj = transform.Find(lineId);
                if (lineObj != null)
                {
                    if (lineObj.TryGetComponent<Renderer>(out var ren)) ren.enabled = false;
                    if (lineObj.TryGetComponent<Collider>(out var col)) col.enabled = false;
                }
                break;
            }
        }
    }

    // --- DATABASE SAVE (NEW RECORD) ---

    // Called by a UI Button click
    public void RequestSaveDrawing(string drawingName, string ownerId, GameType gameType, string sessionId)
    {
        if (NetActive)
        {
            // Tell the authoritative server to initiate the save process
            CmdSaveDrawingToDatabase(drawingName, ownerId, gameType, sessionId);
        }
        else
        {
            // Local fallback for single-player
            _ = SaveDrawingTaskAsync(drawingName, ownerId, gameType, sessionId);
        }
    }

    // 1. The RPC must be standard void, NOT async.
    [ServerRpc(RequireOwnership = false)]
    private void CmdSaveDrawingToDatabase(string drawingName, string ownerId, GameType gameType, string sessionId)
    {
        // 2. Launch the asynchronous database task without awaiting it directly in the RPC signature
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

    // --- DATABASE UPDATE (EXISTING RECORD) ---

    public void RequestUpdateDrawing(string drawingId, string drawingName)
    {
        if (NetActive)
        {
            CmdUpdateDrawingInDatabase(drawingId, drawingName);
        }
        else
        {
            _ = UpdateDrawingTaskAsync(drawingId, drawingName);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdUpdateDrawingInDatabase(string drawingId, string drawingName)
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
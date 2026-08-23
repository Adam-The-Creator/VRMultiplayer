using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;
using FishNet.Object;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class VRDrawing : NetworkBehaviour
{
    public Drawing drawing = new();

    // --- LINE CREATION ---

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

        // TODO: Here, you would also instantiate the physical LineRenderer GameObject 
        // on remote clients based on the newLineData, so they can see it.
    }

    // --- POINT ADDITION ---

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

                // TODO: Update the physical LineRenderer component's position count 
                // and SetPosition on remote clients so the line visually updates.
                break;
            }
        }
    }

    // --- ERASURE ---

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

                // TODO: Find the physical GameObject with this lineId and disable its renderer/collider
                break;
            }
        }
    }

    // --- DATABASE SAVE ---

    // Called by a UI Button click
    public void RequestSaveDrawing(string drawingName, string ownerId, GameType gameType, string sessionId)
    {
        // Tell the authoritative server to initiate the save process
        CmdSaveDrawingToDatabase(drawingName, ownerId, gameType, sessionId);
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

        if (success)
        {
            Debug.Log($"[DB] Drawing '{drawingName}' successfully committed to MongoDB and SQLite.");
            // Optional: You could send an ObserversRpc back to clients here 
            // to update their UI text to "Save Successful!"
        }
        else
        {
            Debug.LogError($"[DB] Failed to save drawing '{drawingName}'.");
        }
    }
}
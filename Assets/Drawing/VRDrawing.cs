using FishNet.Object;
using UnityEngine;
using Assets.Database.DatabaseManagement.MongoDB;
using System.Collections.Generic;

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
}
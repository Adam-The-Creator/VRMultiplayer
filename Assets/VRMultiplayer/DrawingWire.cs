using System;
using System.Collections.Generic;
using System.IO;
using Assets.Database.DatabaseManagement.MongoDB;
using UnityEngine;

// UnityEngine has a Pose as well.
using PoseData = Assets.Database.DatabaseManagement.MongoDB.Pose;

/// <summary>
/// The binary form of the bulk data that <see cref="DrawingSync"/> relays: batches of behaviour data (tracked
/// poses, tool events) and the strokes of a late-join snapshot.
///
/// FishNet's generated serializers would also do this, but they put the whole object graph into one RPC, and
/// RPCs of several KB are split by FishNet into transport-sized pieces. With those the clients reported misparsed
/// packets ("unhandled PacketId", "RPCLink ... could not be found"), so DrawingSync sends a blob made here in small
/// fragments instead. The format is private to the game (both ends run the same build), versioned by the first byte.
/// Strings are nullable, enums travel as ints, and everything read from the network is bounds-checked.
/// </summary>
public static class DrawingWire
{
    private const byte Version = 1;

    // Smallest possible size of one item in the data, used to reject a count that cannot fit into what is left.
    private const int MinPoseBytes = 1;
    private const int MinEventBytes = 18;
    private const int MinLineBytes = 28;
    private const int MinPointBytes = 25;
    private const int MinLineEventBytes = 10;

    // ------------------------------------------------------------------ behaviour data

    public static byte[] EncodeTelemetry(TrackedBehavior poses, List<DrawingEvent> events)
    {
        using MemoryStream stream = new();
        using BinaryWriter w = new(stream);

        w.Write(Version);
        WriteString(w, poses?.playerID);
        WritePoses(w, poses?.head);
        WritePoses(w, poses?.rightHand);
        WritePoses(w, poses?.leftHand);
        WritePoses(w, poses?.player);

        w.Write(CountNonNull(events));
        if (events != null)
        {
            foreach (DrawingEvent e in events)
            {
                if (e != null) WriteEvent(w, e);
            }
        }

        w.Flush();
        return stream.ToArray();
    }

    public static bool TryDecodeTelemetry(byte[] data, out TrackedBehavior poses, out List<DrawingEvent> events)
    {
        poses = null;
        events = null;

        try
        {
            using MemoryStream stream = new(data, writable: false);
            using BinaryReader r = new(stream);
            CheckVersion(r);

            string playerId = ReadString(r);
            List<PoseData> head = ReadPoses(r);
            List<PoseData> rightHand = ReadPoses(r);
            List<PoseData> leftHand = ReadPoses(r);
            List<PoseData> player = ReadPoses(r);

            int eventCount = ReadCount(r, MinEventBytes);
            List<DrawingEvent> readEvents = new(eventCount);
            for (int i = 0; i < eventCount; i++) readEvents.Add(ReadEvent(r));

            poses = new TrackedBehavior(playerId, head, rightHand, leftHand, player);
            events = readEvents;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DrawingWire] The behaviour data could not be read ({data?.Length ?? 0} bytes): {e.Message}");
            return false;
        }
    }

    private static void WritePoses(BinaryWriter w, List<PoseData> poses)
    {
        w.Write(poses?.Count ?? 0);
        if (poses == null) return;

        // A missing pose keeps its place: the channels of a track are index-aligned.
        foreach (PoseData p in poses)
        {
            if (p == null)
            {
                w.Write((byte)0);
                continue;
            }

            w.Write(p.timestamp != null ? (byte)2 : (byte)1);
            w.Write(p.utcMs);
            w.Write(p.pX);
            w.Write(p.pY);
            w.Write(p.pZ);
            w.Write(p.rX);
            w.Write(p.rY);
            w.Write(p.rZ);
            w.Write(p.rW);
            if (p.timestamp != null) w.Write(p.timestamp);
        }
    }

    private static List<PoseData> ReadPoses(BinaryReader r)
    {
        int count = ReadCount(r, MinPoseBytes);
        List<PoseData> poses = new(count);

        for (int i = 0; i < count; i++)
        {
            byte flags = r.ReadByte();
            if (flags == 0)
            {
                poses.Add(null);
                continue;
            }

            PoseData p = new()
            {
                utcMs = r.ReadInt64(),
                pX = r.ReadSingle(),
                pY = r.ReadSingle(),
                pZ = r.ReadSingle(),
                rX = r.ReadSingle(),
                rY = r.ReadSingle(),
                rZ = r.ReadSingle(),
                rW = r.ReadSingle()
            };
            if (flags == 2) p.timestamp = r.ReadString();
            poses.Add(p);
        }

        return poses;
    }

    private static void WriteEvent(BinaryWriter w, DrawingEvent e)
    {
        w.Write((int)e.type);
        WriteString(w, e.playerID);
        w.Write((int)e.hand);
        w.Write(e.utcMs);
        WriteString(w, e.value);
    }

    private static DrawingEvent ReadEvent(BinaryReader r)
    {
        DrawingEventType type = (DrawingEventType)r.ReadInt32();
        string playerId = ReadString(r);
        Hand hand = (Hand)r.ReadInt32();
        long utcMs = r.ReadInt64();
        string value = ReadString(r);
        return new DrawingEvent(type, playerId, hand, utcMs, value);
    }

    // ------------------------------------------------------------------ strokes

    public static byte[] EncodeLines(List<Line> lines)
    {
        using MemoryStream stream = new();
        using BinaryWriter w = new(stream);

        w.Write(Version);
        w.Write(CountNonNull(lines));
        if (lines != null)
        {
            foreach (Line line in lines)
            {
                if (line != null) WriteLine(w, line);
            }
        }

        w.Flush();
        return stream.ToArray();
    }

    public static bool TryDecodeLines(byte[] data, out List<Line> lines)
    {
        lines = null;

        try
        {
            using MemoryStream stream = new(data, writable: false);
            using BinaryReader r = new(stream);
            CheckVersion(r);

            int count = ReadCount(r, MinLineBytes);
            List<Line> read = new(count);
            for (int i = 0; i < count; i++) read.Add(ReadLine(r));

            lines = read;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DrawingWire] The strokes could not be read ({data?.Length ?? 0} bytes): {e.Message}");
            return false;
        }
    }

    private static void WriteLine(BinaryWriter w, Line line)
    {
        WriteString(w, line.id);
        WriteString(w, line.userID);
        w.Write(line.startWidth);
        w.Write(line.endWidth);
        WriteColor(w, line.startColor);
        WriteColor(w, line.endColor);
        w.Write((int)line.hand);
        w.Write((int)line.status);

        w.Write(CountNonNull(line.points));
        if (line.points != null)
        {
            foreach (Point p in line.points)
            {
                if (p == null) continue;
                w.Write(p.x);
                w.Write(p.y);
                w.Write(p.z);
                w.Write(p.utcMs);
                w.Write(p.pressure);
                WriteString(w, p.timestamp);
            }
        }

        w.Write(CountNonNull(line.history));
        if (line.history != null)
        {
            foreach (LineEvent h in line.history)
            {
                if (h == null) continue;
                w.Write((int)h.eventType);
                WriteString(w, h.invoker);
                WriteString(w, h.timestamp);
                w.Write((int)h.hand);
            }
        }
    }

    private static Line ReadLine(BinaryReader r)
    {
        Line line = new()
        {
            id = ReadString(r),
            userID = ReadString(r),
            startWidth = r.ReadSingle(),
            endWidth = r.ReadSingle(),
            startColor = ReadColor(r),
            endColor = ReadColor(r),
            hand = (Hand)r.ReadInt32(),
            status = (Status)r.ReadInt32()
        };

        int pointCount = ReadCount(r, MinPointBytes);
        line.points = new List<Point>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            line.points.Add(new Point
            {
                x = r.ReadSingle(),
                y = r.ReadSingle(),
                z = r.ReadSingle(),
                utcMs = r.ReadInt64(),
                pressure = r.ReadSingle(),
                timestamp = ReadString(r)
            });
        }

        int historyCount = ReadCount(r, MinLineEventBytes);
        line.history = new List<LineEvent>(historyCount);
        for (int i = 0; i < historyCount; i++)
        {
            line.history.Add(new LineEvent
            {
                eventType = (LineEventType)r.ReadInt32(),
                invoker = ReadString(r),
                timestamp = ReadString(r),
                hand = (Hand)r.ReadInt32()
            });
        }

        return line;
    }

    private static void WriteColor(BinaryWriter w, LineColor color)
    {
        w.Write(color != null);
        if (color == null) return;

        w.Write(color.r);
        w.Write(color.g);
        w.Write(color.b);
        w.Write(color.a);
    }

    private static LineColor ReadColor(BinaryReader r)
    {
        return r.ReadBoolean() ? new LineColor(r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadSingle()) : null;
    }

    // ------------------------------------------------------------------ helpers

    private static void WriteString(BinaryWriter w, string value)
    {
        w.Write(value != null);
        if (value != null) w.Write(value);
    }

    private static string ReadString(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;

    private static int CountNonNull<T>(List<T> list) where T : class
    {
        if (list == null) return 0;

        int count = 0;
        foreach (T item in list)
        {
            if (item != null) count++;
        }
        return count;
    }

    private static void CheckVersion(BinaryReader r)
    {
        byte version = r.ReadByte();
        if (version != Version) throw new InvalidDataException($"unknown version {version}");
    }

    // A count read from the network must fit into the data that is left, otherwise it is corrupt: this keeps the
    // lists from being allocated at a size the sender never sent.
    private static int ReadCount(BinaryReader r, int minBytesPerItem)
    {
        int count = r.ReadInt32();
        long remaining = r.BaseStream.Length - r.BaseStream.Position;
        if (count < 0 || (long)count * minBytesPerItem > remaining) throw new InvalidDataException($"item count {count} exceeds the data");
        return count;
    }
}

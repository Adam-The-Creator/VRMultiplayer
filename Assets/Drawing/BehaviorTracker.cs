using UnityEngine;
using UnityEngine.SceneManagement;

// UnityEngine has a Pose as well.
using PoseData = Assets.Database.DatabaseManagement.MongoDB.Pose;

/// <summary>
/// Records what the local player does while drawing: the poses of the head, both hands and the rig (where the
/// player stands) at Preferences.Drawing.TrackingSampleRateHz, in world space like the strokes.
///
/// The samples go into the <see cref="VRDrawing"/> next to this component (which adds this component itself),
/// and are relayed to the other players in batches, so every machine can save the complete drawing.
/// Switched off with Preferences.Drawing.TrackingEnabled.
/// </summary>
[RequireComponent(typeof(VRDrawing))]
public class BehaviorTracker : MonoBehaviour
{
    private const float MinRateHz = 1f;
    private const float MaxRateHz = 30f;
    private const float FlushIntervalSeconds = 5f;

    // Anything beyond this is treated as corrupt tracking data (NaN, Infinity, garbage), the same as Avatar does.
    private const float MaxCoordinate = 10000f;

    private VRDrawing _drawing;
    private float _nextSampleTime;
    private float _nextFlushTime;

    private void Awake()
    {
        _drawing = GetComponent<VRDrawing>();
    }

    private void Update()
    {
        DrawingSettings settings = ConfigurationManager.Current.Drawing;
        if (!settings.TrackingEnabled) return;

        // The same rule as for drawing itself: nothing is recorded in the lobby.
        if (settings.DrawingBlockedScenes.Contains(SceneManager.GetActiveScene().name)) return;

        Player player = Player.LocalInstance;
        string playerId = AuthManager.GetCurrentUserID();
        if (player == null || string.IsNullOrEmpty(playerId)) return;
        if (player.vrCameraTransform == null || player.leftControllerTransform == null || player.rightControllerTransform == null)
            return;

        float now = Time.unscaledTime;

        if (now >= _nextSampleTime)
        {
            _nextSampleTime = now + 1f / Mathf.Clamp(settings.TrackingSampleRateHz, MinRateHz, MaxRateHz);
            Sample(player, playerId);
        }

        if (now >= _nextFlushTime)
        {
            _nextFlushTime = now + FlushIntervalSeconds;
            _drawing.FlushTelemetry();
        }
    }

    private void Sample(Player player, string playerId)
    {
        long utcMs = VRDrawing.UtcNowMs();

        PoseData head = Read(player.vrCameraTransform, utcMs);
        PoseData right = Read(player.rightControllerTransform, utcMs);
        PoseData left = Read(player.leftControllerTransform, utcMs);
        PoseData rig = Read(player.transform, utcMs);

        // A tracking glitch must not end up in the data: skip the whole sample so the channels stay aligned.
        if (!IsValid(head) || !IsValid(right) || !IsValid(left) || !IsValid(rig)) return;

        _drawing.RecordPoses(playerId, head, right, left, rig);
    }

    private static PoseData Read(Transform source, long utcMs) => new(source.position, source.rotation, utcMs);

    private static bool IsValid(PoseData pose)
    {
        // Comparisons with NaN are false, so NaN and +-Infinity are rejected by the same checks.
        bool positionOk = Mathf.Abs(pose.pX) < MaxCoordinate && Mathf.Abs(pose.pY) < MaxCoordinate && Mathf.Abs(pose.pZ) < MaxCoordinate;
        if (!positionOk) return false;

        float sqrLength = pose.rX * pose.rX + pose.rY * pose.rY + pose.rZ * pose.rZ + pose.rW * pose.rW;
        return sqrLength > 0.0001f && sqrLength < 100f; // finite, non-zero, roughly unit length
    }
}

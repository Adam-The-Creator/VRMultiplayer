using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class Avatar : NetworkBehaviour
{
    // ROLE: Mirrors the owner's XR hardware (head + hands) onto the networked avatar on every machine.
    //
    // The avatar prefab has no NetworkTransform, so FishNet never replicates its transform. Instead the owner
    // samples the local Player rig and streams the world-space pose to the server (unreliable, ~30 Hz); the
    // server relays it to every other client, which smooths towards the latest pose. This is the same
    // local-first relay pattern DrawingSync uses for strokes.

    [Header("Avatar Transforms")]
    [Tooltip("Transform placed on the player's head. Leave empty to move the avatar root, which keeps the head " +
             "model's authored offset inside the prefab.")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [SerializeField, Tooltip("The offset of the avatar from the camera, in the camera's local space (x right, y up, z forward)")]
    protected Vector3 avatarOffset;

    [Header("Network sync")]
    [SerializeField, Tooltip("Pose updates per second the owner sends")]
    private float sendRate = 30f;

    [SerializeField, Tooltip("How quickly remote avatars catch up with the received pose (higher = snappier)")]
    private float followSpeed = 25f;

    [SerializeField, Tooltip("A jump bigger than this many metres (e.g. a teleport) snaps instead of gliding")]
    private float snapDistance = 2f;

    // Anything beyond this is treated as corrupt tracking data (NaN, Infinity, garbage) and ignored.
    private const float MaxCoordinate = 10000f;

    private Renderer[] _renderers;
    private float _nextSendTime;

    // Latest pose received from the owner (remote avatars only).
    private bool _hasRemotePose;
    private Vector3 _headPos, _leftPos, _rightPos;
    private Quaternion _headRot, _leftRot, _rightRot;

    private Transform HeadTransform => headTarget != null ? headTarget : transform;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Both MeshRenderer and SkinnedMeshRenderer, across the entire avatar.
        _renderers = GetComponentsInChildren<Renderer>();

        if (base.IsOwner)
        {
            // Hide the head model locally so it doesn't block the player's view: it only renders its shadow.
            foreach (Renderer ren in _renderers)
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
        else
        {
            // Stay invisible until the first pose arrives, otherwise the model is seen at the spawn point.
            SetRenderersVisible(false);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _hasRemotePose = false;
    }

    // LateUpdate so the XR rig has already been moved by tracking this frame.
    private void LateUpdate()
    {
        if (!base.IsClientInitialized) return;

        if (base.IsOwner) UpdateAsOwner();
        else UpdateAsRemote();
    }

    // ------------------------------------------------------------------ owner

    // Owner: mirror the local hardware onto this avatar and stream the pose to everybody else.
    private void UpdateAsOwner()
    {
        Player player = Player.LocalInstance;
        if (player == null || player.vrCameraTransform == null) return;

        Transform cam = player.vrCameraTransform;
        Quaternion headRot = cam.rotation;
        Vector3 headPos = cam.position + headRot * avatarOffset;

        // A missing controller falls back to the head so the RPC always carries a valid pose.
        GetPose(player.leftControllerTransform, headPos, headRot, out Vector3 leftPos, out Quaternion leftRot);
        GetPose(player.rightControllerTransform, headPos, headRot, out Vector3 rightPos, out Quaternion rightRot);

        if (!IsValidPose(headPos, headRot) || !IsValidPose(leftPos, leftRot) || !IsValidPose(rightPos, rightRot))
            return; // tracking glitch: keep the last good pose instead of poisoning the transforms

        // The owner's own copy follows the hardware directly (no smoothing).
        Place(HeadTransform, headPos, headRot, 1f, 0f);
        Place(leftHandTarget, leftPos, leftRot, 1f, 0f);
        Place(rightHandTarget, rightPos, rightRot, 1f, 0f);

        if (Time.unscaledTime < _nextSendTime) return;
        _nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);

        CmdSendPose(headPos, headRot, leftPos, leftRot, rightPos, rightRot);
    }

    private static void GetPose(Transform source, Vector3 fallbackPos, Quaternion fallbackRot,
        out Vector3 position, out Quaternion rotation)
    {
        position = source != null ? source.position : fallbackPos;
        rotation = source != null ? source.rotation : fallbackRot;
    }

    // ------------------------------------------------------------------ remote

    // Remote: glide towards the latest pose received from the owner.
    private void UpdateAsRemote()
    {
        if (!_hasRemotePose) return;

        float blend = 1f - Mathf.Exp(-Mathf.Max(1f, followSpeed) * Time.deltaTime);
        float snapSqr = snapDistance * snapDistance;

        Place(HeadTransform, _headPos, _headRot, blend, snapSqr);
        Place(leftHandTarget, _leftPos, _leftRot, blend, snapSqr);
        Place(rightHandTarget, _rightPos, _rightRot, blend, snapSqr);
    }

    // Moves a target towards a world pose. blend >= 1, or a jump beyond snapSqr (squared metres), places it exactly.
    private static void Place(Transform target, Vector3 position, Quaternion rotation, float blend, float snapSqr)
    {
        if (target == null) return;

        if (blend >= 1f || (target.position - position).sqrMagnitude > snapSqr)
        {
            target.SetPositionAndRotation(position, rotation);
            return;
        }

        target.SetPositionAndRotation(
            Vector3.Lerp(target.position, position, blend),
            Quaternion.Slerp(target.rotation, rotation, blend));
    }

    private void SetRenderersVisible(bool visible)
    {
        if (_renderers == null) return;

        foreach (Renderer ren in _renderers)
        {
            if (ren != null) ren.enabled = visible;
        }
    }

    // ------------------------------------------------------------------ network relay

    // Owner -> server. Unreliable on purpose: a lost update is simply replaced by the next one.
    // RequireOwnership (default) makes the server reject anybody but the owner.
    [ServerRpc]
    private void CmdSendPose(Vector3 headPos, Quaternion headRot, Vector3 leftPos, Quaternion leftRot,
        Vector3 rightPos, Quaternion rightRot, Channel channel = Channel.Unreliable)
    {
        if (!IsValidPose(headPos, headRot) || !IsValidPose(leftPos, leftRot) || !IsValidPose(rightPos, rightRot))
            return;

        RpcPose(headPos, headRot, leftPos, leftRot, rightPos, rightRot);
    }

    // Server -> every client except the owner, who already has the pose. The host's own client receives it too.
    [ObserversRpc(ExcludeOwner = true)]
    private void RpcPose(Vector3 headPos, Quaternion headRot, Vector3 leftPos, Quaternion leftRot,
        Vector3 rightPos, Quaternion rightRot, Channel channel = Channel.Unreliable)
    {
        if (!IsValidPose(headPos, headRot) || !IsValidPose(leftPos, leftRot) || !IsValidPose(rightPos, rightRot))
            return;

        _headPos = headPos;
        _headRot = Quaternion.Normalize(headRot);
        _leftPos = leftPos;
        _leftRot = Quaternion.Normalize(leftRot);
        _rightPos = rightPos;
        _rightRot = Quaternion.Normalize(rightRot);

        if (_hasRemotePose) return;

        // First pose: jump straight to it and reveal the model.
        _hasRemotePose = true;
        Place(HeadTransform, _headPos, _headRot, 1f, 0f);
        Place(leftHandTarget, _leftPos, _leftRot, 1f, 0f);
        Place(rightHandTarget, _rightPos, _rightRot, 1f, 0f);
        SetRenderersVisible(true);
    }

    private static bool IsValidPose(Vector3 position, Quaternion rotation)
    {
        // Comparisons with NaN are false, so NaN and +-Infinity are rejected by the same checks.
        bool positionOk = Mathf.Abs(position.x) < MaxCoordinate
                          && Mathf.Abs(position.y) < MaxCoordinate
                          && Mathf.Abs(position.z) < MaxCoordinate;
        if (!positionOk) return false;

        float sqrLength = rotation.x * rotation.x + rotation.y * rotation.y
                          + rotation.z * rotation.z + rotation.w * rotation.w;
        return sqrLength > 0.0001f && sqrLength < 100f; // finite, non-zero, roughly unit length
    }
}

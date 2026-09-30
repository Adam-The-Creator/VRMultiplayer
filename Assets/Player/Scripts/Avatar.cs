using FishNet.Object;
using UnityEngine;

public class Avatar : NetworkBehaviour
{
    // ROLE: Controls the networked avatar's movement by mimicking the local Player hardware.

    [Header("Avatar Transforms")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;

    [SerializeField, Tooltip("The offset of the avatar from the camera")]
    protected Vector3 avatarOffset;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Optional: Hide the head model locally so it doesn't block the player's view
        if (base.IsOwner)
        {
            // Grab all renderers (both MeshRenderer and SkinnedMeshRenderer) across the entire avatar
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

            foreach (var ren in allRenderers)
            {
                // Set the mesh to render only its shadow, making it invisible to the local camera
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }


    private void Update()
    {
        // Only the owner should dictate where the avatar is moving
        if (!base.IsOwner) return;

        // Ensure the local hardware has loaded
        if (Player.LocalInstance == null) return;

        // Map local hardware transforms to the networked avatar transforms
        if (headTarget != null && Player.LocalInstance.vrCameraTransform != null)
        {
            headTarget.SetPositionAndRotation(
                Player.LocalInstance.vrCameraTransform.position + avatarOffset,
                Player.LocalInstance.vrCameraTransform.rotation
            );
        }

        if (leftHandTarget != null && Player.LocalInstance.leftControllerTransform != null)
        {
            leftHandTarget.SetPositionAndRotation(
                Player.LocalInstance.leftControllerTransform.position,
                Player.LocalInstance.leftControllerTransform.rotation
            );
        }

        if (rightHandTarget != null && Player.LocalInstance.rightControllerTransform != null)
        {
            rightHandTarget.SetPositionAndRotation(
                Player.LocalInstance.rightControllerTransform.position,
                Player.LocalInstance.rightControllerTransform.rotation
            );
        }
    }
}

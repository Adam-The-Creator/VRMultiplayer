using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Avatar : MonoBehaviour
{
    //ROLE: This class is responsible for controlling the avatar's movement and interactions in the game.
    //      It updates the avatar's position, and manages animations. Use inverse kinematics in case of
    //      using human-like avatar.

    [Header("Avatar Settings")]
    public GameObject avatar;

    [SerializeField, Tooltip("Enable or disable the visibility of the avatar object")]
    private bool enableAvatar = true;

    [SerializeField, Tooltip("The Transform of the camera")]
    private Transform vrCameraTransform;

    [SerializeField, Tooltip("The Transform of the left controller")]
    private Transform leftControllerTransform;

    [SerializeField, Tooltip("The Transform of the right controller")]
    private Transform rightControllerTransform;

    [SerializeField, Tooltip("The Transform of the Avatar Object")]
    protected Transform avatarTransform;

    [SerializeField, Tooltip("The offset of the avatar from the camera")]
    protected Vector3 avatarOffset;

    public void Initialize(Transform vrCam, Transform avatar, Transform leftController = null, Transform rightController = null)
    {
        vrCameraTransform = vrCam;
        leftControllerTransform = leftController;
        rightControllerTransform = rightController;
        avatarTransform = avatar;
        this.avatar.SetActive(enableAvatar);
    }
    private void LateUpdate()
    {
        if (vrCameraTransform != null)
        {
            avatarTransform.SetPositionAndRotation(vrCameraTransform.position + avatarOffset, Quaternion.Euler(0, vrCameraTransform.rotation.eulerAngles.y, 0));
        }
    }
}

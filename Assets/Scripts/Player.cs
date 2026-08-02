using FishNet.Managing.Scened;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class Player : MonoBehaviour
{
    //ROLE: The highest level component that represents the player in the scene.
    //      It holds references to the camera, controllers and avatar, and initializes the avatar with these references.

    //public static Player LocalInstance { get; private set; }

    [SerializeField, Tooltip("The Object of XR Device Simulator")]
    private GameObject xrDeviceSimulatorObject;

    [SerializeField, Tooltip("The Transform of the camera")]
    private Transform vrCameraTransform;

    [SerializeField, Tooltip("The Transform of the left controller (UniversalController object)")]
    private Transform leftControllerTransform;

    [SerializeField, Tooltip("The Transform of the right controller (UniversalController object)")]
    private Transform rightControllerTransform;

    [SerializeField, Tooltip("The Avatar component")]
    private Avatar avatar;

    [SerializeField, Tooltip("The Character Controller component")]
    private CharacterController characterController;

    [SerializeField, Tooltip("The Left Brush reference")]
    private Brush leftBrush;

    [SerializeField, Tooltip("The Right Brush reference")]
    private Brush rightBrush;


    private void Awake()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();

        DontDestroyOnLoad(this.gameObject);
    }



    //public override void OnStartClient()
    //{
    //    base.OnStartClient();

    //    if (base.IsOwner)
    //    {
    //        LocalInstance = this;
    //        SetupLocalPlayer();
    //    }
    //    else
    //    {
    //        SetupRemotePlayer();
    //    }

    //    AssignDrawingObject();
    //}

    //private void SetupLocalPlayer()
    //{
    //    GameObject tempPlayer = GameObject.FindGameObjectWithTag("TemporaryPlayer");

    //    if (tempPlayer != null)
    //    {
    //        Debug.Log("[Player] Temporary Player found. Taking over control.");
    //        StartCoroutine(DestroyTemporaryPlayer(tempPlayer));
    //    }

    //    if (avatar != null) avatar.Initialize(vrCameraTransform, avatar.transform, leftControllerTransform, rightControllerTransform);
    //    else Debug.LogWarning("Avatar component is not assigned in the Player script.");

    //    if (xrDeviceSimulatorObject != null)
    //    {
    //        bool isRealHeadset = false;
    //        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
    //        {
    //            var activeLoader = XRGeneralSettings.Instance.Manager.activeLoader;
    //            if (activeLoader != null)
    //            {
    //                Debug.Log($"[XR] Active Loader: {activeLoader.name}");
    //                if (!activeLoader.name.ToLower().Contains("mock")) isRealHeadset = true;
    //            }
    //            else Debug.LogWarning("[XR] No active XR Loader.");
    //        }
    //        Debug.Log($"[XR] Real VR device found: {isRealHeadset}.\n[XR] XR Simulator enabled: {!isRealHeadset}");
    //        xrDeviceSimulatorObject.SetActive(!isRealHeadset);
    //    }

    //    base.NetworkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
    //}

    //private void SetupRemotePlayer()
    //{

    //    Camera cam = gameObject.GetComponent<XROrigin>().Camera;
    //    if (cam != null)
    //    {
    //        cam.enabled = false;
    //        if (cam.TryGetComponent<AudioListener>(out var listener)) listener.enabled = false;
    //    }

    //    if (characterController != null) characterController.enabled = false;

    //    if (xrDeviceSimulatorObject != null) xrDeviceSimulatorObject.SetActive(false);

    //    TrackedPoseDriver[] poseDrivers = GetComponentsInChildren<TrackedPoseDriver>(true);
    //    foreach (var driver in poseDrivers) driver.enabled = false;

    //    XRBaseInteractor[] interactors = GetComponentsInChildren<XRBaseInteractor>(true);
    //    foreach (XRBaseInteractor interactor in interactors) interactor.enabled = false;

    //    LocomotionProvider[] locomotionProviders = GetComponentsInChildren<LocomotionProvider>(true);
    //    foreach (var provider in locomotionProviders) provider.enabled = false;
    //}

    //private IEnumerator DestroyTemporaryPlayer(GameObject tempPlayer)
    //{
    //    XRUIInputModule inputModule = null;
    //    if (EventSystem.current != null)
    //    {
    //        inputModule = EventSystem.current.GetComponent<XRUIInputModule>();
    //        if (inputModule != null) inputModule.enabled = false;

    //        EventSystem.current.SetSelectedGameObject(null);
    //    }

    //    tempPlayer.SetActive(false);

    //    yield return null;
    //    yield return null;

    //    if (inputModule != null) inputModule.enabled = true;

    //    Camera newCam = vrCameraTransform.GetComponent<Camera>();
    //    if (newCam != null)
    //    {
    //        newCam.enabled = false;
    //        newCam.enabled = true;
    //    }

    //    if (tempPlayer != null)
    //    {
    //        Destroy(tempPlayer);
    //        Debug.Log("[Player] Temporary Player successfully destroyed. Camera focus and UI updated.");
    //    }
    //}

    //public override void OnStopClient()
    //{
    //    base.OnStopClient();
    //    if (base.IsOwner)
    //    {
    //        if (base.NetworkManager != null && base.NetworkManager.SceneManager != null)
    //        {
    //            base.NetworkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
    //        }
    //        if (LocalInstance == this) LocalInstance = null;
    //    }
    //}

    //private void AssignDrawingObject()
    //{
    //    GameObject drawingObject = GameObject.FindGameObjectWithTag("Drawing");
    //    if (leftBrush != null && rightBrush != null && drawingObject != null)
    //    {
    //        leftBrush.drawingObject = drawingObject;
    //        rightBrush.drawingObject = drawingObject;
    //    }
    //}

    //private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    //{
    //    if (!base.IsOwner) return;

    //    foreach (Scene scene in args.LoadedScenes)
    //    {
    //        if (scene.name == "SetupScene") continue;

    //        GameObject spawnPoint = GameObject.FindWithTag("Respawn") ?? GameObject.FindWithTag("Anchor");

    //        if (spawnPoint != null)
    //        {
    //            if (characterController != null) characterController.enabled = false;
    //            transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
    //            Physics.SyncTransforms();
    //            if (characterController != null) characterController.enabled = true;
    //        }

    //        AssignDrawingObject();
    //        break;
    //    }
    //}
}

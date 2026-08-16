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
    // ROLE: The highest level component that represents the local hardware in the scene.

    public static Player LocalInstance { get; private set; }

    [Header("Hardware Transforms")]
    [Tooltip("The Transform of the camera")]
    public Transform vrCameraTransform;

    [Tooltip("The Transform of the left controller")]
    public Transform leftControllerTransform;

    [Tooltip("The Transform of the right controller")]
    public Transform rightControllerTransform;

    private void Awake()
    {
        // Enforce Singleton pattern
        if (LocalInstance != null && LocalInstance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        LocalInstance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}

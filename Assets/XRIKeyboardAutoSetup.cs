using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class XRIKeyboardAutoSetup : MonoBehaviour
{
    private void Awake()
    {
        // Hook into scene loading to catch additive scenes (like your Room/Drawing scenes)
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Run once at startup for the Lobby scene
        SetupAllInputFields();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupAllInputFields();
    }

    private void SetupAllInputFields()
    {
        // Find ALL input fields, including ones on disabled UI panels
        TMP_InputField[] allInputFields = FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var inputField in allInputFields)
        {
            SetupSingleInputField(inputField);
        }

        Debug.Log($"[XRIKeyboardAutoSetup] Successfully configured {allInputFields.Length} input fields for VR.");
    }

    private void SetupSingleInputField(TMP_InputField inputField)
    {
        inputField.shouldHideMobileInput = true;

        // 1. Setup XRKeyboardDisplay
        if (!inputField.TryGetComponent<XRKeyboardDisplay>(out var keyboardDisplay))
        {
            keyboardDisplay = inputField.gameObject.AddComponent<XRKeyboardDisplay>();
        }
        keyboardDisplay.inputField = inputField;

        // 2. Setup Canvas Raycaster
        Canvas canvas = inputField.GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            // Remove the standard GraphicRaycaster to prevent XR conflicts
            if (canvas.TryGetComponent<UnityEngine.UI.GraphicRaycaster>(out var oldRaycaster))
            {
                Destroy(oldRaycaster);
            }

            // Ensure the XR-specific raycaster is present
            if (!canvas.TryGetComponent<TrackedDeviceGraphicRaycaster>(out _))
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
        }
    }
}
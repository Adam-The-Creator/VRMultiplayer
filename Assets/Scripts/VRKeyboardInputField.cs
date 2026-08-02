using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

[RequireComponent(typeof(TMP_InputField))]
public class VRKeyboardInputField : MonoBehaviour, IPointerClickHandler, ISelectHandler
{
    private TMP_InputField inputField;
    private VRKeyboard keyboard;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        if (inputField == null)
        {
            Debug.LogError("VRKeyboardInputField: TMP_InputField component not found!");
        }

        SetupXRInteraction();
    }

    void Start()
    {
        keyboard = VRKeyboard.Instance;
        if (keyboard == null)
        {
            Debug.LogWarning("VRKeyboardInputField: VRKeyboard instance not found. Make sure VRKeyboard is in the scene.");
        }
    }

    private void SetupXRInteraction()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            TrackedDeviceGraphicRaycaster raycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            if (raycaster == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
        }

        if (inputField != null)
        {
            inputField.shouldHideMobileInput = true;
           
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenVRKeyboard();
    }

    public void OnSelect(BaseEventData eventData)
    {
        OpenVRKeyboard();
    }

    private void OpenVRKeyboard()
    {
        Debug.Log($"VRKeyboardInputField: OpenVRKeyboard called on {gameObject.name}");
        
        if (keyboard == null)
        {
            keyboard = VRKeyboard.Instance;
            if (keyboard == null)
            {
                Debug.LogError("VRKeyboardInputField: VRKeyboard instance not found! Make sure VRKeyboard is in the scene.");
                return;
            }
        }
        
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
            if (inputField == null)
            {
                Debug.LogError("VRKeyboardInputField: TMP_InputField component not found!");
                return;
            }
        }
        
        if (keyboard != null && inputField != null)
        {
            Debug.Log("VRKeyboardInputField: Calling keyboard.OpenKeyboard()");
            keyboard.OpenKeyboard(inputField);
        }
        else
        {
            Debug.LogWarning("VRKeyboardInputField: Cannot open keyboard - keyboard or inputField is null.");
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        StartCoroutine(CloseKeyboardDelayed());
    }

    private System.Collections.IEnumerator CloseKeyboardDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (inputField != null && !inputField.isFocused && keyboard != null)
        {
        }
    }
}


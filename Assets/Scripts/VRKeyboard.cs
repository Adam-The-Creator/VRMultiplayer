using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class VRKeyboard : MonoBehaviour
{
    [Header("Keyboard UI")]
    [SerializeField] private GameObject keyboardPanel;
    [SerializeField] private TMP_InputField currentInputField;
    [SerializeField] private TextMeshProUGUI displayText;

    [Header("Keyboard Buttons")]
    [SerializeField] private Button[] letterButtons;
    [SerializeField] private Button spaceButton;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button shiftButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button[] numberButtons;

    [Header("Keyboard Layouts")]
    [SerializeField] private GameObject lettersLayout;
    [SerializeField] private GameObject numbersLayout;
    [SerializeField] private Button switchToNumbersButton;
    [SerializeField] private Button switchToLettersButton;

    private bool isUpperCase = false;
    private bool isNumbersLayout = false;
    private string currentText = "";
    private bool isFollowingCamera = false;
    private Camera vrCamera;
    private Canvas keyboardCanvas; 
    private const float FOLLOW_DISTANCE = 1.5f;
    private const float FOLLOW_HEIGHT_OFFSET = -0.3f;

    private static VRKeyboard instance;
    public static VRKeyboard Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VRKeyboard>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (keyboardPanel != null || letterButtons != null)
        {
            InitializeKeyboard();
        }
    }

    void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private System.Collections.IEnumerator DelayedStart()
    {
        yield return null; 
        
        keyboardCanvas = GetComponentInChildren<Canvas>(true); 
        
        if (keyboardCanvas == null)
        {
            Debug.LogWarning("VRKeyboard: Canvas not found! Keyboard may not have been generated yet. Retrying...");
            yield return new WaitForSeconds(0.1f);
            keyboardCanvas = GetComponentInChildren<Canvas>(true);
        }
        
        keyboardPanel = keyboardCanvas?.transform.Find("KeyboardPanel")?.gameObject;
        
        if (keyboardPanel == null && keyboardCanvas != null)
        {
            foreach (Transform child in keyboardCanvas.transform)
            {
                if (child.name == "KeyboardPanel")
                {
                    keyboardPanel = child.gameObject;
                    break;
                }
            }
        }
        
        if (keyboardPanel != null)
        {
            keyboardPanel.SetActive(false);
            Debug.Log("VRKeyboard: Keyboard panel found and disabled on Start");
        }
        else
        {
            Debug.LogWarning("VRKeyboard: Keyboard panel not found! Make sure the keyboard has been generated. Check VRKeyboardGenerator component.");
        }
        
        FindVRCamera();
        
        SetupAllInputFields();
    }

    private void SetupAllInputFields()
    {
        TMP_InputField[] allInputFields = UnityEngine.Object.FindObjectsOfType<TMP_InputField>(true); 

        int setupCount = 0;
        foreach (TMP_InputField inputField in allInputFields)
        {
            if (inputField != null && inputField.GetComponent<VRKeyboardInputField>() == null)
            {
                inputField.gameObject.AddComponent<VRKeyboardInputField>();
                setupCount++;
            }
        }

        if (setupCount > 0)
        {
            Debug.Log($"VRKeyboard: Auto-setup complete. Configured {setupCount} input field(s).");
        }
    }

    [ContextMenu("Setup All Input Fields Now")]
    public void SetupAllInputFieldsManual()
    {
        SetupAllInputFields();
    }

    private void FindVRCamera()
    {
        vrCamera = Camera.main;
        
        if (vrCamera == null)
        {
            try
            {
                var xrOriginType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XROrigin, UnityEngine.XR.Interaction.Toolkit");
                if (xrOriginType != null)
                {
                    var xrOrigin = FindObjectOfType(xrOriginType);
                    if (xrOrigin != null)
                    {
                        var cameraProperty = xrOriginType.GetProperty("Camera");
                        if (cameraProperty != null)
                        {
                            var camera = cameraProperty.GetValue(xrOrigin) as Camera;
                            if (camera != null)
                            {
                                vrCamera = camera;
                                Debug.Log("VRKeyboard: Found XR Origin camera via reflection");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"VRKeyboard: Reflection failed to find XR camera: {e.Message}");
            }
            
            if (vrCamera == null)
            {
                GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (cameraObj != null)
                {
                    vrCamera = cameraObj.GetComponent<Camera>();
                    Debug.Log("VRKeyboard: Found camera via MainCamera tag");
                }
                else
                {
                    GameObject xrOriginObj = GameObject.Find("XR Origin (XR Rig)");
                    if (xrOriginObj != null)
                    {
                        vrCamera = xrOriginObj.GetComponentInChildren<Camera>();
                        if (vrCamera != null)
                        {
                            Debug.Log("VRKeyboard: Found camera via XR Origin GameObject name");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log("VRKeyboard: Using Camera.main");
        }
        
        if (vrCamera == null)
        {
            Debug.LogWarning("VRKeyboard: Could not find VR camera! Keyboard will not follow player.");
        }
    }

    private void InitializeKeyboard()
    {
        if (letterButtons != null && letterButtons.Length > 0)
        {
            for (int i = 0; i < letterButtons.Length && i < 26; i++)
            {
                if (letterButtons[i] != null)
                {
                    char letter = (char)('A' + i);
                    TextMeshProUGUI buttonText = letterButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = letter.ToString();
                    }

                    char letterToAdd = letter;
                    letterButtons[i].onClick.AddListener(() => AddCharacter(letterToAdd));
                }
            }
        }

        if (numberButtons != null && numberButtons.Length > 0)
        {
            for (int i = 0; i < numberButtons.Length && i < 10; i++)
            {
                if (numberButtons[i] != null)
                {
                    char number = (char)('0' + i);
                    TextMeshProUGUI buttonText = numberButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = number.ToString();
                    }

                    char numberToAdd = number;
                    numberButtons[i].onClick.AddListener(() => AddCharacter(numberToAdd));
                }
            }
        }

        if (spaceButton != null)
        {
            spaceButton.onClick.AddListener(() => AddCharacter(' '));
        }

        if (backspaceButton != null)
        {
            backspaceButton.onClick.AddListener(DeleteLastCharacter);
        }

        if (enterButton != null)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(() => {
                Debug.Log("Enter button clicked!");
                CloseKeyboard();
            });
        }

        if (shiftButton != null)
        {
            shiftButton.onClick.AddListener(ToggleShift);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                Debug.Log("Close button clicked!");
                CloseKeyboard();
            });
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearText);
        }

        if (switchToNumbersButton != null)
        {
            switchToNumbersButton.onClick.AddListener(() => SwitchLayout(true));
        }

        if (switchToLettersButton != null)
        {
            switchToLettersButton.onClick.AddListener(() => SwitchLayout(false));
        }

        MakeButtonsXRInteractable();
    }

    public void AssignGeneratedReferences(
        GameObject panel,
        TextMeshProUGUI display,
        Button[] letters,
        Button space,
        Button backspace,
        Button enter,
        Button shift,
        Button close,
        Button clear,
        Button[] numbers,
        GameObject lettersLayoutObj,
        GameObject numbersLayoutObj,
        Button switchToNumbers,
        Button switchToLetters)
    {
        keyboardPanel = panel;
        displayText = display;
        letterButtons = letters;
        spaceButton = space;
        backspaceButton = backspace;
        enterButton = enter;
        shiftButton = shift;
        closeButton = close;
        clearButton = clear;
        numberButtons = numbers;
        lettersLayout = lettersLayoutObj;
        numbersLayout = numbersLayoutObj;
        switchToNumbersButton = switchToNumbers;
        switchToLettersButton = switchToLetters;

        InitializeKeyboard();
    }

    private void MakeButtonsXRInteractable()
    {
        Button[] allButtons = GetComponentsInChildren<Button>();
        foreach (Button button in allButtons)
        {
            if (button != null)
            {
                Canvas canvas = button.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    TrackedDeviceGraphicRaycaster raycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                    if (raycaster == null)
                    {
                        canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                    }
                }

                if (button.targetGraphic != null)
                {
                    button.targetGraphic.raycastTarget = true;
                }
            }
        }
    }

    public void OpenKeyboard(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            Debug.LogWarning("VRKeyboard: OpenKeyboard called with null inputField!");
            return;
        }

        Debug.Log($"VRKeyboard: OpenKeyboard called for input field: {inputField.name}");

        if (keyboardCanvas == null)
        {
            keyboardCanvas = GetComponentInChildren<Canvas>(true); 
            if (keyboardCanvas == null)
            {
                Debug.LogError("VRKeyboard: Canvas not found! Make sure the keyboard has been generated.");
                return;
            }
        }
        
        bool isKeyboardCurrentlyVisible = (keyboardPanel != null && keyboardPanel.activeSelf) || 
                                         (keyboardCanvas != null && keyboardCanvas.gameObject.activeSelf);
        
        if (isKeyboardCurrentlyVisible && currentInputField == inputField)
        {
            Debug.Log("VRKeyboard: Keyboard already open for this input field");
            return;
        }

        if (isKeyboardCurrentlyVisible && currentInputField != inputField)
        {
            Debug.Log("VRKeyboard: Closing previous keyboard for different input field");
            CloseKeyboard();
        }

        if (vrCamera == null)
        {
            FindVRCamera();
        }

        currentInputField = inputField;
        currentText = inputField.text ?? "";

        if (keyboardPanel == null)
        {
            Debug.LogError("VRKeyboard: Cannot open keyboard - keyboardPanel is null! Make sure the keyboard has been generated.");
            return;
        }

        if (keyboardCanvas != null)
        {
            if (!keyboardCanvas.gameObject.activeSelf)
            {
                keyboardCanvas.gameObject.SetActive(true);
                Debug.Log("VRKeyboard: Canvas re-enabled");
            }
        }
        else
        {
            Debug.LogError("VRKeyboard: Canvas not found! Keyboard may not work properly.");
            return;
        }
        
        keyboardPanel.SetActive(true);
        Debug.Log("VRKeyboard: Keyboard panel activated - active: " + keyboardPanel.activeSelf);
        
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log("VRKeyboard: VRKeyboard GameObject activated");
        }
        
        SetupClickOutsideDetection();
        isFollowingCamera = true;
        UpdateKeyboardPosition();
        
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"VRKeyboard: Keyboard opened successfully - Canvas active: {keyboardCanvas.gameObject.activeSelf}, Panel active: {keyboardPanel.activeSelf}, GameObject active: {gameObject.activeSelf}");
        Debug.Log($"VRKeyboard: Keyboard position: {transform.position}, Canvas position: {keyboardCanvas.transform.position}");
    }

    private void SetupClickOutsideDetection()
    {
        RemoveBackgroundBlocker();

        if (keyboardCanvas == null)
        {
            keyboardCanvas = GetComponentInChildren<Canvas>(true); 
        }
        
        if (keyboardCanvas == null) return;

        GameObject backgroundBlocker = new GameObject("KeyboardBackgroundBlocker");
        backgroundBlocker.transform.SetParent(keyboardCanvas.transform, false);
        backgroundBlocker.transform.SetAsFirstSibling(); 

        Image blockerImage = backgroundBlocker.AddComponent<Image>();
        blockerImage.color = new Color(0, 0, 0, 0.01f); 
        blockerImage.raycastTarget = true;

        RectTransform blockerRect = backgroundBlocker.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.sizeDelta = Vector2.zero;
        blockerRect.anchoredPosition = Vector2.zero;

        Button blockerButton = backgroundBlocker.AddComponent<Button>();
        blockerButton.onClick.AddListener(() => CloseKeyboard());
        
        TrackedDeviceGraphicRaycaster raycaster = keyboardCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
        if (raycaster == null)
        {
            keyboardCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
    }

    private void RemoveBackgroundBlocker()
    {
        if (keyboardCanvas == null)
        {
            keyboardCanvas = GetComponentInChildren<Canvas>(true); 
        }
        
        if (keyboardCanvas != null)
        {
            Transform backgroundBlocker = keyboardCanvas.transform.Find("KeyboardBackgroundBlocker");
            if (backgroundBlocker != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(backgroundBlocker.gameObject);
                }
                else
                {
                    DestroyImmediate(backgroundBlocker.gameObject);
                }
            }
        }
    }

    public void CloseKeyboard()
    {
        Debug.Log("VRKeyboard: CloseKeyboard called!");
        
        isFollowingCamera = false;

        RemoveBackgroundBlocker();

        if (currentInputField != null)
        {
            currentInputField.text = currentText;
            if (currentInputField.isFocused)
            {
                currentInputField.DeactivateInputField();
            }
            currentInputField = null;
        }

        if (keyboardCanvas == null)
        {
            keyboardCanvas = GetComponentInChildren<Canvas>(true); 
        }
        
        if (keyboardCanvas != null)
        {
            keyboardCanvas.gameObject.SetActive(false);
            Debug.Log("VRKeyboard: Canvas disabled");
        }

        if (keyboardPanel != null)
        {
            keyboardPanel.SetActive(false);
            Debug.Log("VRKeyboard: Keyboard panel set to inactive");
        }
        else
        {
            Debug.LogWarning("VRKeyboard: keyboardPanel is null!");
        }

        currentText = "";
        
        Debug.Log("VRKeyboard: Keyboard closed successfully");
    }

    private void UpdateKeyboardPosition()
    {
        if (!isFollowingCamera) return;

        if (vrCamera == null)
        {
            FindVRCamera();
            if (vrCamera == null)
            {
                Debug.LogWarning("VRKeyboard: Cannot update position - VR camera not found!");
                return;
            }
        }

        Transform cameraTransform = vrCamera.transform;
        if (cameraTransform == null)
        {
            Debug.LogWarning("VRKeyboard: Camera transform is null!");
            return;
        }

        Vector3 position = cameraTransform.position + cameraTransform.forward * FOLLOW_DISTANCE;
        position.y = cameraTransform.position.y + FOLLOW_HEIGHT_OFFSET; 
        
        transform.position = position;
        
        Vector3 directionToCamera = cameraTransform.position - transform.position;
        directionToCamera.y = 0; 
        
        if (directionToCamera.sqrMagnitude > 0.01f) 
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera.normalized);
            //Debug.Log($"VRKeyboard: Position updated to {position}, rotation set");
        }
    }

    private void AddCharacter(char character)
    {
        if (character >= 'A' && character <= 'Z')
        {
            character = isUpperCase ? character : char.ToLower(character);
            if (isUpperCase)
            {
                StartCoroutine(DisableShiftAfterDelay());
            }
        }

        currentText += character;

        if (currentInputField != null)
        {
            currentInputField.text = currentText;
        }
    }

    private IEnumerator DisableShiftAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        if (isUpperCase)
        {
            ToggleShift();
        }
    }

    private void DeleteLastCharacter()
    {
        if (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);


            if (currentInputField != null)
            {
                currentInputField.text = currentText;
            }
        }
    }

    private void ClearText()
    {
        currentText = "";

        if (currentInputField != null)
        {
            currentInputField.text = currentText;
        }
    }

    private void ToggleShift()
    {
        isUpperCase = !isUpperCase;
        UpdateLetterButtonsCase();
        UpdateShiftButton();
    }

    private void UpdateLetterButtonsCase()
    {
        if (letterButtons == null) return;

        for (int i = 0; i < letterButtons.Length && i < 26; i++)
        {
            if (letterButtons[i] != null)
            {
                TextMeshProUGUI buttonText = letterButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    char letter = (char)('A' + i);
                    buttonText.text = isUpperCase ? letter.ToString() : char.ToLower(letter).ToString();
                }
            }
        }
    }

    private void UpdateShiftButton()
    {
        if (shiftButton != null)
        {
            Image buttonImage = shiftButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isUpperCase ? Color.yellow : Color.white;
            }
        }
    }

    private void SwitchLayout(bool showNumbers)
    {
        isNumbersLayout = showNumbers;

        if (lettersLayout != null)
        {
            lettersLayout.SetActive(!showNumbers);
        }

        if (numbersLayout != null)
        {
            numbersLayout.SetActive(showNumbers);
        }

        if (switchToNumbersButton != null)
        {
            switchToNumbersButton.gameObject.SetActive(!showNumbers);
        }

        if (switchToLettersButton != null)
        {
            switchToLettersButton.gameObject.SetActive(showNumbers);
        }
    }

    void Update()
    {
        if (keyboardCanvas == null)
        {
            keyboardCanvas = GetComponentInChildren<Canvas>(true);
        }
        
        bool isKeyboardVisible = (keyboardPanel != null && keyboardPanel.activeSelf) || 
                                (keyboardCanvas != null && keyboardCanvas.gameObject.activeSelf);
        
        if (!isKeyboardVisible)
        {
            isFollowingCamera = false;
            return;
        }

        if (isFollowingCamera && isKeyboardVisible)
        {
            UpdateKeyboardPosition();
        }
        
        if (vrCamera == null && isKeyboardVisible)
        {
            FindVRCamera();
        }

        if (currentInputField != null && keyboardPanel != null && keyboardPanel.activeSelf)
        {
            if (currentInputField.text != currentText)
            {
                currentText = currentInputField.text;
            }

            if (currentInputField == null || !currentInputField.gameObject.activeInHierarchy)
            {
                CloseKeyboard();
            }
        }

        #if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape) && keyboardPanel != null && keyboardPanel.activeSelf)
        {
            CloseKeyboard();
        }
        #endif
    }
}


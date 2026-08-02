using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;

[RequireComponent(typeof(VRKeyboard))]
public class VRKeyboardGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private float buttonSize = 60f;
    [SerializeField] private float buttonSpacing = 10f;
    [SerializeField] private float rowSpacing = 15f;
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private float keyboardWidth = 900f;
    [SerializeField] private float keyboardHeight = 600f;

    [Header("References")]
    [SerializeField] private Font buttonFont;
    
    private VRKeyboard keyboard;
    private Canvas keyboardCanvas;
    private GameObject keyboardPanel;
    private GameObject lettersLayout;
    private GameObject numbersLayout;
    private Button[] letterButtons = new Button[26];
    private Button[] numberButtons = new Button[10];

    void Start()
    {
        keyboard = GetComponent<VRKeyboard>();
        if (generateOnStart)
        {
            GenerateKeyboard();
        }
    }

    [ContextMenu("Generate Keyboard")]
    public void GenerateKeyboard()
    {
        if (keyboard == null)
        {
            keyboard = GetComponent<VRKeyboard>();
            if (keyboard == null)
            {
                Debug.LogError("VRKeyboardGenerator: VRKeyboard component not found!");
                return;
            }
        }

        CreateCanvas();
        
        CreateKeyboardPanel();
        
        CreateLayouts();
        
        CreateLetterButtons();
        
        CreateNumberButtons();
        
        CreateSpecialButtons();
        
        
        AssignReferencesToVRKeyboard();
        
        Canvas.ForceUpdateCanvases();
        
        Debug.Log("VR Keyboard generated successfully!");
    }

    private void CreateCanvas()
    {
        keyboardCanvas = GetComponentInChildren<Canvas>();
        if (keyboardCanvas == null)
        {
            GameObject canvasObj = new GameObject("KeyboardCanvas");
            canvasObj.transform.SetParent(transform);
            keyboardCanvas = canvasObj.AddComponent<Canvas>();
            keyboardCanvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<TrackedDeviceGraphicRaycaster>();
            
            RectTransform canvasRect = keyboardCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(keyboardWidth, keyboardHeight);
            canvasRect.localScale = Vector3.one * 0.001f; 
            canvasRect.localRotation = Quaternion.identity;
            
            Canvas.ForceUpdateCanvases();
        }
    }

    private void CreateKeyboardPanel()
    {
        if (keyboardPanel != null)
        {
            if (Application.isPlaying)
                Destroy(keyboardPanel);
            else
                DestroyImmediate(keyboardPanel);
        }
        
        keyboardPanel = new GameObject("KeyboardPanel");
        keyboardPanel.transform.SetParent(keyboardCanvas.transform, false);
        
        Image panelImage = keyboardPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        RectTransform panelRect = keyboardPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.localPosition = Vector3.zero;
        panelRect.localRotation = Quaternion.identity;
        panelRect.localScale = Vector3.one;
        
        Canvas.ForceUpdateCanvases();
        
        if (Application.isPlaying)
        {
        }
    }

    private void CreateLayouts()
    {
        lettersLayout = new GameObject("LettersLayout");
        lettersLayout.transform.SetParent(keyboardPanel.transform, false);
        RectTransform lettersRect = lettersLayout.AddComponent<RectTransform>();
        lettersRect.anchorMin = new Vector2(0, 0);
        lettersRect.anchorMax = new Vector2(1, 1);
        lettersRect.sizeDelta = Vector2.zero;
        lettersRect.anchoredPosition = Vector2.zero;
        lettersRect.offsetMin = Vector2.zero;
        lettersRect.offsetMax = Vector2.zero;
        lettersRect.localPosition = Vector3.zero;
        lettersRect.localRotation = Quaternion.identity;
        lettersRect.localScale = Vector3.one;

        numbersLayout = new GameObject("NumbersLayout");
        numbersLayout.transform.SetParent(keyboardPanel.transform, false);
        RectTransform numbersRect = numbersLayout.AddComponent<RectTransform>();
        numbersRect.anchorMin = new Vector2(0, 0);
        numbersRect.anchorMax = new Vector2(1, 1);
        numbersRect.sizeDelta = Vector2.zero;
        numbersRect.anchoredPosition = Vector2.zero;
        numbersRect.offsetMin = Vector2.zero;
        numbersRect.offsetMax = Vector2.zero;
        numbersRect.localPosition = Vector3.zero;
        numbersRect.localRotation = Quaternion.identity;
        numbersRect.localScale = Vector3.one;
        numbersLayout.SetActive(false);
    }

    private void CreateLetterButtons()
    {
        RectTransform panelRect = keyboardPanel.GetComponent<RectTransform>();
        
        Canvas.ForceUpdateCanvases();
        
        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;
        
        if (panelWidth == 0) panelWidth = keyboardWidth;
        if (panelHeight == 0) panelHeight = keyboardHeight;
        
        float startY = panelHeight * 0.5f - buttonSize * 0.5f - buttonSpacing; 
        float numberRowY = startY;
        float numberRowWidth = 10 * (buttonSize + buttonSpacing) - buttonSpacing;
        float numberStartX = -numberRowWidth * 0.5f; 
        
        numberButtons = new Button[10];
        for (int i = 0; i < 10; i++)
        {
            Button button = CreateButton(i.ToString(), lettersLayout.transform,
                new Vector2(numberStartX + i * (buttonSize + buttonSpacing), numberRowY));
            numberButtons[i] = button;
        }

        string[] rows = {
            "QWERTZUIOP",  
            "ASDFGHJKL",
            "YXCVBNM"       
        };

        letterButtons = new Button[26];
        float letterStartY = startY - (buttonSize + rowSpacing); 

        for (int row = 0; row < rows.Length; row++)
        {
            string rowLetters = rows[row];
            float rowY = letterStartY - (row * (buttonSize + rowSpacing));
            float rowWidth = rowLetters.Length * (buttonSize + buttonSpacing) - buttonSpacing;
            float startX = -rowWidth * 0.5f; 

            for (int col = 0; col < rowLetters.Length; col++)
            {
                char letter = rowLetters[col];
                Button button = CreateButton(letter.ToString(), lettersLayout.transform, 
                    new Vector2(startX + col * (buttonSize + buttonSpacing), rowY));
                
                int letterIndex = char.ToUpper(letter) - 'A';
                if (letterIndex >= 0 && letterIndex < 26)
                {
                    letterButtons[letterIndex] = button;
                }
            }
        }
    }

    private void CreateNumberButtons()
    {
        RectTransform panelRect = keyboardPanel.GetComponent<RectTransform>();
        float panelHeight = panelRect.rect.height;
        
        float startY = panelHeight * 0.5f - buttonSize * 0.5f - buttonSpacing;
        float rowWidth = 10 * (buttonSize + buttonSpacing) - buttonSpacing;
        float startX = -rowWidth * 0.5f; 

        Button[] numberLayoutButtons = new Button[10];
        for (int i = 0; i < 10; i++)
        {
            Button button = CreateButton(i.ToString(), numbersLayout.transform,
                new Vector2(startX + i * (buttonSize + buttonSpacing), startY));
            numberLayoutButtons[i] = button;
        }
        
        if (numberButtons == null || numberButtons.Length == 0)
        {
            numberButtons = numberLayoutButtons;
        }
    }

    private void CreateSpecialButtons()
    {
        Canvas.ForceUpdateCanvases();
        
        RectTransform panelRect = keyboardPanel.GetComponent<RectTransform>();
        float panelHeight = panelRect.rect.height;
        float panelWidth = panelRect.rect.width;
        
        if (panelHeight == 0) panelHeight = keyboardHeight;
        if (panelWidth == 0) panelWidth = keyboardWidth;
        
        float specialButtonWidth = buttonSize * 1.5f;
        float specialButtonSpacing = buttonSpacing * 2f;
        float bottomY = -panelHeight * 0.5f + buttonSize * 0.5f + buttonSpacing * 2f;
        float topRowY = bottomY + buttonSize + rowSpacing;
        
        Button closeButton = CreateButton("ESC", lettersLayout.transform,
            new Vector2(0, bottomY), 
            new Vector2(buttonSize * 2.5f, buttonSize * 1.3f)); 

        Button spaceButton = CreateButton("SPACE", lettersLayout.transform,
            new Vector2(0, bottomY + buttonSize * 1.5f),
            new Vector2(buttonSize * 5f, buttonSize));

        Button shiftButton = CreateButton("SHIFT", lettersLayout.transform,
            new Vector2(-panelRect.rect.width * 0.25f - specialButtonWidth * 0.5f, topRowY),
            new Vector2(specialButtonWidth, buttonSize));

        Button backspaceButton = CreateButton("DEL", lettersLayout.transform,
            new Vector2(panelRect.rect.width * 0.25f + specialButtonWidth * 0.5f, topRowY),
            new Vector2(specialButtonWidth, buttonSize));

        Button enterButton = CreateButton("ENTER", lettersLayout.transform,
            new Vector2(panelRect.rect.width * 0.4f, bottomY + buttonSize * 1.5f), 
            new Vector2(specialButtonWidth, buttonSize));

        Button clearButton = CreateButton("CLEAR", lettersLayout.transform,
            new Vector2(-panelRect.rect.width * 0.1f, topRowY),
            new Vector2(specialButtonWidth, buttonSize));

        Button switchToNumbersButton = CreateButton("123", lettersLayout.transform,
            new Vector2(-panelRect.rect.width * 0.4f, bottomY),
            new Vector2(specialButtonWidth, buttonSize));

        Button switchToLettersButton = CreateButton("ABC", numbersLayout.transform,
            new Vector2(-panelRect.rect.width * 0.4f, bottomY),
            new Vector2(specialButtonWidth, buttonSize));

        StoreSpecialButtonReferences(spaceButton, shiftButton, backspaceButton, enterButton, 
            clearButton, closeButton, switchToNumbersButton, switchToLettersButton);
    }

    private void StoreSpecialButtonReferences(Button space, Button shift, Button backspace, 
        Button enter, Button clear, Button close, Button switchToNumbers, Button switchToLetters)
    {
        VRKeyboardButtonReferences buttonRefs = GetComponent<VRKeyboardButtonReferences>();
        if (buttonRefs == null)
        {
            buttonRefs = gameObject.AddComponent<VRKeyboardButtonReferences>();
        }
        
        buttonRefs.spaceButton = space;
        buttonRefs.shiftButton = shift;
        buttonRefs.backspaceButton = backspace;
        buttonRefs.enterButton = enter;
        buttonRefs.clearButton = clear;
        buttonRefs.closeButton = close;
        buttonRefs.switchToNumbersButton = switchToNumbers;
        buttonRefs.switchToLettersButton = switchToLetters;
    }

    private void CreateDisplayText()
    {
        GameObject displayTextObj = new GameObject("DisplayText");
        displayTextObj.transform.SetParent(keyboardPanel.transform, false);
        
        TextMeshProUGUI displayText = displayTextObj.AddComponent<TextMeshProUGUI>();
        displayText.text = "";
        displayText.fontSize = 24;
        displayText.color = Color.white;
        displayText.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = displayTextObj.GetComponent<RectTransform>();
        RectTransform panelRect = keyboardPanel.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.8f);
        textRect.anchorMax = new Vector2(1, 0.95f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.localPosition = Vector3.zero;
        textRect.localScale = Vector3.one;
        
        VRKeyboardButtonReferences buttonRefs = GetComponent<VRKeyboardButtonReferences>();
        if (buttonRefs != null)
        {
            buttonRefs.displayText = displayText;
        }
    }

    private Button CreateButton(string label, Transform parent, Vector2 position, Vector2? size = null)
    {
        GameObject buttonObj = new GameObject(label + "Button");
        buttonObj.transform.SetParent(parent, false);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = buttonColor;

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 24;
        text.color = buttonTextColor;
        text.alignment = TextAlignmentOptions.Center;
        if (buttonFont != null)
        {
        }

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        textRect.localPosition = Vector3.zero;
        textRect.localScale = Vector3.one;

        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        Vector2 buttonSizeV = size ?? new Vector2(buttonSize, buttonSize);
        
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = buttonSizeV;
        
        buttonRect.anchoredPosition = position;
        
        buttonRect.localRotation = Quaternion.identity;
        buttonRect.localScale = Vector3.one;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        button.colors = colors;

        return button;
    }

    private void AssignReferencesToVRKeyboard()
    {
        VRKeyboardButtonReferences buttonRefs = GetComponent<VRKeyboardButtonReferences>();
        if (buttonRefs == null)
        {
            Debug.LogError("VRKeyboardGenerator: Button references not found! Please regenerate the keyboard.");
            return;
        }

        keyboard.AssignGeneratedReferences(
            keyboardPanel,
            null, 
            letterButtons,
            buttonRefs.spaceButton,
            buttonRefs.backspaceButton,
            buttonRefs.enterButton,
            buttonRefs.shiftButton,
            buttonRefs.closeButton,
            buttonRefs.clearButton,
            numberButtons,
            lettersLayout,
            numbersLayout,
            buttonRefs.switchToNumbersButton,
            buttonRefs.switchToLettersButton
        );
    }
}

public class VRKeyboardButtonReferences : MonoBehaviour
{
    public Button spaceButton;
    public Button backspaceButton;
    public Button enterButton;
    public Button shiftButton;
    public Button closeButton;
    public Button clearButton;
    public Button switchToNumbersButton;
    public Button switchToLettersButton;
    public TextMeshProUGUI displayText;
}


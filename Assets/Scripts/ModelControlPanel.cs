using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ModelControlPanel : MonoBehaviour
{
    [Header("Panel Toggle")]
    [SerializeField] private Button modelButton;
    [SerializeField] private GameObject modelPanel;

    [Header("Model Controls")]
    [SerializeField] private TMP_Dropdown modelDropdown;
    [SerializeField] private Button placeButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button deleteLastButton;
    [SerializeField] private Button deleteAllButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI selectedModelText;

    [Header("Text Settings")]
    [SerializeField] private float modelTextSize = 16f;
    [SerializeField] private float statusTextSize = 18f;
    [SerializeField] private float dropdownItemTextSize = 16f;

    private ModelManager modelManager;
    private ModelPlacer modelPlacer;
    private bool isPanelVisible = false;

    void Awake()
    {
        Debug.Log("ModelControlPanel: Awake() called");
        if (modelPanel != null)
        {
            modelPanel.SetActive(false);
            isPanelVisible = false;
        }
        else
        {
            FindButtonAndPanelReferences(false);
            if (modelPanel != null)
            {
                modelPanel.SetActive(false);
                isPanelVisible = false;
            }
        }
    }


    void Start()
    {
        Debug.Log("ModelControlPanel: Start() called");
        
        if (modelButton == null || modelPanel == null)
        {
            Debug.LogWarning("ModelControlPanel: Button or panel references are null, trying to find them...");
            FindButtonAndPanelReferences();
        }
        
        if (modelPanel != null)
        {
            modelPanel.SetActive(false);
            isPanelVisible = false;
            Debug.Log($"ModelControlPanel: Panel initialized and set to inactive. Panel active: {modelPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("ModelControlPanel: modelPanel is null in Start()! Panel will not work.");
        }

        if (modelButton != null)
        {
            Transform current = modelButton.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            modelButton.gameObject.SetActive(true);
            
            modelButton.onClick.RemoveAllListeners();
            modelButton.onClick.AddListener(TogglePanel);
            Debug.Log($"ModelControlPanel: Model button listener attached. Button: {modelButton.name}, Interactable: {modelButton.interactable}");
        }
        else
        {
            Debug.LogError("ModelControlPanel: Model button is not assigned! Button will not work.");
        }

        InitializeModelManager();
        InitializeModelPlacer();
        SetupControls();
        SetTextSizes();
    }
    
    void OnEnable()
    {
        if (modelButton != null)
        {
            Transform current = modelButton.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            modelButton.gameObject.SetActive(true);
        }
        
        if (modelPanel != null)
        {
            modelPanel.SetActive(false);
            isPanelVisible = false;
            Transform current = modelPanel.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
        }
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"ModelControlPanel: OnSceneLoaded() called for scene '{scene.name}' - re-initializing");
        
        modelButton = null;
        modelPanel = null;
        modelDropdown = null;
        placeButton = null;
        cancelButton = null;
        deleteLastButton = null;
        deleteAllButton = null;
        statusText = null;
        
        StartCoroutine(DelayedReinitialize(scene.name));
    }
    
    private bool ShouldHavePanelsInScene(string sceneName)
    {
        string[] scenesWithPanels = { "DrawingScene", "BeachScene", "ForestScene", "WordForestScene", "RoomScene" };
        foreach (string scene in scenesWithPanels)
        {
            if (sceneName.Contains(scene) || sceneName == scene)
            {
                return true;
            }
        }
        return false;
    }
    
    private IEnumerator DelayedReinitialize(string sceneName)
    {
        yield return null;
        yield return null;
        
        bool shouldHavePanels = ShouldHavePanelsInScene(sceneName);
        
        FindButtonAndPanelReferences();
        
        if (shouldHavePanels && (modelButton == null || modelPanel == null))
        {
            yield return new WaitForSeconds(0.1f);
            FindButtonAndPanelReferences();
        }
        
        if (shouldHavePanels && (modelButton == null || modelPanel == null))
        {
            yield return new WaitForSeconds(0.1f);
            FindButtonAndPanelReferences();
        }
        
        if (modelButton != null)
        {
            Transform current = modelButton.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                
                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null && !canvas.enabled)
                {
                    canvas.enabled = true;
                }
                
                current = current.parent;
            }
            
            modelButton.gameObject.SetActive(true);
        }
        
        if (modelPanel != null)
        {
            Transform panelCurrent = modelPanel.transform.parent;
            while (panelCurrent != null)
            {
                if (!panelCurrent.gameObject.activeSelf)
                {
                    panelCurrent.gameObject.SetActive(true);
                }
                panelCurrent = panelCurrent.parent;
            }
            
            modelPanel.SetActive(false);
            isPanelVisible = false;
            Debug.Log($"ModelControlPanel: Panel reset after scene load. Panel active: {modelPanel.activeSelf}");
        }
        else
        {
            if (shouldHavePanels)
            {
                Debug.LogError($"ModelControlPanel: modelPanel is still null after scene load in '{sceneName}'!");
            }
            else
            {
                Debug.Log($"ModelControlPanel: modelPanel not found in scene '{sceneName}' (expected - panels don't exist in this scene)");
            }
        }
        
        if (modelButton != null)
        {
            modelButton.onClick.RemoveAllListeners();
            modelButton.onClick.AddListener(TogglePanel);
            Debug.Log($"ModelControlPanel: Button listener re-attached after scene load. Button: {modelButton.name}, Interactable: {modelButton.interactable}");
        }
        else
        {
            if (shouldHavePanels)
            {
                Debug.LogError($"ModelControlPanel: modelButton is still null after scene load in '{sceneName}'! Trying to find it...");
                FindButtonAndPanelReferences();
                if (modelButton != null)
                {
                    modelButton.onClick.RemoveAllListeners();
                    modelButton.onClick.AddListener(TogglePanel);
                }
            }
            else
            {
                Debug.Log($"ModelControlPanel: modelButton not found in scene '{sceneName}' (expected - buttons don't exist in this scene)");
            }
        }
        
        if (modelPanel == null && modelButton != null && shouldHavePanels)
        {
            Debug.LogWarning($"ModelControlPanel: modelPanel is null but button was found. Trying to find panel near button...");
            FindButtonAndPanelReferences();
        }
        
        if (modelPanel != null)
        {
            FindUIElementsInPanel();
        }
        else if (shouldHavePanels)
        {
            Debug.LogWarning($"ModelControlPanel: modelPanel is still null after all attempts in scene '{sceneName}'. Panel may not be visible.");
        }
        
        InitializeModelManager();
        InitializeModelPlacer();
        SetupControls();
    }
    
    private void FindButtonAndPanelReferences(bool verbose = true)
    {
        if (modelButton == null)
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);
            
            foreach (Button btn in allButtons)
            {
                string btnName = btn.name.ToLower();
                if ((btnName.Contains("model") || btnName.Contains("modell")) && 
                    (btnName.Contains("button") || btnName.Contains("btn")))
                {
                    modelButton = btn;
                    break;
                }
            }
            
            if (modelButton == null)
            {
                foreach (Button btn in allButtons)
                {
                    string btnName = btn.name.ToLower();
                    if (btnName.Contains("model") || btnName.Contains("modell"))
                    {
                        modelButton = btn;
                        break;
                    }
                }
            }
        }
        
        if (modelPanel == null)
        {
            if (modelButton != null)
            {
                Transform buttonParent = modelButton.transform.parent;
                if (buttonParent != null)
                {
                    foreach (Transform child in buttonParent)
                    {
                        string childName = child.name.ToLower();
                        if ((childName.Contains("panel") || childName.Contains("control")) && 
                            childName.Contains("model"))
                        {
                            modelPanel = child.gameObject;
                            break;
                        }
                    }
                    
                    if (modelPanel == null && buttonParent.parent != null)
                    {
                        foreach (Transform sibling in buttonParent.parent)
                        {
                            string siblingName = sibling.name.ToLower();
                            if ((siblingName.Contains("panel") || siblingName.Contains("control")) && 
                                siblingName.Contains("model"))
                            {
                                modelPanel = sibling.gameObject;
                                break;
                            }
                        }
                    }
                }
            }
            
            if (modelPanel == null)
            {
                string[] panelNames = { "ModelPanel", "Model Control Panel", "ModelPanel", "Model" };
                foreach (string name in panelNames)
                {
                    GameObject found = GameObject.Find(name);
                    if (found != null)
                    {
                        modelPanel = found;
                        Debug.Log($"ModelControlPanel: Found model panel by exact name: {name}");
                        break;
                    }
                }
            }
            
            if (modelPanel == null)
            {
                TMP_Dropdown[] allDropdowns = FindObjectsOfType<TMP_Dropdown>(true);
                foreach (TMP_Dropdown dropdown in allDropdowns)
                {
                    if (dropdown.name.ToLower().Contains("model"))
                    {
                        Transform current = dropdown.transform;
                        while (current != null)
                        {
                            string currentName = current.name.ToLower();
                            if (currentName.Contains("panel") || currentName.Contains("control"))
                            {
                                modelPanel = current.gameObject;
                                Debug.Log($"ModelControlPanel: Found model panel via dropdown parent: {current.name}");
                                break;
                            }
                            current = current.parent;
                        }
                        if (modelPanel != null) break;
                    }
                }
            }
            
            if (modelPanel == null)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
                foreach (GameObject obj in allObjects)
                {
                    string objName = obj.name.ToLower();
                    if ((objName.Contains("model") && objName.Contains("panel")) || 
                        (objName.Contains("model") && objName.Contains("control")))
                    {
                        if (obj.GetComponent<Canvas>() != null || 
                            obj.GetComponent<RectTransform>() != null ||
                            obj.transform.childCount > 0)
                        {
                            modelPanel = obj;
                            Debug.Log($"ModelControlPanel: Found model panel by search: {obj.name}");
                            break;
                        }
                    }
                }
            }
        }
    }

    private void FindUIElementsInPanel()
    {
        if (modelPanel == null) return;
        
        if (modelDropdown == null)
        {
            modelDropdown = modelPanel.GetComponentInChildren<TMP_Dropdown>(true);
            if (modelDropdown != null)
            {
                Debug.Log($"ModelControlPanel: Found model dropdown: {modelDropdown.name}");
            }
        }
        
        if (placeButton == null)
        {
            Button[] buttons = modelPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                string btnName = btn.name.ToLower();
                if (btnName.Contains("place"))
                {
                    placeButton = btn;
                }
                else if (btnName.Contains("cancel"))
                {
                    cancelButton = btn;
                }
                else if (btnName.Contains("delete") && btnName.Contains("last"))
                {
                    deleteLastButton = btn;
                }
                else if (btnName.Contains("delete") && btnName.Contains("all"))
                {
                    deleteAllButton = btn;
                }
            }
        }
        
        if (statusText == null || selectedModelText == null)
        {
            TextMeshProUGUI[] texts = modelPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                string textName = text.name.ToLower();
                if (textName.Contains("status"))
                {
                    statusText = text;
                    Debug.Log($"ModelControlPanel: Found status text: {text.name}");
                }
                else if (textName.Contains("selected") || textName.Contains("model"))
                {
                    selectedModelText = text;
                    Debug.Log($"ModelControlPanel: Found selected model text: {text.name}");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (modelButton != null)
        {
            modelButton.onClick.RemoveAllListeners();
        }

        if (placeButton != null)
        {
            placeButton.onClick.RemoveAllListeners();
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
        }

        if (deleteLastButton != null)
        {
            deleteLastButton.onClick.RemoveAllListeners();
        }

        if (deleteAllButton != null)
        {
            deleteAllButton.onClick.RemoveAllListeners();
        }

        if (modelDropdown != null)
        {
            modelDropdown.onValueChanged.RemoveAllListeners();
        }
    }

    private void SetTextSizes()
    {
        if (selectedModelText != null)
        {
            selectedModelText.fontSize = modelTextSize;
            selectedModelText.enableAutoSizing = false;
        }

        if (statusText != null)
        {
            statusText.fontSize = statusTextSize;
            statusText.enableAutoSizing = false;
        }

        FixDropdownItemTextSizes();
    }

    private float lastButtonCheckTime = 0f;
    private const float BUTTON_CHECK_INTERVAL = 2f; // Check every 2 seconds

    void Update()
    {
        if (Time.time - lastButtonCheckTime > BUTTON_CHECK_INTERVAL)
        {
            lastButtonCheckTime = Time.time;
            if (modelButton != null)
            {
            }
            else
            {
                FindButtonAndPanelReferences(verbose: false);
                if (modelButton != null)
                {
                    modelButton.onClick.RemoveAllListeners();
                    modelButton.onClick.AddListener(TogglePanel);
                }
            }
            
            if (modelPanel == null)
            {
                FindButtonAndPanelReferences();
            }
        }
        
        if (modelDropdown != null && modelDropdown.captionText != null)
        {
            if (modelDropdown.captionText.fontSize != modelTextSize)
            {
                modelDropdown.captionText.fontSize = modelTextSize;
                modelDropdown.captionText.enableAutoSizing = false;
                modelDropdown.captionText.fontSizeMin = modelTextSize;
                modelDropdown.captionText.fontSizeMax = modelTextSize;
            }
        }

        if (modelDropdown != null && modelDropdown.IsExpanded)
        {
            FixDropdownItemTextSizes();
        }
    }

    private void InitializeModelManager()
    {
        modelManager = ModelManager.Instance;
        if (modelManager == null)
        {
            modelManager = FindObjectOfType<ModelManager>();
            if (modelManager == null)
            {
                GameObject modelManagerObj = new GameObject("ModelManager");
                modelManager = modelManagerObj.AddComponent<ModelManager>();
            }
        }

        StartCoroutine(InitializeModelDropdownWhenReady());
    }

    private void InitializeModelPlacer()
    {
        modelPlacer = FindObjectOfType<ModelPlacer>();
        if (modelPlacer == null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            bool shouldHavePlacer = ShouldHavePanelsInScene(currentSceneName);
            
            if (shouldHavePlacer)
            {
                Debug.LogWarning($"ModelControlPanel: ModelPlacer not found in scene '{currentSceneName}'. Models cannot be placed without it.");
            }
            else
            {
                Debug.Log($"ModelControlPanel: ModelPlacer not found in scene '{currentSceneName}' (expected - ModelPlacer doesn't exist in this scene)");
            }
        }
    }

    private void SetupControls()
    {
        if (placeButton != null)
        {
            placeButton.onClick.RemoveAllListeners();
            placeButton.onClick.AddListener(OnPlaceButtonClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
        
        if (deleteLastButton != null)
        {
            deleteLastButton.onClick.RemoveAllListeners();
            deleteLastButton.onClick.AddListener(OnDeleteLastButtonClicked);
        }
        
        if (deleteAllButton != null)
        {
            deleteAllButton.onClick.RemoveAllListeners();
            deleteAllButton.onClick.AddListener(OnDeleteAllButtonClicked);
        }
    }

    private IEnumerator InitializeModelDropdownWhenReady()
    {
        if (modelDropdown == null)
        {
            yield break;
        }

        if (modelManager == null)
        {
            yield break;
        }

        float timeout = 15f;
        float elapsed = 0f;
        while (modelManager.IsLoadingModels() && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        yield return null;
        yield return null;
        yield return null;

        RefreshDropdown();

        StartCoroutine(PeriodicDropdownRefresh());
    }

    private void RefreshDropdown()
    {
        if (modelDropdown == null || modelManager == null) return;

        List<string> modelNames = modelManager.GetModelNames();

        int currentSelection = modelDropdown.value;

        modelDropdown.ClearOptions();

        if (modelNames.Count > 0)
        {
            modelDropdown.AddOptions(modelNames);

            modelDropdown.onValueChanged.RemoveAllListeners();

            if (currentSelection >= 0 && currentSelection < modelNames.Count)
            {
                modelDropdown.value = currentSelection;
            }
            else
            {
                modelDropdown.value = 0;
            }

            if (modelDropdown.captionText != null && modelNames.Count > 0)
            {
                int selectedIndex = modelDropdown.value;
                if (selectedIndex >= 0 && selectedIndex < modelNames.Count)
                {
                    modelDropdown.captionText.text = modelNames[selectedIndex];
                }
                modelDropdown.captionText.fontSize = modelTextSize;
                modelDropdown.captionText.enableAutoSizing = false;
                modelDropdown.captionText.fontSizeMin = modelTextSize;
                modelDropdown.captionText.fontSizeMax = modelTextSize;
            }

            modelDropdown.onValueChanged.AddListener(OnModelSelectionChanged);
            
            Debug.Log($"ModelControlPanel: Dropdown populated with {modelNames.Count} options, selected index: {modelDropdown.value}");
        }
    }

    private IEnumerator PeriodicDropdownRefresh()
    {
        yield return new WaitForSeconds(1f);

        int lastModelCount = 0;

        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (modelManager == null || modelDropdown == null) break;

            int currentModelCount = modelManager.GetModelCount();

            if (currentModelCount > lastModelCount)
            {
                RefreshDropdown();
                lastModelCount = currentModelCount;
            }

            if (!modelManager.IsLoadingModels() && lastModelCount > 0)
            {
                yield return new WaitForSeconds(0.5f);
                RefreshDropdown();
                break;
            }
        }
    }

    private void FixDropdownItemTextSizes()
    {
        if (modelDropdown == null) return;

        if (modelDropdown.captionText != null)
        {
            modelDropdown.captionText.fontSize = modelTextSize;
            modelDropdown.captionText.enableAutoSizing = false;
            modelDropdown.captionText.fontSizeMin = modelTextSize;
            modelDropdown.captionText.fontSizeMax = modelTextSize;
        }

        var template = modelDropdown.template;
        if (template != null && template.gameObject.activeInHierarchy)
        {
            var allTexts = template.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text == modelDropdown.captionText) continue;

                text.fontSize = dropdownItemTextSize;
                text.enableAutoSizing = false;
                text.fontSizeMin = dropdownItemTextSize;
                text.fontSizeMax = dropdownItemTextSize;
            }

            var itemTransform = template.Find("Viewport/Content/Item");
            if (itemTransform == null)
            {
                itemTransform = template.Find("Content/Item");
            }
            if (itemTransform == null)
            {
                var content = template.GetComponentInChildren<RectTransform>();
                if (content != null && content.name.Contains("Content"))
                {
                    var items = content.GetComponentsInChildren<RectTransform>();
                    foreach (var item in items)
                    {
                        if (item.name.Contains("Item"))
                        {
                            var itemText = item.GetComponentInChildren<TextMeshProUGUI>(true);
                            if (itemText != null)
                            {
                                itemText.fontSize = dropdownItemTextSize;
                                itemText.enableAutoSizing = false;
                                itemText.fontSizeMin = dropdownItemTextSize;
                                itemText.fontSizeMax = dropdownItemTextSize;
                            }
                        }
                    }
                }
            }
            else
            {
                var itemText = itemTransform.GetComponentInChildren<TextMeshProUGUI>(true);
                if (itemText != null)
                {
                    itemText.fontSize = dropdownItemTextSize;
                    itemText.enableAutoSizing = false;
                    itemText.fontSizeMin = dropdownItemTextSize;
                    itemText.fontSizeMax = dropdownItemTextSize;
                }
            }
        }
    }

    private void TogglePanel()
    {
        Debug.Log($"ModelControlPanel: TogglePanel() called. modelPanel is {(modelPanel != null ? "not null" : "NULL")}, isPanelVisible: {isPanelVisible}");
        
        if (modelPanel == null)
        {
            Debug.LogWarning("ModelControlPanel: modelPanel is null, trying to find it...");
            FindButtonAndPanelReferences();
            if (modelPanel == null)
            {
                Debug.LogError("ModelControlPanel: Could not find modelPanel in scene!");
                return;
            }
        }
        
        if (modelPanel != null)
        {
            Transform parent = modelPanel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    Debug.LogWarning($"ModelControlPanel: Parent '{parent.name}' is inactive, activating it...");
                    parent.gameObject.SetActive(true);
                }
                parent = parent.parent;
            }
            
            isPanelVisible = !isPanelVisible;
            modelPanel.SetActive(isPanelVisible);
            Debug.Log($"ModelControlPanel: Panel set to {(isPanelVisible ? "ACTIVE" : "INACTIVE")}, actual active state: {modelPanel.activeSelf}, activeInHierarchy: {modelPanel.activeInHierarchy}");

            if (isPanelVisible)
            {
                RefreshDropdown();
                
                if (modelDropdown != null && modelDropdown.captionText != null && modelManager != null)
                {
                    List<string> modelNames = modelManager.GetModelNames();
                    if (modelNames.Count > 0 && modelDropdown.value >= 0 && modelDropdown.value < modelNames.Count)
                    {
                        modelDropdown.captionText.text = modelNames[modelDropdown.value];
                    }
                }
            }
        }
        else
        {
            Debug.LogError("ModelControlPanel: TogglePanel() called but modelPanel is null!");
        }
    }

    private void OnPlaceButtonClicked()
    {
        if (modelManager == null || modelPlacer == null)
        {
            if (statusText != null)
            {
                statusText.text = "Error: ModelPlacer not found";
            }
            return;
        }

        int selectedIndex = modelManager.GetSelectedModelIndex();
        if (selectedIndex <= 0)
        {
            if (statusText != null)
            {
                statusText.text = "Please select a model first";
            }
            return;
        }

        modelPlacer.EnablePlacementMode();
        if (statusText != null)
        {
            statusText.text = "Point and click to place model";
        }
    }

    private void OnCancelButtonClicked()
    {
        if (modelPlacer == null)
        {
            if (statusText != null)
            {
                statusText.text = "Error: ModelPlacer not found";
            }
            return;
        }

        modelPlacer.DisablePlacementMode();
        if (statusText != null)
        {
            statusText.text = "Placement cancelled";
        }
    }

    private void OnDeleteLastButtonClicked()
    {
        if (modelPlacer == null)
        {
            if (statusText != null)
            {
                statusText.text = "Error: ModelPlacer not found";
            }
            return;
        }

        int countBefore = modelPlacer.GetPlacedModelCount();
        modelPlacer.DeleteLastPlacedModel();
        int countAfter = modelPlacer.GetPlacedModelCount();
        
        if (statusText != null)
        {
            if (countBefore > countAfter)
            {
                statusText.text = $"Deleted last model ({countAfter} remaining)";
            }
            else
            {
                statusText.text = "No models to delete";
            }
        }
    }

    private void OnDeleteAllButtonClicked()
    {
        if (modelPlacer == null)
        {
            if (statusText != null)
            {
                statusText.text = "Error: ModelPlacer not found";
            }
            return;
        }

        int countBefore = modelPlacer.GetPlacedModelCount();
        modelPlacer.DeleteAllPlacedModels();
        
        if (statusText != null)
        {
            if (countBefore > 0)
            {
                statusText.text = $"Deleted all {countBefore} model(s)";
            }
            else
            {
                statusText.text = "No models to delete";
            }
        }
    }

    private void OnModelSelectionChanged(int selectedIndex)
    {
        if (modelManager == null) return;

        modelManager.SetSelectedModelIndex(selectedIndex);

        if (modelDropdown != null && modelDropdown.captionText != null)
        {
            modelDropdown.captionText.fontSize = modelTextSize;
            modelDropdown.captionText.enableAutoSizing = false;
            modelDropdown.captionText.fontSizeMin = modelTextSize;
            modelDropdown.captionText.fontSizeMax = modelTextSize;
        }

        if (selectedIndex == 0)
        {
            if (selectedModelText != null)
            {
                selectedModelText.text = "No model selected";
            }
            if (statusText != null)
            {
                statusText.text = "Select a model to place";
            }
        }
        else
        {
            string modelName = modelManager.GetModelNames()[selectedIndex];
            if (selectedModelText != null)
            {
                selectedModelText.text = $"Selected: {modelName}";
            }
            if (statusText != null)
            {
                statusText.text = "Ready to place";
            }
        }
    }
}

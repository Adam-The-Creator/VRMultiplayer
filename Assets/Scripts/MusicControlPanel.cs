using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MusicControlPanel : MonoBehaviour
{
    [Header("Panel Toggle")]
    [SerializeField] private Button musicButton; 
    [SerializeField] private GameObject musicPanel; 

    [Header("Music Controls")]
    [SerializeField] private TMP_Dropdown musicDropdown;
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private TextMeshProUGUI currentTrackText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Text Settings")]
    [SerializeField] private float volumeTextSize = 24f;
    [SerializeField] private float trackTextSize = 16f; 
    [SerializeField] private float statusTextSize = 18f;
    [SerializeField] private float dropdownItemTextSize = 16f; 

    private MusicManager musicManager;
    private bool isPanelVisible = false;

    void Awake()
    {
        Debug.Log("MusicControlPanel: Awake() called");
        if (musicPanel != null)
        {
            musicPanel.SetActive(false);
            isPanelVisible = false;
        }
        else
        {
            FindButtonAndPanelReferences();
            if (musicPanel != null)
            {
                musicPanel.SetActive(false);
                isPanelVisible = false;
            }
        }
    }


    void Start()
    {
        Debug.Log("MusicControlPanel: Start() called");
        
        if (musicButton == null || musicPanel == null)
        {
            Debug.LogWarning("MusicControlPanel: Button or panel references are null, trying to find them...");
            FindButtonAndPanelReferences();
        }
        
        if (musicPanel != null)
        {
            musicPanel.SetActive(false);
            isPanelVisible = false;
            Debug.Log($"MusicControlPanel: Panel initialized and set to inactive. Panel active: {musicPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("MusicControlPanel: musicPanel is null in Start()! Panel will not work.");
        }

        if (musicButton != null)
        {
            Transform current = musicButton.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            musicButton.gameObject.SetActive(true);
            
            musicButton.onClick.RemoveAllListeners();
            musicButton.onClick.AddListener(TogglePanel);
            Debug.Log($"MusicControlPanel: Music button listener attached. Button: {musicButton.name}, Interactable: {musicButton.interactable}");
        }
        else
        {
            Debug.LogError("MusicControlPanel: Music button is not assigned! Button will not work.");
        }

        InitializeMusicManager();

        SetupControls();
        
        SetTextSizes();
    }
    
    void OnEnable()
    {
        if (musicButton != null)
        {
            Transform current = musicButton.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            musicButton.gameObject.SetActive(true);
        }
        
        if (musicPanel != null)
        {
            musicPanel.SetActive(false);
            isPanelVisible = false;
            Transform current = musicPanel.transform;
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
        Debug.Log($"MusicControlPanel: OnSceneLoaded() called for scene '{scene.name}' - re-initializing");
        
        musicButton = null;
        musicPanel = null;
        musicDropdown = null;
        playButton = null;
        pauseButton = null;
        volumeSlider = null;
        volumeValueText = null;
        currentTrackText = null;
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
        
        if (musicButton != null)
        {
            Transform current = musicButton.transform;
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
            
            musicButton.gameObject.SetActive(true);
        }
        
        if (musicPanel != null)
        {
            Transform panelCurrent = musicPanel.transform.parent;
            while (panelCurrent != null)
            {
                if (!panelCurrent.gameObject.activeSelf)
                {
                    panelCurrent.gameObject.SetActive(true);
                }
                panelCurrent = panelCurrent.parent;
            }
            
            musicPanel.SetActive(false);
            isPanelVisible = false;
            Debug.Log($"MusicControlPanel: Panel reset after scene load. Panel active: {musicPanel.activeSelf}");
        }
        else
        {
            if (shouldHavePanels)
            {
                Debug.LogError($"MusicControlPanel: musicPanel is still null after scene load in '{sceneName}'!");
            }
            else
            {
                Debug.Log($"MusicControlPanel: musicPanel not found in scene '{sceneName}' (expected - panels don't exist in this scene)");
            }
        }
        
        if (musicButton != null)
        {
            musicButton.onClick.RemoveAllListeners();
            musicButton.onClick.AddListener(TogglePanel);
            Debug.Log($"MusicControlPanel: Button listener re-attached after scene load. Button: {musicButton.name}, Interactable: {musicButton.interactable}");
        }
        else
        {
            if (shouldHavePanels)
            {
                Debug.LogError($"MusicControlPanel: musicButton is still null after scene load in '{sceneName}'! Trying to find it...");
                FindButtonAndPanelReferences();
                if (musicButton != null)
                {
                    musicButton.onClick.RemoveAllListeners();
                    musicButton.onClick.AddListener(TogglePanel);
                }
            }
            else
            {
                Debug.Log($"MusicControlPanel: musicButton not found in scene '{sceneName}' (expected - buttons don't exist in this scene)");
            }
        }
        
        if (musicPanel == null && musicButton != null && shouldHavePanels)
        {
            Debug.LogWarning($"MusicControlPanel: musicPanel is null but button was found. Trying to find panel near button...");
            FindButtonAndPanelReferences();
        }
        
        if (musicPanel != null)
        {
            FindUIElementsInPanel();
        }
        else if (shouldHavePanels)
        {
            Debug.LogWarning($"MusicControlPanel: musicPanel is still null after all attempts in scene '{sceneName}'. Panel may not be visible.");
        }
        
        InitializeMusicManager();
        SetupControls();
    }
    
    private void FindButtonAndPanelReferences()
    {
        if (musicButton == null)
        {
            Button[] allButtons = FindObjectsOfType<Button>(true);
            foreach (Button btn in allButtons)
            {
                if (btn.name.Contains("Music") && (btn.name.Contains("Button") || btn.name.Contains("Btn")))
                {
                    musicButton = btn;
                    break;
                }
            }
        }
        
        if (musicPanel == null)
        {
            if (musicButton != null)
            {
                Transform buttonParent = musicButton.transform.parent;
                if (buttonParent != null)
                {
                    foreach (Transform child in buttonParent)
                    {
                        string childName = child.name.ToLower();
                        if ((childName.Contains("panel") || childName.Contains("control")) && 
                            childName.Contains("music"))
                        {
                            musicPanel = child.gameObject;
                            break;
                        }
                    }
                    
                    if (musicPanel == null && buttonParent.parent != null)
                    {
                        foreach (Transform sibling in buttonParent.parent)
                        {
                            string siblingName = sibling.name.ToLower();
                            if ((siblingName.Contains("panel") || siblingName.Contains("control")) && 
                                siblingName.Contains("music"))
                            {
                                musicPanel = sibling.gameObject;
                                break;
                            }
                        }
                    }
                }
            }
            
            if (musicPanel == null)
            {
                string[] panelNames = { "MusicPanel", "Music Control Panel", "MusicPanel", "Music" };
                foreach (string name in panelNames)
                {
                    GameObject found = GameObject.Find(name);
                    if (found != null)
                    {
                        musicPanel = found;
                        Debug.Log($"MusicControlPanel: Found music panel by exact name: {name}");
                        break;
                    }
                }
            }
            
            if (musicPanel == null)
            {
                TMP_Dropdown[] allDropdowns = FindObjectsOfType<TMP_Dropdown>(true);
                foreach (TMP_Dropdown dropdown in allDropdowns)
                {
                    if (dropdown.name.ToLower().Contains("music"))
                    {
                        Transform current = dropdown.transform;
                        while (current != null)
                        {
                            string currentName = current.name.ToLower();
                            if (currentName.Contains("panel") || currentName.Contains("control"))
                            {
                                musicPanel = current.gameObject;
                                Debug.Log($"MusicControlPanel: Found music panel via dropdown parent: {current.name}");
                                break;
                            }
                            current = current.parent;
                        }
                        if (musicPanel != null) break;
                    }
                }
            }
            
            if (musicPanel == null)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
                foreach (GameObject obj in allObjects)
                {
                    string objName = obj.name.ToLower();
                    if ((objName.Contains("music") && objName.Contains("panel")) || 
                        (objName.Contains("music") && objName.Contains("control")))
                    {
                        if (obj.GetComponent<Canvas>() != null || 
                            obj.GetComponent<RectTransform>() != null ||
                            obj.transform.childCount > 0)
                        {
                            musicPanel = obj;
                            Debug.Log($"MusicControlPanel: Found music panel by search: {obj.name}");
                            break;
                        }
                    }
                }
            }
        }
    }

    private void FindUIElementsInPanel()
    {
        if (musicPanel == null) return;
        
        if (musicDropdown == null)
        {
            musicDropdown = musicPanel.GetComponentInChildren<TMP_Dropdown>(true);
            if (musicDropdown != null)
            {
                Debug.Log($"MusicControlPanel: Found music dropdown: {musicDropdown.name}");
            }
        }
        
        if (playButton == null || pauseButton == null)
        {
            Button[] buttons = musicPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                string btnName = btn.name.ToLower();
                if (btnName.Contains("play"))
                {
                    playButton = btn;
                }
                else if (btnName.Contains("pause") || btnName.Contains("stop"))
                {
                    pauseButton = btn;
                }
            }
        }
        
        if (volumeSlider == null)
        {
            volumeSlider = musicPanel.GetComponentInChildren<Slider>(true);
            if (volumeSlider != null)
            {
                Debug.Log($"MusicControlPanel: Found volume slider: {volumeSlider.name}");
            }
        }
        
        if (volumeValueText == null || currentTrackText == null || statusText == null)
        {
            TextMeshProUGUI[] texts = musicPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                string textName = text.name.ToLower();
                if (textName.Contains("volume") && textName.Contains("value"))
                {
                    volumeValueText = text;
                    Debug.Log($"MusicControlPanel: Found volume value text: {text.name}");
                }
                else if (textName.Contains("track") || textName.Contains("current"))
                {
                    currentTrackText = text;
                    Debug.Log($"MusicControlPanel: Found current track text: {text.name}");
                }
                else if (textName.Contains("status"))
                {
                    statusText = text;
                    Debug.Log($"MusicControlPanel: Found status text: {text.name}");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (musicButton != null)
        {
            musicButton.onClick.RemoveAllListeners();
        }

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
        }

        if (musicDropdown != null)
        {
            musicDropdown.onValueChanged.RemoveAllListeners();
        }
    }

    private void SetTextSizes()
    {
        if (volumeValueText != null)
        {
            volumeValueText.fontSize = volumeTextSize;
            volumeValueText.enableAutoSizing = false;
        }
        
        if (currentTrackText != null)
        {
            currentTrackText.fontSize = trackTextSize;
            currentTrackText.enableAutoSizing = false;
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
        UpdateUI();
        
        if (Time.time - lastButtonCheckTime > BUTTON_CHECK_INTERVAL)
        {
            lastButtonCheckTime = Time.time;
            if (musicButton != null)
            {
            }
            else
            {
                FindButtonAndPanelReferences();
                if (musicButton != null)
                {
                    musicButton.onClick.RemoveAllListeners();
                    musicButton.onClick.AddListener(TogglePanel);
                }
            }
            
            if (musicPanel == null)
            {
                FindButtonAndPanelReferences();
            }
        }
        
        if (musicDropdown != null && musicDropdown.captionText != null)
        {
            if (musicDropdown.captionText.fontSize != trackTextSize)
            {
                musicDropdown.captionText.fontSize = trackTextSize;
                musicDropdown.captionText.enableAutoSizing = false;
                musicDropdown.captionText.fontSizeMin = trackTextSize;
                musicDropdown.captionText.fontSizeMax = trackTextSize;
            }
        }
        
        if (musicDropdown != null && musicDropdown.IsExpanded)
        {
            FixDropdownItemTextSizes();
        }
    }

    private void InitializeMusicManager()
    {
        musicManager = MusicManager.Instance;
        if (musicManager == null)
        {
            musicManager = FindObjectOfType<MusicManager>();
            if (musicManager == null)
            {
                GameObject musicManagerObj = new GameObject("MusicManager");
                musicManager = musicManagerObj.AddComponent<MusicManager>();
                Debug.Log("MusicControlPanel: Created MusicManager instance");
            }
        }

        StartCoroutine(InitializeMusicDropdownWhenReady());
    }

    private void SetupControls()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();

            if (musicManager != null)
            {
                volumeSlider.value = musicManager.GetVolume();
            }
            else
            {
                volumeSlider.value = 0.5f;
            }

            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            UpdateVolumeText();
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
    }

    private IEnumerator InitializeMusicDropdownWhenReady()
    {
        if (musicDropdown == null)
        {
            yield break;
        }

        if (musicManager == null)
        {
            yield break;
        }

        float timeout = 15f; 
        float elapsed = 0f;
        while (musicManager.IsLoadingMusic() && elapsed < timeout)
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
        if (musicDropdown == null || musicManager == null) return;

        List<string> trackNames = musicManager.GetMusicTrackNames();
        
        Debug.Log($"MusicControlPanel: Refreshing dropdown with {trackNames.Count} tracks (including 'None')");
        Debug.Log($"MusicControlPanel: Track names: {string.Join(", ", trackNames)}");
        
        int currentSelection = musicDropdown.value;
        
        musicDropdown.ClearOptions();
        
        if (trackNames.Count > 0)
        {
            musicDropdown.AddOptions(trackNames);
            
            if (musicDropdown.captionText != null)
            {
                musicDropdown.captionText.fontSize = trackTextSize;
                musicDropdown.captionText.enableAutoSizing = false;
                musicDropdown.captionText.fontSizeMin = trackTextSize;
                musicDropdown.captionText.fontSizeMax = trackTextSize;
            }
            
            musicDropdown.onValueChanged.RemoveAllListeners();
            
            if (currentSelection >= 0 && currentSelection < trackNames.Count)
            {
                musicDropdown.value = currentSelection;
            }
            else
            {
                musicDropdown.value = 0;
            }
            
            if (musicDropdown.captionText != null && trackNames.Count > 0)
            {
                int selectedIndex = musicDropdown.value;
                if (selectedIndex >= 0 && selectedIndex < trackNames.Count)
                {
                    musicDropdown.captionText.text = trackNames[selectedIndex];
                }
                musicDropdown.captionText.fontSize = trackTextSize;
                musicDropdown.captionText.enableAutoSizing = false;
                musicDropdown.captionText.fontSizeMin = trackTextSize;
                musicDropdown.captionText.fontSizeMax = trackTextSize;
            }
            
            musicDropdown.onValueChanged.AddListener(OnMusicSelectionChanged);
            
            Debug.Log($"MusicControlPanel: Dropdown populated with {trackNames.Count} options, selected index: {musicDropdown.value}");
        }
        else
        {
            Debug.LogWarning("MusicControlPanel: No track names available to populate dropdown");
        }
    }

    private IEnumerator PeriodicDropdownRefresh()
    {
        yield return new WaitForSeconds(1f);
        
        int lastTrackCount = 0;
        
        while (true)
        {
            yield return new WaitForSeconds(0.5f); 
            
            if (musicManager == null || musicDropdown == null) break;
            
            int currentTrackCount = musicManager.GetTrackCount();
            
            if (currentTrackCount > lastTrackCount)
            {
                Debug.Log($"MusicControlPanel: Track count increased from {lastTrackCount} to {currentTrackCount}, refreshing dropdown");
                RefreshDropdown();
                lastTrackCount = currentTrackCount;
            }
            
            if (!musicManager.IsLoadingMusic() && lastTrackCount > 0)
            {
                yield return new WaitForSeconds(0.5f);
                RefreshDropdown();
                break;
            }
        }
    }

    private void FixDropdownItemTextSizes()
    {
        if (musicDropdown == null) return;

        if (musicDropdown.captionText != null)
        {
            musicDropdown.captionText.fontSize = trackTextSize;
            musicDropdown.captionText.enableAutoSizing = false;
            musicDropdown.captionText.fontSizeMin = trackTextSize;
            musicDropdown.captionText.fontSizeMax = trackTextSize;
        }

        var template = musicDropdown.template;
        if (template != null && template.gameObject.activeInHierarchy)
        {
            var allTexts = template.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in allTexts)
            {
                if (text == musicDropdown.captionText) continue;
                
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
        Debug.Log($"MusicControlPanel: TogglePanel() called. musicPanel is {(musicPanel != null ? "not null" : "NULL")}, isPanelVisible: {isPanelVisible}");
        
        if (musicPanel == null)
        {
            Debug.LogWarning("MusicControlPanel: musicPanel is null, trying to find it...");
            FindButtonAndPanelReferences();
            if (musicPanel == null)
            {
                Debug.LogError("MusicControlPanel: Could not find musicPanel in scene!");
                return;
            }
        }
        
        if (musicPanel != null)
        {
            Transform parent = musicPanel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    Debug.LogWarning($"MusicControlPanel: Parent '{parent.name}' is inactive, activating it...");
                    parent.gameObject.SetActive(true);
                }
                parent = parent.parent;
            }
            
            isPanelVisible = !isPanelVisible;
            musicPanel.SetActive(isPanelVisible);
            Debug.Log($"MusicControlPanel: Panel set to {(isPanelVisible ? "ACTIVE" : "INACTIVE")}, actual active state: {musicPanel.activeSelf}, activeInHierarchy: {musicPanel.activeInHierarchy}");
            
            if (isPanelVisible)
            {
                RefreshDropdown();
                
                if (musicDropdown != null && musicDropdown.captionText != null && musicManager != null)
                {
                    List<string> trackNames = musicManager.GetMusicTrackNames();
                    if (trackNames.Count > 0 && musicDropdown.value >= 0 && musicDropdown.value < trackNames.Count)
                    {
                        musicDropdown.captionText.text = trackNames[musicDropdown.value];
                    }
                }
            }
        }
        else
        {
            Debug.LogError("MusicControlPanel: TogglePanel() called but musicPanel is null!");
        }
    }

    private void OnVolumeChanged(float value)
    {
        if (musicManager != null)
        {
            musicManager.SetVolume(value);
        }
        UpdateVolumeText();
    }

    private void UpdateVolumeText()
    {
        if (volumeValueText != null && volumeSlider != null)
        {
            volumeValueText.text = $"{Mathf.RoundToInt(volumeSlider.value * 100)}%";
        }
    }

    private void OnPlayButtonClicked()
    {
        if (musicManager == null) return;

        int currentIndex = musicManager.GetCurrentTrackIndex();
        
        if (musicManager.IsPaused())
        {
            musicManager.ResumeMusic();
            if (statusText != null)
            {
                statusText.text = "Resumed";
            }
        }
        else if (currentIndex > 0)
        {
            if (statusText != null)
            {
                statusText.text = "Already playing";
            }
        }
        else
        {
            if (musicDropdown != null && musicDropdown.options.Count > 1)
            {
                musicDropdown.value = 1; 
                OnMusicSelectionChanged(1);
            }
        }
    }

    private void OnPauseButtonClicked()
    {
        if (musicManager == null) return;

        if (musicManager.IsPlaying())
        {
            musicManager.PauseMusic();
            if (statusText != null)
            {
                statusText.text = "Paused";
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text = "Nothing playing";
            }
        }
    }

    private void OnMusicSelectionChanged(int selectedIndex)
    {
        if (musicManager == null) return;

        musicManager.PlayMusic(selectedIndex);

        if (musicDropdown != null && musicDropdown.captionText != null)
        {
            musicDropdown.captionText.fontSize = trackTextSize;
            musicDropdown.captionText.enableAutoSizing = false;
            musicDropdown.captionText.fontSizeMin = trackTextSize;
            musicDropdown.captionText.fontSizeMax = trackTextSize;
        }

        if (selectedIndex == 0)
        {
            if (currentTrackText != null)
            {
                currentTrackText.text = ""; 
            }
            if (statusText != null)
            {
                statusText.text = "Stopped";
            }
        }
        else
        {

            if (currentTrackText != null)
            {
                currentTrackText.text = ""; 
            }
            if (statusText != null)
            {
                statusText.text = "Playing";
            }
        }
    }

    private void UpdateUI()
    {
        if (musicManager == null) return;

        bool isPlaying = musicManager.IsPlaying();
        bool isPaused = musicManager.IsPaused();
        
        if (playButton != null)
        {
            playButton.interactable = true; 
        }

        if (pauseButton != null)
        {
            pauseButton.interactable = isPlaying; 
        }

    
        if (currentTrackText != null)
        {
            if (!isPlaying && !isPaused)
            {
                currentTrackText.text = ""; 
            }
        }

        if (statusText != null && !isPlaying && !isPaused)
        {
            int currentIndex = musicManager.GetCurrentTrackIndex();
            if (currentIndex <= 0)
            {
                statusText.text = "Stopped";
            }
        }
    }
}


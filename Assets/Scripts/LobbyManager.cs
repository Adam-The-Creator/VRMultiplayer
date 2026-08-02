using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;          
using UnityEngine.SceneManagement;
using DrawingData;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject drawingsPanel;
    [SerializeField] private GameObject playerInfoPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Game Type Selection")]
    [SerializeField] private TMP_Dropdown gameTypeDropdown; 
    [SerializeField] private Button startGameButton;
    //[SerializeField] private Toggle showSadChildToggle;
    [SerializeField] private Button exitButton;

    [Header("Drawings Panel Elements")]
    [SerializeField] private Button newDrawingButton;
    [SerializeField] private RectTransform drawingListContent;

    [Header("Player Info Panel Elements")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_InputField ageInput;
    [SerializeField] private TMP_Dropdown genderDropdown;
    [SerializeField] private TMP_Dropdown DominantHandDropdown;
    [SerializeField] private Button playerInfoSaveButton;
    [SerializeField] private Button logoutButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Drawing List Prefab")]
    [SerializeField] private GameObject drawingListItemPrefab; 

    private DatabaseManager databaseManager; 
    private string loggedInUserId = null; 
    private const string PlayerPrefSelectedGameType = "SelectedGameType";
    private const string PlayerPrefShowSadChild = "ShowSadChild";

    void Start()
    {
        databaseManager = DatabaseManager.Instance;
        AuthManager authManager = GameObject.FindGameObjectWithTag("AuthManager").GetComponent<AuthManager>();
        DataAnalysisManager analysisManager = GameObject.FindGameObjectWithTag("DataAnalysisManager").GetComponent<DataAnalysisManager>();

        authManager.InitializeEvents();

        authManager.OnLogIn += () =>
        {
            loggedInUserId = AuthManager.GetCurrentUserID();
            if (usernameText != null) usernameText.text = AuthManager.GetCurrentUsername();
            if (gameTypeDropdown != null) gameTypeDropdown.interactable = true;
            if (newDrawingButton != null) newDrawingButton.interactable = true;
            analysisManager.SetPanelsActive(AuthManager.GetCurrentUserRole() == DatabaseManager.Role.Psychologist);
            SwitchToDrawingsPanel();
            EnablePlayerInfoPanel(true);
        };

        authManager.OnLogout += () =>
        {
            loggedInUserId = null;
            if (drawingsPanel != null) drawingsPanel.SetActive(false);
            if (playerInfoPanel != null) EnablePlayerInfoPanel(false);
            if (usernameText != null) usernameText.text = "";
            if (gameTypeDropdown != null) gameTypeDropdown.interactable = false;
            if (newDrawingButton != null) newDrawingButton.interactable = false;
            if (drawingListContent != null) foreach (Transform child in drawingListContent) Destroy(child.gameObject);
            analysisManager.SetPanelsActive(false);
        };

        //Ensure input fields have VRKeyboardInputField component
        //SetupVRKeyboardForInputFields();

        if (newDrawingButton != null) newDrawingButton.onClick.AddListener(OnNewDrawingButtonClicked);
        else Debug.LogWarning("LobbyManager: New Drawing Button not assigned.");

        if (logoutButton != null) logoutButton.onClick.AddListener(() => authManager.Logout());
        else Debug.LogWarning("LobbyManager: Logout Button not assigned.");

        if (exitButton != null) exitButton.onClick.AddListener(() => Application.Quit());
        else Debug.LogWarning("LobbyManager: Exit Button not assigned.");

        //if (startGameButton != null) startGameButton.onClick.AddListener(StartSelectedGame);
        //else Debug.Log("LobbyManager: StartGame Button is not assigned (optional).");

        if (!AuthManager.IsLoggedIn())
        {
            drawingsPanel.SetActive(false);
            EnablePlayerInfoPanel(false);
            if (gameTypeDropdown != null) gameTypeDropdown.interactable = false;
            if (newDrawingButton != null) newDrawingButton.interactable = false;
            analysisManager.SetPanelsActive(false);
        }
        else
        {
            SwitchToDrawingsPanel();
            EnablePlayerInfoPanel(true);
            if (gameTypeDropdown != null) gameTypeDropdown.interactable = true;
            if (newDrawingButton != null) newDrawingButton.interactable = true;
            analysisManager.SetPanelsActive(AuthManager.GetCurrentUserRole() == DatabaseManager.Role.Psychologist);
        }

        gamePanel.SetActive(true);

        if (feedbackText != null) feedbackText.text = "";

        if (gameTypeDropdown != null)
        {
            gameTypeDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData("Drawing"),
                    new TMP_Dropdown.OptionData("Word Forest"),
                    new TMP_Dropdown.OptionData("Beach"),
                    new TMP_Dropdown.OptionData("Room")
                };
            gameTypeDropdown.AddOptions(options);

            int savedSelection = PlayerPrefs.GetInt(PlayerPrefSelectedGameType, (int)GameType.InnerChild);
            savedSelection = Mathf.Clamp(savedSelection, 0, options.Count - 1);
            gameTypeDropdown.value = savedSelection;
            gameTypeDropdown.RefreshShownValue();
            gameTypeDropdown.onValueChanged.AddListener(OnGameTypeChanged);
        }

        // Initialize Sad Child toggle
        //if (showSadChildToggle != null)
        //{
        //    bool showSadChild = PlayerPrefs.GetInt(PlayerPrefShowSadChild, 1) == 1; // Default to showing
        //    showSadChildToggle.isOn = showSadChild;
        //    showSadChildToggle.onValueChanged.AddListener(OnSadChildToggleChanged);

        //    // Initially show/hide toggle based on selected game type
        //    int initialSelection = PlayerPrefs.GetInt(PlayerPrefSelectedGameType, (int)GameType.InnerChild);
        //    GameType initialGameType = (GameType)initialSelection;
        //    bool shouldShowToggle = initialGameType == GameType.WordForest ||
        //                           initialGameType == GameType.Beach ||
        //                           initialGameType == GameType.Room;
        //    showSadChildToggle.gameObject.SetActive(shouldShowToggle);
        //}
        //else
        //{
        //    Debug.LogWarning("LobbyManager: Show Sad Child Toggle is not assigned.");
        //}
    }

    //private void OnSadChildToggleChanged(bool isOn)
    //{
    //    PlayerPrefs.SetInt(PlayerPrefShowSadChild, isOn ? 1 : 0);
    //    PlayerPrefs.Save();
    //    Debug.Log($"LobbyManager: Sad Child visibility set to: {isOn}");
    //}

    private void OnGameTypeChanged(int selectedIndex)
    {
        PlayerPrefs.SetInt(PlayerPrefSelectedGameType, selectedIndex);
        PlayerPrefs.Save();
        
        // Show/hide Sad Child toggle based on selected game type
        // Only show toggle for Forest (WordForest), Beach, and Room
        //if (showSadChildToggle != null)
        //{
        //    GameType selected = (GameType)selectedIndex;
        //    bool shouldShowToggle = selected == GameType.WordForest || 
        //                           selected == GameType.Beach || 
        //                           selected == GameType.Room;
        //    showSadChildToggle.gameObject.SetActive(shouldShowToggle);
        //}
        
        PopulateDrawingList();
    }

    private GameType GetSelectedGameType()
    {
        if (gameTypeDropdown != null)
        {
            return (GameType)Mathf.Clamp(gameTypeDropdown.value, 0, gameTypeDropdown.options.Count - 1);
        }

        int stored = PlayerPrefs.GetInt(PlayerPrefSelectedGameType, (int)GameType.InnerChild);
        return (GameType)Mathf.Clamp(stored, 0, gameTypeDropdown.options.Count - 1);
    }

    // UNUSED
    //private void StartSelectedGame()
    //{
    //    var selected = GetSelectedGameType();
    //    Debug.Log($"LobbyManager: Starting game type: {selected}");

    //    PlayerPrefs.SetString("CurrentUserID", loggedInUserId);
    //    if (selected == GameType.InnerChild)
    //    {
    //        PlayerPrefs.SetString("DrawingToLoad", "");
    //        PlayerPrefs.SetInt("LoadMode", 0);
    //        PlayerPrefs.Save();
    //        SceneManager.LoadScene("DrawingScene");
    //    }
    //    else if (selected == GameType.WordForest)
    //    {
    //        PlayerPrefs.DeleteKey("DrawingToLoad");
    //        PlayerPrefs.DeleteKey("LoadMode");
    //        PlayerPrefs.Save();
    //        SceneManager.LoadScene("WordForestScene");
    //    }
    //    else if (selected == GameType.Beach)
    //    {
    //        PlayerPrefs.SetString("DrawingToLoad", "");
    //        PlayerPrefs.SetInt("LoadMode", 0);
    //        PlayerPrefs.Save();
    //        SceneManager.LoadScene("BeachScene");
    //    }
    //    else if (selected == GameType.Room)
    //    {
    //        PlayerPrefs.SetString("DrawingToLoad", "");
    //        PlayerPrefs.SetInt("LoadMode", 0);
    //        PlayerPrefs.Save();
    //        SceneManager.LoadScene("RoomScene");
    //    }
    //}

    private void SwitchToDrawingsPanel()
    {
        if(drawingsPanel != null) drawingsPanel.SetActive(true);
        PopulateDrawingList();
    }

    private void PopulateDrawingList()
    {
        if (string.IsNullOrEmpty(loggedInUserId)) loggedInUserId = AuthManager.GetCurrentUserID();

        if (drawingListItemPrefab == null)
        {
            Debug.LogError("LobbyManager: Drawing List Item Prefab is not assigned!");
            if (feedbackText != null) feedbackText.text = "Hiba: UI sablon (lista) nincs beállítva!";
            return;
        }
        if (drawingListContent == null)
        {
            Debug.LogError("LobbyManager: Drawing List Content RectTransform is not assigned!");
            if (feedbackText != null) feedbackText.text = "Hiba: Rajzlista panel nincs beállítva!";
            return;
        }
        
        foreach (Transform child in drawingListContent)
        {
            Destroy(child.gameObject);
        }
        
        Debug.Log($"LobbyManager: Fetching drawings for user: {loggedInUserId}");
        List<DatabaseManager.Drawing> allDrawings = databaseManager.GetDrawingsForUser(loggedInUserId);
        
        var selectedGameType = GetSelectedGameType();
        List<DatabaseManager.Drawing> filteredDrawings = new List<DatabaseManager.Drawing>();
        
        if (allDrawings != null)
        {
            foreach (var drawing in allDrawings)
            {
                if (drawing.GameType == (int)selectedGameType)
                {
                    filteredDrawings.Add(drawing);
                }
            }
        }
        
        if (filteredDrawings.Count > 0)
        {
            string gameTypeName = GetGameTypeName((int)selectedGameType);
            if (feedbackText != null) feedbackText.text = $"{filteredDrawings.Count} rajz található ({gameTypeName} térképen).";
            foreach (DatabaseManager.Drawing drawing in filteredDrawings)
            {
                GameObject listItem = Instantiate(drawingListItemPrefab, drawingListContent);
                
                TextMeshProUGUI textComponent = listItem.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    string drawingGameTypeName = GetGameTypeName(drawing.GameType);
                    textComponent.text = $"{drawing.Name} ({drawingGameTypeName})"; 
                }
                else
                {
                    Debug.LogWarning($"LobbyManager: No TextMeshProUGUI found on DrawingListItemPrefab instance for drawing '{drawing.Name}'");
                }

                Button loadButtonComponent = listItem.GetComponent<Button>();

                if (loadButtonComponent != null)
                {
                    string currentDrawingPath = drawing.Path;
                    string currentDrawingName = drawing.Name; 

                    loadButtonComponent.onClick.AddListener(() => {
                        Debug.Log($"LobbyManager: Load button clicked for: {currentDrawingName} ({currentDrawingPath})");
                        LoadSelectedDrawing(currentDrawingPath);
                    });
                }
                else
                {
                    Debug.LogWarning($"LobbyManager: No Load Button found on DrawingListItemPrefab instance for drawing '{drawing.Name}'");
                }

                Transform deleteButtonTransform = listItem.transform.Find("DeleteButton"); // Keresd meg a "DeleteButton" nevű gyereket
                if (deleteButtonTransform != null)
                {
                    Button deleteButtonComponent = deleteButtonTransform.GetComponent<Button>();
                    if (deleteButtonComponent != null)
                    {
                        string drawingIdToDelete = drawing.Id;
                        string drawingPathToDelete = drawing.Path;
                        string drawingNameToDelete = drawing.Name;

                        deleteButtonComponent.onClick.AddListener(() => {
                            Debug.Log($"LobbyManager: Delete button clicked for: {drawingNameToDelete} (ID: {drawingIdToDelete})");
                            AttemptDeleteDrawing(drawingIdToDelete, drawingPathToDelete, drawingNameToDelete);
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"LobbyManager: No Button component found on 'DeleteButton' child for drawing '{drawing.Name}'");
                    }
                }
                else
                {
                    Debug.LogWarning($"LobbyManager: 'DeleteButton' child not found on list item for drawing '{drawing.Name}'. Make sure your prefab has a child GameObject named 'DeleteButton' with a Button component.");
                }

            }
        }
        else
        {
            string gameTypeName = GetGameTypeName((int)selectedGameType);
            Debug.Log($"LobbyManager: No drawings found for this user on {gameTypeName} map.");
            if (feedbackText != null) feedbackText.text = $"Nincsenek mentett rajzaid a {gameTypeName} térképen. Hozz létre egy újat!";
        }
    }

    private void EnablePlayerInfoPanel(bool enable)
    {
        if (playerInfoPanel != null)
        {
            playerInfoPanel.SetActive(enable);

            if (enable)
            {
                playerInfoSaveButton.gameObject.SetActive(false);

                if (string.IsNullOrEmpty(loggedInUserId) && AuthManager.IsLoggedIn()) loggedInUserId = AuthManager.GetCurrentUsername();

                if (ageInput != null)
                {
                    int savedAge = databaseManager.GetPlayerAge(loggedInUserId);
                    ageInput.text = savedAge > 0 ? savedAge.ToString() : "";
                    ageInput.onValueChanged.RemoveAllListeners();
                    ageInput.onValueChanged.AddListener((string newValue) =>
                    {
                        playerInfoSaveButton.gameObject.SetActive(true);
                    });
                }

                if (genderDropdown != null)
                {
                    genderDropdown.ClearOptions();
                    List<string> genders = new() { "Male", "Female", "Other" };
                    genderDropdown.AddOptions(genders);

                    string savedGender = databaseManager.GetPlayerGender(loggedInUserId);
                    int genderIndex = genders.IndexOf(savedGender);
                    if (genderIndex < 0) genderIndex = 0;
                    genderDropdown.value = genderIndex;
                    genderDropdown.RefreshShownValue();
                    genderDropdown.onValueChanged.RemoveAllListeners();
                    genderDropdown.onValueChanged.AddListener((int newIndex) =>
                    {
                        playerInfoSaveButton.gameObject.SetActive(true);
                    });
                }

                if (DominantHandDropdown != null)
                {
                    DominantHandDropdown.ClearOptions();
                    List<string> hands = new() { "Left", "Right", "Both" };
                    DominantHandDropdown.AddOptions(hands);

                    string savedHand = databaseManager.GetPlayerDominantHand(loggedInUserId);
                    int handIndex = hands.IndexOf(savedHand);
                    if (handIndex < 0) handIndex = 1;
                    DominantHandDropdown.value = handIndex;
                    DominantHandDropdown.RefreshShownValue();
                    DominantHandDropdown.onValueChanged.RemoveAllListeners();
                    DominantHandDropdown.onValueChanged.AddListener((int newIndex) =>
                    {
                        playerInfoSaveButton.gameObject.SetActive(true);
                    });
                }

                if (playerInfoSaveButton != null)
                {
                    playerInfoSaveButton.onClick.RemoveAllListeners();
                    playerInfoSaveButton.onClick.AddListener(() =>
                    {
                        int age = 0;
                        if (ageInput != null && int.TryParse(ageInput.text, out int parsedAge)) age = parsedAge;

                        string gender = genderDropdown != null ? genderDropdown.options[genderDropdown.value].text : "Male";
                        string hand = DominantHandDropdown != null ? DominantHandDropdown.options[DominantHandDropdown.value].text : "Right";

                        if (databaseManager.UpdatePlayerInfo(loggedInUserId, null, gender, age, hand))
                        {
                            Debug.Log("Player info updated successfully!");
                            playerInfoSaveButton.gameObject.SetActive(false);
                        }
                        else Debug.LogError("Failed to update player info.");
                    });
                }
            }
        }
    }


    private void AttemptDeleteDrawing(string drawingId, string filePath, string drawingName)
    {
        if (feedbackText != null) feedbackText.text = $"'{drawingName}' törlése...";
        Debug.Log($"LobbyManager: Attempting to delete drawing: {drawingName}, ID: {drawingId}, Path: {filePath}");

        if (DatabaseManager.Instance != null)
        {
            bool deleteSuccess = DatabaseManager.Instance.DeleteDrawing(drawingId, filePath);
            if (deleteSuccess)
            {
                Debug.Log($"LobbyManager: Drawing '{drawingName}' deleted successfully.");
                if (feedbackText != null) feedbackText.text = $"'{drawingName}' sikeresen törölve.";
                
                PopulateDrawingList();
            }
            else
            {
                Debug.LogError($"LobbyManager: Failed to delete drawing '{drawingName}'.");
                if (feedbackText != null) feedbackText.text = $"'{drawingName}' törlése sikertelen.";
            }
        }
        else
        {
            Debug.LogError("LobbyManager: DatabaseManager instance not found. Cannot delete drawing.");
            if (feedbackText != null) feedbackText.text = "Hiba: Adatbázis nem elérhető.";
        }
    }


    private void LoadSelectedDrawing(string drawingPath)
    {
        Debug.Log($"LobbyManager: Preparing to load drawing from path: {drawingPath}");
        
        DatabaseManager.Drawing targetDrawing = null;
        List<DatabaseManager.Drawing> allDrawings = databaseManager.GetDrawingsForUser(loggedInUserId);
        
        if (allDrawings != null)
        {
            foreach (var drawing in allDrawings)
            {
                if (drawing.Path == drawingPath)
                {
                    targetDrawing = drawing;
                    break;
                }
            }
        }
        
        if (targetDrawing == null)
        {
            Debug.LogError($"LobbyManager: Could not find drawing with path: {drawingPath}");
            if (feedbackText != null) feedbackText.text = "Hiba: Rajz nem található az adatbázisban!";
            return;
        }
        
        var drawingGameType = (GameType)targetDrawing.GameType;
        Debug.Log($"LobbyManager: Loading drawing '{targetDrawing.Name}' from {GetGameTypeName(targetDrawing.GameType)} map");
        
        PlayerPrefs.SetString("CurrentUserID", loggedInUserId);
        string sceneToLoad = GetSceneNameForGameType(drawingGameType);

        if (sceneToLoad != null)
        {
            PlayerPrefs.SetString("DrawingToLoad", drawingPath);
            PlayerPrefs.SetInt("LoadMode", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"LobbyManager: Unknown GameType {drawingGameType} for drawing: {targetDrawing.Name}");
            if (feedbackText != null) feedbackText.text = "Hiba: Ismeretlen térkép típus!";
        }
    }
    
    public void OnNewDrawingButtonClicked()
    {
        var selected = GetSelectedGameType();
        Debug.Log($"LobbyManager: Starting new session for game type: {selected}");

        string sceneToLoad = GetSceneNameForGameType(selected);

        if (sceneToLoad != null)
        {
            PlayerPrefs.SetString("DrawingToLoad", "");
            PlayerPrefs.SetInt("LoadMode", 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private string GetGameTypeName(int gameType)
    {
        switch (gameType)
        {
            case (int)DrawingData.GameType.InnerChild:
                return "Drawing";
            case (int)DrawingData.GameType.BilateralDrawing:
                return "Bilateral Drawing";
            case (int)DrawingData.GameType.SafePlace:
                return "Safe Place";
            case (int)DrawingData.GameType.WordForest:
                return "Word Forest";
            case (int)DrawingData.GameType.Beach:
                return "Beach";
            case (int)DrawingData.GameType.Room:
                return "Room";
            default:
                return "Unknown";
        }
    }

    public static string GetSceneNameForGameType(GameType gameType)
    {
        return gameType switch
        {
            GameType.InnerChild => "DrawingScene",
            GameType.WordForest => "WordForestScene",
            GameType.Beach => "BeachScene",
            GameType.Room => "RoomScene",
            _ => null,
        };
    }

    //private void SetupVRKeyboardForInputFields()
    //{
    //    // Add VRKeyboardInputField to username input
    //    if (usernameInput != null && usernameInput.GetComponent<VRKeyboardInputField>() == null)
    //    {
    //        usernameInput.gameObject.AddComponent<VRKeyboardInputField>();
    //        Debug.Log("LobbyManager: Added VRKeyboardInputField to usernameInput");
    //    }

    //    // Add VRKeyboardInputField to password input
    //    if (passwordInput != null && passwordInput.GetComponent<VRKeyboardInputField>() == null)
    //    {
    //        passwordInput.gameObject.AddComponent<VRKeyboardInputField>();
    //        Debug.Log("LobbyManager: Added VRKeyboardInputField to passwordInput");
    //    }

    //    // Add VRKeyboardInputField to age input
    //    if (ageInput != null && ageInput.GetComponent<VRKeyboardInputField>() == null)
    //    {
    //        ageInput.gameObject.AddComponent<VRKeyboardInputField>();
    //        Debug.Log("LobbyManager: Added VRKeyboardInputField to ageInput");
    //    }
    //}

}
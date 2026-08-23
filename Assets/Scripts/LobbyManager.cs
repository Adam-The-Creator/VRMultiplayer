using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;
using DrawingMeta = Assets.Database.DatabaseManagement.SQLiteDB.Drawing; // Alias to prevent conflict with MongoDB.Drawing

public class LobbyManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject drawingsPanel;
    [SerializeField] private GameObject playerInfoPanel;
    [SerializeField] private GameObject gamePanel;

    [Header("Game Type Selection")]
    [SerializeField] private TMP_Dropdown gameTypeDropdown;
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

    private string loggedInUserId = null;
    private const string PlayerPrefSelectedGameType = "SelectedGameType";

    void Start()
    {
        AuthManager authManager = GameObject.FindGameObjectWithTag("AuthManager").GetComponent<AuthManager>();
        DataAnalysisManager analysisManager = GameObject.FindGameObjectWithTag("DataAnalysisManager").GetComponent<DataAnalysisManager>();

        authManager.InitializeEvents();

        // Convert lambdas to async to handle Database calls
        authManager.OnLogIn += async () =>
        {
            loggedInUserId = AuthManager.GetCurrentUserID();
            if (usernameText != null) usernameText.text = AuthManager.GetCurrentUsername();
            if (gameTypeDropdown != null) gameTypeDropdown.interactable = true;
            if (newDrawingButton != null) newDrawingButton.interactable = true;

            var userRole = await AuthManager.GetCurrentUserRoleAsync();
            analysisManager.SetPanelsActive(userRole == Assets.Database.DatabaseManagement.SQLiteDB.Role.Psychologist);

            // Wait for drawings to populate
            await SwitchToDrawingsPanelAsync();
            await EnablePlayerInfoPanelAsync(true);
        };

        authManager.OnLogout += () =>
        {
            loggedInUserId = null;
            if (drawingsPanel != null) drawingsPanel.SetActive(false);
            if (playerInfoPanel != null) playerInfoPanel.SetActive(false);
            if (usernameText != null) usernameText.text = "";
            if (gameTypeDropdown != null) gameTypeDropdown.interactable = false;
            if (newDrawingButton != null) newDrawingButton.interactable = false;
            if (drawingListContent != null) foreach (Transform child in drawingListContent) Destroy(child.gameObject);
            analysisManager.SetPanelsActive(false);
        };


        if (newDrawingButton != null) newDrawingButton.onClick.AddListener(OnNewDrawingButtonClicked);
        else Debug.LogWarning("LobbyManager: New Drawing Button not assigned.");

        if (logoutButton != null) logoutButton.onClick.AddListener(() => authManager.Logout());
        else Debug.LogWarning("LobbyManager: Logout Button not assigned.");

        if (exitButton != null) exitButton.onClick.AddListener(() => Application.Quit());
        else Debug.LogWarning("LobbyManager: Exit Button not assigned.");


        InitializeLobbyStateAsync(analysisManager);

        gamePanel.SetActive(true);

        if (feedbackText != null) feedbackText.text = "";

        if (gameTypeDropdown != null)
        {
            gameTypeDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>
            {
                new("Drawing"),
                new("Word Forest"),
                new("Beach"),
                new("Room")
            };
            gameTypeDropdown.AddOptions(options);

            int savedSelection = PlayerPrefs.GetInt(PlayerPrefSelectedGameType, (int)GameType.INNER_CHILD);
            savedSelection = Mathf.Clamp(savedSelection, 0, options.Count - 1);
            gameTypeDropdown.value = savedSelection;
            gameTypeDropdown.RefreshShownValue();

            // When game type changes, we must repopulate the list asynchronously
            gameTypeDropdown.onValueChanged.AddListener(async (int index) =>
            {
                OnGameTypeChanged(index);
                await PopulateDrawingListAsync();
            });
        }
    }

    private async void InitializeLobbyStateAsync(DataAnalysisManager analysisManager)
    {
        if (!AuthManager.IsLoggedIn())
        {
            drawingsPanel.SetActive(false);
            if (playerInfoPanel != null) playerInfoPanel.SetActive(false);
            if (gameTypeDropdown != null) gameTypeDropdown.interactable = false;
            if (newDrawingButton != null) newDrawingButton.interactable = false;
            analysisManager.SetPanelsActive(false);
        }
        else
        {
            var userRole = await AuthManager.GetCurrentUserRoleAsync();
            await SwitchToDrawingsPanelAsync();
            await EnablePlayerInfoPanelAsync(true);

            if (gameTypeDropdown != null) gameTypeDropdown.interactable = true;
            if (newDrawingButton != null) newDrawingButton.interactable = true;
            analysisManager.SetPanelsActive(userRole == Assets.Database.DatabaseManagement.SQLiteDB.Role.Psychologist);
        }
    }

    private void OnGameTypeChanged(int selectedIndex)
    {
        PlayerPrefs.SetInt(PlayerPrefSelectedGameType, selectedIndex);
        PlayerPrefs.Save();
    }

    private GameType GetSelectedGameType()
    {
        if (gameTypeDropdown != null)
        {
            return (GameType)Mathf.Clamp(gameTypeDropdown.value, 0, gameTypeDropdown.options.Count - 1);
        }

        int stored = PlayerPrefs.GetInt(PlayerPrefSelectedGameType, (int)GameType.INNER_CHILD);
        return (GameType)Mathf.Clamp(stored, 0, gameTypeDropdown.options.Count - 1);
    }

    private async System.Threading.Tasks.Task SwitchToDrawingsPanelAsync()
    {
        if (drawingsPanel != null) drawingsPanel.SetActive(true);
        await PopulateDrawingListAsync();
    }

    private async System.Threading.Tasks.Task PopulateDrawingListAsync()
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

        // Asynchronous database call
        List<DrawingMeta> allDrawings = await DatabaseManager.Instance.GetDrawingsForUser(loggedInUserId);

        var selectedGameType = GetSelectedGameType();
        List<DrawingMeta> filteredDrawings = new List<DrawingMeta>();

        if (allDrawings != null)
        {
            foreach (var drawing in allDrawings)
            {
                if (drawing.gameType == selectedGameType)
                {
                    filteredDrawings.Add(drawing);
                }
            }
        }

        if (filteredDrawings.Count > 0)
        {
            string gameTypeName = GetGameTypeName((int)selectedGameType);
            if (feedbackText != null) feedbackText.text = $"{filteredDrawings.Count} rajz található ({gameTypeName} térképen).";

            foreach (DrawingMeta drawing in filteredDrawings)
            {
                GameObject listItem = Instantiate(drawingListItemPrefab, drawingListContent);

                TextMeshProUGUI textComponent = listItem.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    string drawingGameTypeName = GetGameTypeName((int)drawing.gameType);
                    textComponent.text = $"{drawing.name} ({drawingGameTypeName})";
                }
                else
                {
                    Debug.LogWarning($"LobbyManager: No TextMeshProUGUI found on DrawingListItemPrefab instance for drawing '{drawing.name}'");
                }

                Button loadButtonComponent = listItem.GetComponent<Button>();

                if (loadButtonComponent != null)
                {
                    string currentDrawingPath = drawing.path;
                    string currentDrawingName = drawing.name;
                    GameType drawingGameTypeEnum = drawing.gameType;

                    loadButtonComponent.onClick.AddListener(() => {
                        Debug.Log($"LobbyManager: Load button clicked for: {currentDrawingName} ({currentDrawingPath})");
                        LoadSelectedDrawing(currentDrawingPath, drawingGameTypeEnum);
                    });
                }
                else
                {
                    Debug.LogWarning($"LobbyManager: No Load Button found on DrawingListItemPrefab instance for drawing '{drawing.name}'");
                }

                Transform deleteButtonTransform = listItem.transform.Find("DeleteButton");
                if (deleteButtonTransform != null)
                {
                    Button deleteButtonComponent = deleteButtonTransform.GetComponent<Button>();
                    if (deleteButtonComponent != null)
                    {
                        string drawingIdToDelete = drawing.id;
                        string drawingPathToDelete = drawing.path;
                        string drawingNameToDelete = drawing.name;

                        // Call async delete method
                        deleteButtonComponent.onClick.AddListener(async () => {
                            Debug.Log($"LobbyManager: Delete button clicked for: {drawingNameToDelete} (ID: {drawingIdToDelete})");
                            await AttemptDeleteDrawingAsync(drawingIdToDelete, drawingPathToDelete, drawingNameToDelete);
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"LobbyManager: No Button component found on 'DeleteButton' child for drawing '{drawing.name}'");
                    }
                }
                else
                {
                    Debug.LogWarning($"LobbyManager: 'DeleteButton' child not found on list item for drawing '{drawing.name}'.");
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

    private async System.Threading.Tasks.Task EnablePlayerInfoPanelAsync(bool enable)
    {
        if (playerInfoPanel != null)
        {
            playerInfoPanel.SetActive(enable);

            if (enable)
            {
                playerInfoSaveButton.gameObject.SetActive(false);

                if (string.IsNullOrEmpty(loggedInUserId) && AuthManager.IsLoggedIn()) loggedInUserId = AuthManager.GetCurrentUsername();

                int savedAge = await DatabaseManager.Instance.GetPlayerAge(loggedInUserId);
                string savedGender = await DatabaseManager.Instance.GetPlayerGender(loggedInUserId);
                string savedHand = await DatabaseManager.Instance.GetPlayerDominantHand(loggedInUserId);

                if (ageInput != null)
                {
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
                    playerInfoSaveButton.onClick.AddListener(async () =>
                    {
                        int age = 0;
                        if (ageInput != null && int.TryParse(ageInput.text, out int parsedAge)) age = parsedAge;

                        string gender = genderDropdown != null ? genderDropdown.options[genderDropdown.value].text : "Male";
                        string hand = DominantHandDropdown != null ? DominantHandDropdown.options[DominantHandDropdown.value].text : "Right";

                        bool success = await DatabaseManager.Instance.UpdatePlayerInfo(loggedInUserId, null, gender, age, hand);
                        if (success)
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

    private async System.Threading.Tasks.Task AttemptDeleteDrawingAsync(string drawingId, string filePath, string drawingName)
    {
        if (feedbackText != null) feedbackText.text = $"'{drawingName}' törlése...";
        Debug.Log($"LobbyManager: Attempting to delete drawing: {drawingName}, ID: {drawingId}");

        if (DatabaseManager.Instance != null)
        {
            bool deleteSuccess = await DatabaseManager.Instance.DeleteDrawing(drawingId, filePath);
            if (deleteSuccess)
            {
                Debug.Log($"LobbyManager: Drawing '{drawingName}' deleted successfully.");
                if (feedbackText != null) feedbackText.text = $"'{drawingName}' sikeresen törölve.";

                await PopulateDrawingListAsync();
            }
            else
            {
                Debug.LogError($"LobbyManager: Failed to delete drawing '{drawingName}'.");
                if (feedbackText != null) feedbackText.text = $"'{drawingName}' törlése sikertelen.";
            }
        }
    }


    private void LoadSelectedDrawing(string drawingPath, GameType drawingGameType)
    {
        Debug.Log($"LobbyManager: Loading drawing from {GetGameTypeName((int)drawingGameType)} map");

        PlayerPrefs.SetString("CurrentUserID", loggedInUserId);
        string sceneToLoad = GetSceneNameForGameType(drawingGameType);

        if (sceneToLoad != null)
        {
            PlayerPrefs.SetString("DrawingToLoad", drawingPath); // In the new system, we might want to pass the drawing ID instead of the JSON path
            PlayerPrefs.SetInt("LoadMode", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"LobbyManager: Unknown GameType {drawingGameType}");
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
        return gameType switch
        {
            (int)GameType.INNER_CHILD => "Drawing",
            (int)GameType.BILATERAL_DRAWING => "Bilateral Drawing",
            (int)GameType.SAFE_PLACE => "Safe Place",
            (int)GameType.WORD_FOREST => "Word Forest",
            (int)GameType.BEACH => "Beach",
            (int)GameType.ROOM => "Room",
            _ => "Unknown",
        };
    }

    public static string GetSceneNameForGameType(GameType gameType)
    {
        return gameType switch
        {
            GameType.INNER_CHILD => "DrawingScene",
            GameType.WORD_FOREST => "WordForestScene",
            GameType.BEACH => "BeachScene",
            GameType.ROOM => "RoomScene",
            _ => null,
        };
    }
}
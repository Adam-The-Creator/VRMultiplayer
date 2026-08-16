//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using FishNet;
//using FishNet.Managing.Scened;
//using FishNet.Transporting;

//public class MenuPanelManager : MonoBehaviour
//{
//    /*
//        TODO: Handle the visibilitty and the transition between the different panels (main menu, auth, player info) 
//              based on the player's actions and game state. For example, show the player info panel, the main Menu panel if the player 
//              is already authenticated and hide the authentication panel. Authentication is mandatory to show the other panels.
//              Handle the scene transition to the Lobby scene when the player clicks on the play button, 
//              and pass the information about the game mode (single-player or multiplayer) to the Lobby scene if necessary.
//              Reuse methods from the LobbyManager (The role of the LobbyManager shall be decreased).
//              The Network Manager object comes from SetupScene as DontDestroyOnLoad.

//        ROLE: Menus and inputs for Authentication, Settings, Networking and Entering the next Scene after starting the Game.
//              In this Scene the Network Manager object will be enabled and initialized.
     
//     */


//    [Header("UI panels")]
//    [Tooltip("Game object of the Menu Panel")] public GameObject mainMenuPanel;
//    [Tooltip("Game object of the Auth Panel")] public GameObject authPanel;
//    [Tooltip("Game object of the Player Info Panel")] public GameObject playerInfoPanel;

//    [Header("Menu Panel")]
//    [Tooltip("Button to start the game")] public Button playButton;
//    [Tooltip("Button to game settings")] public Button settingsButton;
//    [Tooltip("Button to start the game in multiplayer mode")] public Button multiplayerButton;

//    [Header("Player Info Panel")]
//    [SerializeField] private TMP_Text usernameText;
//    [SerializeField] private TMP_InputField ageInput;
//    [SerializeField] private TMP_Dropdown genderDropdown;
//    [SerializeField] private TMP_Dropdown DominantHandDropdown;
//    [SerializeField] private Button playerInfoSaveButton;
//    [SerializeField] private Button logoutButton;

//    [Header("Multiplayer Settings")]
//    [Tooltip("Input field for the IP address to connect to (if Client)")]
//    public TMP_InputField ipInputField;


//    private readonly string lobbyScene = "LobbyScene";
//    private AuthManager authManager;



//    void Play(bool multiplayer = false)
//    {
//        /*
//            TODO: Implement the logic to start the game, either in single-player or multiplayer mode.
//                  Load the Lobby scene that handles each mode accordingly.
//                  Single-player mode: Start the server (localhost) and client on the same instance, and load the Lobby scene.
//                  Multiplayer mode: Start the client and connect to the server using the provided IP address, and load the Lobby scene.
//                  If no IP address / server is available, then start the game as HOST.
//         */

//        if (!multiplayer)
//        {
//            Debug.Log("[MenuPanelManager] Starting as HOST (Single-player mode)...");

//            InstanceFinder.ServerManager.OnServerConnectionState += OnServerStarted;

//            InstanceFinder.ServerManager.StartConnection();
//            InstanceFinder.ClientManager.StartConnection();
//        }
//        else
//        {
//            Debug.Log("[MenuPanelManager] Starting as CLIENT (Multiplayer mode)...");

//            string ipAddress = "localhost";
//            if (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text))
//            {
//                ipAddress = ipInputField.text;
//            }

//            InstanceFinder.ClientManager.StartConnection(ipAddress);
//        }
//    }

//    void Settings()
//    {
//        /*
//            TODO: Implement the logic to open the settings menu, allowing the player to adjust game settings such as audio, graphics,
//                  network for multiplayer mode and may controls. [LOW_PRIO]
//         */
//    }

//    private void Start()
//    {
//        if (InstanceFinder.NetworkManager == null)
//        {
//            var networkManager = FindObjectOfType<FishNet.Managing.NetworkManager>(true);
//            if (networkManager != null)
//            {
//                networkManager.gameObject.SetActive(true);
//                Debug.Log("[MenuPanelManager] NetworkManager activated.");
//            }
//            else Debug.LogError("[MenuPanelManager] FATAL ERROR: NetworkManager not found in the scene!");
//        }

//        authManager = authPanel.GetComponent<AuthManager>();
//        if (authManager != null)
//        {
//            authManager.InitializeEvents();

//            authManager.OnLogIn += UpdatePanels;
//            authManager.OnLogout += UpdatePanels;
//            logoutButton.onClick.AddListener(() => authManager.Logout());
//        }

//        playButton.onClick.AddListener(() => Play());
//        multiplayerButton.onClick.AddListener(() => Play(multiplayer: true));
//        settingsButton.onClick.AddListener(() => Settings());

//        UpdatePanels();
//    }

//    private void OnDestroy()
//    {
//        if (authManager != null)
//        {
//            authManager.OnLogIn -= UpdatePanels;
//            authManager.OnLogout -= UpdatePanels;
//        }
//    }

//    private void UpdatePanels()
//    {
//        bool isLoggedIn = AuthManager.IsLoggedIn();

//        if (authPanel != null) authPanel.SetActive(!isLoggedIn);
//        if (mainMenuPanel != null) mainMenuPanel.SetActive(isLoggedIn);
//        EnablePlayerInfoPanel(isLoggedIn);

//        usernameText.text = isLoggedIn ? AuthManager.GetCurrentUsername() : "";
//    }

//    private void EnablePlayerInfoPanel(bool enable)
//    {
//        if (playerInfoPanel != null)
//        {
//            playerInfoPanel.SetActive(enable);

//            if (enable)
//            {
//                playerInfoSaveButton.gameObject.SetActive(false);

//                string loggedInUserId = AuthManager.GetCurrentUserID();

//                if (ageInput != null)
//                {
//                    int savedAge = DatabaseManager.Instance.GetPlayerAge(loggedInUserId);
//                    ageInput.text = savedAge > 0 ? savedAge.ToString() : "";
//                    ageInput.onValueChanged.RemoveAllListeners();
//                    ageInput.onValueChanged.AddListener((string newValue) =>
//                    {
//                        playerInfoSaveButton.gameObject.SetActive(true);
//                    });
//                }

//                if (genderDropdown != null)
//                {
//                    genderDropdown.ClearOptions();
//                    List<string> genders = new() { "Male", "Female", "Other" };
//                    genderDropdown.AddOptions(genders);

//                    string savedGender = DatabaseManager.Instance.GetPlayerGender(loggedInUserId);
//                    int genderIndex = genders.IndexOf(savedGender);
//                    if (genderIndex < 0) genderIndex = 0;
//                    genderDropdown.value = genderIndex;
//                    genderDropdown.RefreshShownValue();
//                    genderDropdown.onValueChanged.RemoveAllListeners();
//                    genderDropdown.onValueChanged.AddListener((int newIndex) =>
//                    {
//                        playerInfoSaveButton.gameObject.SetActive(true);
//                    });
//                }

//                if (DominantHandDropdown != null)
//                {
//                    DominantHandDropdown.ClearOptions();
//                    List<string> hands = new() { "Left", "Right", "Both" };
//                    DominantHandDropdown.AddOptions(hands);

//                    string savedHand = DatabaseManager.Instance.GetPlayerDominantHand(loggedInUserId);
//                    int handIndex = hands.IndexOf(savedHand);
//                    if (handIndex < 0) handIndex = 1;
//                    DominantHandDropdown.value = handIndex;
//                    DominantHandDropdown.RefreshShownValue();
//                    DominantHandDropdown.onValueChanged.RemoveAllListeners();
//                    DominantHandDropdown.onValueChanged.AddListener((int newIndex) =>
//                    {
//                        playerInfoSaveButton.gameObject.SetActive(true);
//                    });
//                }

//                if (playerInfoSaveButton != null)
//                {
//                    playerInfoSaveButton.onClick.RemoveAllListeners();
//                    playerInfoSaveButton.onClick.AddListener(() =>
//                    {
//                        int age = 0;
//                        if (ageInput != null && int.TryParse(ageInput.text, out int parsedAge)) age = parsedAge;

//                        string gender = genderDropdown != null ? genderDropdown.options[genderDropdown.value].text : "Male";
//                        string hand = DominantHandDropdown != null ? DominantHandDropdown.options[DominantHandDropdown.value].text : "Right";

//                        if (DatabaseManager.Instance.UpdatePlayerInfo(loggedInUserId, null, gender, age, hand))
//                        {
//                            Debug.Log("Player info updated successfully!");
//                            playerInfoSaveButton.gameObject.SetActive(false);
//                        }
//                        else Debug.LogError("Failed to update player info.");
//                    });
//                }
//            }
//        }
//    }

//    private void OnServerStarted(ServerConnectionStateArgs args)
//    {
//        if (args.ConnectionState == LocalConnectionState.Started)
//        {
//            InstanceFinder.ServerManager.OnServerConnectionState -= OnServerStarted;

//            Debug.Log($"[MenuPanelManager] Server started. Loading global scene: {lobbyScene}");

//            SceneLoadData sceneLoadData = new SceneLoadData(lobbyScene);
//            sceneLoadData.ReplaceScenes = ReplaceOption.All;

//            InstanceFinder.SceneManager.LoadGlobalScenes(sceneLoadData);
//        }
//    }
//}

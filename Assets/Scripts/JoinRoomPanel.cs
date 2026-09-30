using System;
using System.Threading.Tasks;
using Assets.Database.DatabaseManagement;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JoinRoomPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Tooltip("After connecting, how long to wait for the host's scene. The host may still be in the lobby, " +
             "so this is deliberately long.")]
    [SerializeField] private float sceneWaitSeconds = 90f;

    private bool _busy;

    private void Start()
    {
        if (feedbackText != null) feedbackText.text = "";
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    private async void OnJoinClicked()
    {
        try { await JoinAsync(); }
        catch (Exception e)
        {
            Debug.LogException(e);
            if (this != null) Fail("Join failed unexpectedly. See the log.");
        }
    }

    private async Task JoinAsync()
    {
        if (_busy) return;

        string code = roomCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code)) return;

        _busy = true;
        joinButton.interactable = false;
        Scene loginScene = gameObject.scene; // the lobby we unload once the game scene is up

        SetFeedback("Locating room...");
        var roomData = await DatabaseManager.Instance.JoinRoom(code);
        if (this == null) return; // lobby was unloaded while we waited

        if (roomData == null)
        {
            Debug.LogError("JoinRoomPanel: Room not found or network error.");
            Fail("Invalid or expired Room Code.");
            return;
        }

        string sceneToLoad = LobbyManager.GetSceneNameForGameType(roomData.gameType);
        if (sceneToLoad == null)
        {
            Fail("This room uses a game type that is not supported.");
            return;
        }

        // Inject the joined room's context into PlayerPrefs (restored if the join fails).
        string prevSession = PlayerPrefs.GetString("CurrentSessionID", "");
        string prevDrawing = PlayerPrefs.GetString("DrawingToLoad", "");
        int prevMode = PlayerPrefs.GetInt("LoadMode", 0);

        string drawingId = roomData.drawingID ?? "";
        PlayerPrefs.SetString("CurrentSessionID", roomData.sessionID ?? "");
        PlayerPrefs.SetString("DrawingToLoad", drawingId);
        PlayerPrefs.SetInt("LoadMode", string.IsNullOrEmpty(drawingId) ? 0 : 1);
        PlayerPrefs.Save();

        void RestorePrefs()
        {
            PlayerPrefs.SetString("CurrentSessionID", prevSession);
            PlayerPrefs.SetString("DrawingToLoad", prevDrawing);
            PlayerPrefs.SetInt("LoadMode", prevMode);
            PlayerPrefs.Save();
        }

        // 1. Connect to the host and WAIT for the result (this can take a few seconds over Steam).
        SetFeedback("Connecting to host...");
        bool connected = await VRNetworkManager.Instance.JoinRemoteSessionAsync(roomData.roomAddress);
        if (this == null) return;

        if (!connected)
        {
            RestorePrefs();
            Fail(VRNetworkManager.Instance.LastJoinError ?? "Could not connect to the host.");
            return;
        }

        // 2. The host loads its scene as a FishNet global scene; FishNet then makes us load it too.
        //    The host may not have started the session yet, so wait generously.
        SetFeedback("Connected. Waiting for the host's session...");
        float start = Time.realtimeSinceStartup;
        while (!SceneManager.GetSceneByName(sceneToLoad).isLoaded)
        {
            if (this == null) return;

            if (Time.realtimeSinceStartup - start > sceneWaitSeconds)
            {
                Debug.LogError($"JoinRoomPanel: '{sceneToLoad}' was not loaded by the host within {sceneWaitSeconds}s. " +
                               "Leaving the remote session.");
                await VRNetworkManager.Instance.LeaveRemoteSessionAsync();
                RestorePrefs();
                if (this != null) Fail("The host has not started the session yet. Try again later.");
                return;
            }
            await Task.Yield();
        }

        // 3. Make the networked scene active and get rid of the lobby.
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
        if (loginScene.isLoaded) SceneManager.UnloadSceneAsync(loginScene);

        // 4. If the host closes the scene or the connection drops, send this player back to the lobby.
        NetworkSceneFlow.WatchRemoteSession(sceneToLoad);
    }

    private void Fail(string message)
    {
        SetFeedback(message);
        _busy = false;
        if (joinButton != null) joinButton.interactable = true;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
    }
}
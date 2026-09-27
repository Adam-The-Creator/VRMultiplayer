using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Database.DatabaseManagement;
using Steamworks;

public class CreateRoomPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button createRoomButton;

    private string _activeRoomCode = null;

    private void Start()
    {
        if (feedbackText != null) feedbackText.text = "";

        createRoomButton.onClick.AddListener(async () =>
        {
            createRoomButton.interactable = false;
            feedbackText.text = "Generating code...";

            string roomName = string.IsNullOrWhiteSpace(roomNameInput.text) ? "My Room" : roomNameInput.text;
            string sessionId = PlayerPrefs.GetString("CurrentSessionID", null);
            string drawingId = PlayerPrefs.GetString("DrawingToLoad", null);
            string hostId = AuthManager.GetCurrentUserID();

            if (string.IsNullOrEmpty(drawingId))
            {
                feedbackText.text = "Please save your drawing first!";
                createRoomButton.interactable = true;
                return;
            }

            // Fallback to localhost, but grab the Steam ID if Steam is running
            string hostSteamAddress = "localhost";
            if (SteamManager.Initialized)
            {
                hostSteamAddress = SteamUser.GetSteamID().ToString();
            }

            string code = await DatabaseManager.Instance.CreateRoom(roomName, hostSteamAddress, sessionId, drawingId, hostId);

            if (!string.IsNullOrEmpty(code))
            {
                _activeRoomCode = code; // Store the code so we can delete the room later
                roomCodeText.text = code;
                feedbackText.text = "Room is live and ready to share!";

                //VRNetworkManager.Instance.StartHostSession();
            }
            else
            {
                feedbackText.text = "Failed to create room.";
                createRoomButton.interactable = true;
            }
        });
    }

    // Automatically clean up the room from the database if the host leaves this view
    private async void OnDestroy()
    {
        if (!string.IsNullOrEmpty(_activeRoomCode) && DatabaseManager.Instance != null)
        {
            await DatabaseManager.Instance.DeleteRoom(_activeRoomCode);
            _activeRoomCode = null;
        }
    }

    // Backup cleanup in case the host quits the game entirely while the room is open
    private async void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(_activeRoomCode) && DatabaseManager.Instance != null)
        {
            await DatabaseManager.Instance.DeleteRoom(_activeRoomCode);
            _activeRoomCode = null;
        }
    }
}
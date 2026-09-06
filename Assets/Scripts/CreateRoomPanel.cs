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

    private void Start()
    {
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
                roomCodeText.text = code;
                feedbackText.text = "Room is live and ready to share!";

                VRNetworkManager.Instance.StartHostSession();
            }
            else
            {
                feedbackText.text = "Failed to create room.";
                createRoomButton.interactable = true;
            }
        });
    }
}
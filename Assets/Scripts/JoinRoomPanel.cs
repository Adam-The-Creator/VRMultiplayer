using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Assets.Database.DatabaseManagement;
using System.Threading.Tasks;

public class JoinRoomPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private void Start()
    {
        if (feedbackText != null) feedbackText.text = "";

        joinButton.onClick.AddListener(async () =>
        {
            string code = roomCodeInput.text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code)) return;

            joinButton.interactable = false;
            feedbackText.text = "Locating room...";

            var roomData = await DatabaseManager.Instance.JoinRoom(code);

            if (roomData != null)
            {
                feedbackText.text = "Connecting to Host...";

                // Inject the joined room's context into PlayerPrefs
                PlayerPrefs.SetString("CurrentSessionID", roomData.sessionID);
                PlayerPrefs.SetString("DrawingToLoad", roomData.drawingID);
                PlayerPrefs.SetInt("LoadMode", 1);
                PlayerPrefs.Save();

                string sceneToLoad = LobbyManager.GetSceneNameForGameType(roomData.gameType);

                // 1. Tell FishNet to connect. FishNet's SceneManager will automatically 
                //    download and load the host's active scene (e.g., DrawingScene) for us.
                VRNetworkManager.Instance.JoinRemoteSession(roomData.roomAddress);

                // 2. Wait for FishNet to finish loading the networked scene
                feedbackText.text = "Syncing Environment...";
                float timeout = 10f;
                float timer = 0f;

                while (!SceneManager.GetSceneByName(sceneToLoad).isLoaded)
                {
                    timer += Time.deltaTime;
                    if (timer > timeout)
                    {
                        feedbackText.text = "Connection timed out. Host may be offline.";
                        joinButton.interactable = true;
                        return;
                    }
                    await Task.Yield();
                }

                // 3. Set the newly networked scene as active and destroy the Login scene
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
                SceneManager.UnloadSceneAsync(gameObject.scene.name);
            }
            else
            {
                Debug.LogError("JoinRoomPanel: Room not found or network error.");
                feedbackText.text = "Invalid or expired Room Code.";
                joinButton.interactable = true;
            }
        });
    }
}
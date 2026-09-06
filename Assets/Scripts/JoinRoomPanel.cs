using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Assets.Database.DatabaseManagement;

public class JoinRoomPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinButton;

    private void Start()
    {
        if (roomCodeInput != null && roomCodeInput.GetComponent<VRKeyboardInputField>() == null)
            roomCodeInput.gameObject.AddComponent<VRKeyboardInputField>();

        joinButton.onClick.AddListener(async () =>
        {
            string code = roomCodeInput.text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code)) return;

            joinButton.interactable = false;

            var roomData = await DatabaseManager.Instance.JoinRoom(code);

            if (roomData != null)
            {
                // Inject the joined room's context into PlayerPrefs
                PlayerPrefs.SetString("CurrentSessionID", roomData.sessionID);
                PlayerPrefs.SetString("DrawingToLoad", roomData.drawingID);
                PlayerPrefs.SetInt("LoadMode", 1);
                PlayerPrefs.Save();

                VRNetworkManager.Instance.JoinSession("localhost");

                // Get the correct environment based on the room's GameType
                string sceneToLoad = LobbyManager.GetSceneNameForGameType(roomData.gameType);

                SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
                SceneManager.UnloadSceneAsync(gameObject.scene.name); // Cleanly unload Lobby
            }
            else
            {
                Debug.LogError("JoinRoomPanel: Room not found or network error.");
                joinButton.interactable = true;
            }
        });
    }
}
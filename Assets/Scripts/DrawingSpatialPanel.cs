using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Assets.Database.DatabaseManagement.MongoDB;

public class DrawingSpatialPanel : MonoBehaviour
{
    [Header("Save Drawing Options")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TMP_InputField drawingNameInput;
    [SerializeField] private Button saveDrawingButton;

    [Header("Back to lobby")]
    [SerializeField] private Button backToLobbyButton;

    void Start()
    {
        if (saveDrawingButton != null)
        {
            saveDrawingButton.onClick.AddListener(() =>
            {
                if (feedbackText != null) feedbackText.text = "Sending save request...";
                Debug.Log("Save button clicked in DrawingSpatialPanel.");

                // Use the updated AuthManager to get the ID
                string currentUserId = AuthManager.GetCurrentUserID();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    Debug.LogError("Cannot save drawing: UserID not found.");
                    if (feedbackText != null) feedbackText.text = "Error: you are not logged in.";
                    return;
                }

                string drawingName = drawingNameInput != null && !string.IsNullOrWhiteSpace(drawingNameInput.text)
                    ? drawingNameInput.text
                    : $"Drawing_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Find the authoritative network drawing object
                GameObject drawingContainer = GameObject.FindGameObjectWithTag("Drawing");
                if (drawingContainer == null || !drawingContainer.TryGetComponent<VRDrawing>(out var vrDrawing))
                {
                    Debug.LogError("VRDrawing component not found in the scene! Cannot save network drawing.");
                    if (feedbackText != null) feedbackText.text = "Error: Drawing object missing.";
                    return;
                }

                // Retrieve context
                GameType currentGameType = (GameType)PlayerPrefs.GetInt("SelectedGameType", (int)GameType.INNER_CHILD);
                string currentSessionId = PlayerPrefs.GetString("CurrentSessionID", "NoSession");

                // Trigger the network save command on the VRDrawing object
                vrDrawing.RequestSaveDrawing(drawingName, currentUserId, currentGameType, currentSessionId);

                if (feedbackText != null) feedbackText.text = "Save request sent to server!";
                if (drawingNameInput != null) drawingNameInput.text = "";
            });
        }
        else Debug.LogWarning("Save Button is not assigned!");

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.AddListener(() =>
            {
                Debug.Log("Returning to Lobby Scene from Drawing Scene...");

                if (FindObjectOfType<MusicManager>() != null)
                {
                    FindObjectOfType<MusicManager>().StopMusic();
                }

                VRDrawing vrDrawing = FindObjectOfType<VRDrawing>();
                if (vrDrawing != null)
                {
                    foreach (Transform child in vrDrawing.transform) Destroy(child.gameObject);
                    vrDrawing.drawing.lines.Clear();
                }

                string environmentSceneName = gameObject.scene.name;
                SceneManager.LoadScene("Login", LoadSceneMode.Additive);
                SceneManager.UnloadSceneAsync(environmentSceneName);
            });
        }
        else Debug.LogWarning("Back to Lobby Button is not assigned!");
    }
}
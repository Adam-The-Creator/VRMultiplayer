using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;

// Alias to prevent conflict with MongoDB.Drawing
using DrawingMeta = Assets.Database.DatabaseManagement.SQLiteDB.Drawing;

public class DrawingSpatialPanel : MonoBehaviour
{
    [Header("Save Drawing Options")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TMP_InputField drawingNameInput;
    [SerializeField] private Button saveDrawingButton;
    [SerializeField] private Button saveAsNewDrawingButton;

    [Header("Back to lobby")]
    [SerializeField] private Button backToLobbyButton;

    private string originalName = "";
    private string loadedDrawingId = "";
    private bool isLoaded = false;

    async void Start()
    {
        // 1. Determine if we are editing an existing drawing
        int loadMode = PlayerPrefs.GetInt("LoadMode", 0);
        loadedDrawingId = PlayerPrefs.GetString("DrawingToLoad", "");
        isLoaded = loadMode == 1 && !string.IsNullOrEmpty(loadedDrawingId);

        if (isLoaded)
        {
            // Fetch the drawing's original metadata from the database
            DrawingMeta meta = await DatabaseManager.Instance.GetDrawingMetaById(loadedDrawingId);
            if (meta != null && drawingNameInput != null)
            {
                originalName = meta.name;
                drawingNameInput.text = originalName;
            }
        }
        else
        {
            // If it is a brand new drawing, hide the "Save As New" button entirely
            if (saveAsNewDrawingButton != null)
            {
                saveAsNewDrawingButton.gameObject.SetActive(false);
            }
        }

        // 2. Setup the "Save As New" interactability logic
        if (drawingNameInput != null && saveAsNewDrawingButton != null && isLoaded)
        {
            saveAsNewDrawingButton.interactable = false; // Disabled by default until name changes

            drawingNameInput.onValueChanged.AddListener((string newValue) =>
            {
                saveAsNewDrawingButton.interactable = (newValue != originalName);
            });
        }

        // 3. Main Save Button Logic
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

                if (isLoaded)
                {
                    // Update existing record
                    vrDrawing.RequestUpdateDrawing(loadedDrawingId, drawingName);

                    // Reset original name so SaveAsNew button disables again
                    originalName = drawingName;
                    if (saveAsNewDrawingButton != null) saveAsNewDrawingButton.interactable = false;
                }
                else
                {
                    // Save as completely new
                    GameType currentGameType = (GameType)PlayerPrefs.GetInt("SelectedGameType", (int)GameType.INNER_CHILD);
                    string currentSessionId = PlayerPrefs.GetString("CurrentSessionID", "NoSession");
                    vrDrawing.RequestSaveDrawing(drawingName, currentUserId, currentGameType, currentSessionId);
                }

                if (feedbackText != null) feedbackText.text = "Save request sent to server!";
            });
        }

        // 4. Save As New Button Logic
        if (saveAsNewDrawingButton != null)
        {
            saveAsNewDrawingButton.onClick.AddListener(() =>
            {
                if (feedbackText != null) feedbackText.text = "Sending save as new request...";
                Debug.Log("Save As New button clicked.");

                string currentUserId = AuthManager.GetCurrentUserID();
                if (string.IsNullOrEmpty(currentUserId)) return;

                string drawingName = drawingNameInput != null && !string.IsNullOrWhiteSpace(drawingNameInput.text)
                    ? drawingNameInput.text
                    : $"Drawing_{DateTime.Now:yyyyMMdd_HHmmss}";

                GameObject drawingContainer = GameObject.FindGameObjectWithTag("Drawing");
                if (drawingContainer == null || !drawingContainer.TryGetComponent<VRDrawing>(out var vrDrawing)) return;

                GameType currentGameType = (GameType)PlayerPrefs.GetInt("SelectedGameType", (int)GameType.INNER_CHILD);
                string currentSessionId = PlayerPrefs.GetString("CurrentSessionID", "NoSession");

                // Save as a completely new drawing (does not modify loadedDrawingId, keeps it intact)
                vrDrawing.RequestSaveDrawing(drawingName, currentUserId, currentGameType, currentSessionId);

                if (feedbackText != null) feedbackText.text = "Saved as new drawing!";
            });
        }

        // 5. Back to Lobby Logic
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
    }
}
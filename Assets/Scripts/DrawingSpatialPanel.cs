using DrawingData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        if (drawingNameInput != null && drawingNameInput.GetComponent<VRKeyboardInputField>() == null)
        {
            drawingNameInput.gameObject.AddComponent<VRKeyboardInputField>();
            Debug.Log("DrawingSpatialPanel: Added VRKeyboardInputField to drawingNameInput");
        }

        if (saveDrawingButton != null)
        {
            saveDrawingButton.onClick.AddListener(() =>
            {
                if (feedbackText != null) feedbackText.text = "Save...";
                Debug.Log("Save button clicked in DrawingHandMenuLeft.");

                string currentUserId = PlayerPrefs.GetString("CurrentUserID", null);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    Debug.LogError("Cannot save drawing: UserID not found in PlayerPrefs.");
                    if (feedbackText != null) feedbackText.text = "Error: you are not logged in.";
                    return;
                }

                string drawingName;
                if (drawingNameInput != null && !string.IsNullOrWhiteSpace(drawingNameInput.text))
                {
                    drawingName = drawingNameInput.text;
                }
                else
                {
                    drawingName = $"Drawing_{DateTime.Now:yyyyMMdd_HHmmss}";
                    Debug.LogWarning("Drawing name input is empty or not assigned, using default generated name: " + drawingName);
                }

                GameObject drawingContainer = GameObject.FindGameObjectWithTag("Drawing");
                if (drawingContainer == null)
                {
                    Debug.LogWarning("Drawing container not found, creating one...");
                    drawingContainer = new GameObject("Drawing")
                    {
                        tag = "Drawing"
                    };
                    drawingContainer.AddComponent<VRDrawing>();
                    drawingContainer.GetComponent<VRDrawing>().drawing = new DrawingData.Drawing
                    {
                        metadata = new DrawingData.Metadata(),
                        lines = new List<DrawingData.Line>()
                    };
                }
                else
                {
                    VRDrawing vrDrawing = drawingContainer.GetComponent<VRDrawing>();
                    if (vrDrawing == null)
                    {
                        Debug.LogWarning("Drawing container found but VRDrawing component missing. Adding it...");
                        vrDrawing = drawingContainer.AddComponent<VRDrawing>();
                    }

                    if (vrDrawing.drawing == null)
                    {
                        Debug.LogWarning("VRDrawing.drawing is null. Initializing it...");
                        vrDrawing.drawing = new DrawingData.Drawing
                        {
                            metadata = new DrawingData.Metadata(),
                            lines = new List<DrawingData.Line>()
                        };
                    }

                    if (vrDrawing.drawing.lines == null)
                    {
                        vrDrawing.drawing.lines = new List<DrawingData.Line>();
                    }

                    if (vrDrawing.drawing.metadata == null)
                    {
                        vrDrawing.drawing.metadata = new DrawingData.Metadata();
                    }
                }

                GameType currentGameType = (GameType)PlayerPrefs.GetInt("SelectedGameType", (int)DrawingData.GameType.InnerChild);
                string currentSessionId = PlayerPrefs.GetString("CurrentSessionID", "NoSession");

                if (FileHandler.Instance == null)
                {
                    Debug.LogError("FileHandler instance not found!");
                    if (feedbackText != null) feedbackText.text = "Error: FileManager is not found.";
                    return;
                }
                string savedFilePath = FileHandler.Instance.ExportToJSON(drawingContainer, currentUserId, drawingName, currentGameType);

                if (!string.IsNullOrEmpty(savedFilePath))
                {
                    Debug.Log($"File saved to: {savedFilePath}. Updating database...");
                    if (DatabaseManager.Instance != null)
                    {
                        bool dbSuccess = DatabaseManager.Instance.SaveDrawing(currentUserId, drawingName, savedFilePath, currentGameType, currentSessionId);
                        if (dbSuccess)
                        {
                            Debug.Log($"Drawing metadata saved to database successfully! GameType: {currentGameType}");
                            if (feedbackText != null)
                            {
                                feedbackText.text = "Your drawing has been saved successfully!";
                            }
                            if (drawingNameInput != null) drawingNameInput.text = "";
                        }
                        else
                        {
                            Debug.LogError("Failed to save drawing metadata to database.");
                            if (feedbackText != null) feedbackText.text = "Database error!";
                        }
                    }
                    else
                    {
                        Debug.LogError("DatabaseManager instance not found. Cannot save metadata.");
                        if (feedbackText != null) feedbackText.text = "Database is not reachable!";
                    }
                }
                else
                {
                    Debug.LogError("Failed to save drawing to JSON file. Database not updated.");
                    if (feedbackText != null) feedbackText.text = "Error during save your drawing!";
                }
            });
        }
        else Debug.LogWarning("Save Button is not assigned!");

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.AddListener(() =>
            {
                Debug.Log("Returning to Lobby Scene from Drawing Scene...");
                
                if (MusicManager.Instance != null)
                {
                    MusicManager.Instance.StopMusic();
                    Debug.Log("Music stopped when exiting to lobby");
                }
                
                SceneManager.LoadScene("Scenes/Login");
            });
        }
        else Debug.LogWarning("Back to Lobby Button is not assigned!");
    }
}

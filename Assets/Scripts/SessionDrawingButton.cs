using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Assets.Database.DatabaseManagement;
using DrawingMeta = Assets.Database.DatabaseManagement.SQLiteDB.Drawing;

public class SessionDrawingButton : MonoBehaviour
{
    [SerializeField] private Button loadDrawingButton;
    [SerializeField] private TMP_Text drawingNameText;

    public string DrawingName
    {
        get => drawingNameText != null ? drawingNameText.text : "Drawing";
        set
        {
            if (drawingNameText != null)
            {
                drawingNameText.text = value;
            }
        }
    }

    // Updated to use the DrawingMeta type from SQLiteDB
    private DrawingMeta drawing;

    public event Action OnClick;

    // Converted to async void to await the database call
    public async void Initialize(string drawingId = null)
    {
        loadDrawingButton = GetComponent<Button>();
        loadDrawingButton.onClick.RemoveAllListeners();

        drawingNameText = GetComponentInChildren<TMP_Text>();
        drawingNameText.text = "Loading..."; // Optional: feedback while fetching

        OnClick = null;

        if (!string.IsNullOrEmpty(drawingId))
        {
            // Await the new asynchronous fetch method
            drawing = await DatabaseManager.Instance.GetDrawingMetaById(drawingId);

            // Use the updated camelCase property names
            DrawingName = drawing != null ? drawing.name : "Unknown Drawing";

            loadDrawingButton.interactable = drawing != null;

            if (drawing != null)
            {
                loadDrawingButton.onClick.AddListener(() => {
                    Debug.Log($"SessionDrawingButton: Load drawing with ID: {drawingId}");

                    // Directly use drawing.gameType without casting since they match
                    string sceneToLoad = LobbyManager.GetSceneNameForGameType(drawing.gameType);

                    if (sceneToLoad != null)
                    {
                        PlayerPrefs.SetString("DrawingToLoad", drawing.id);
                        PlayerPrefs.SetInt("LoadMode", 1);
                        PlayerPrefs.Save();
                        _ = NetworkSceneFlow.EnterGameSceneAsync(sceneToLoad); // additive, keeps Core alive
                    }
                    OnClick?.Invoke();
                });
            }
        }
        else
        {
            DrawingName = "Empty";
            loadDrawingButton.interactable = false;
            Debug.LogWarning("SessionDrawingButton: No drawing ID provided, button disabled.");
        }
    }
}
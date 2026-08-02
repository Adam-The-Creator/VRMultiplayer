using DrawingData;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SessionDrawingButton : MonoBehaviour
{
    [SerializeField] private Button loadDrawingButton;
    [SerializeField] private TMP_Text drawingNameText;
    public string DrawingName {
        get => drawingNameText != null ? drawingNameText.text : "Drawing";
        set {
            if (drawingNameText != null)
            {
                drawingNameText.text = value;
            }
        }
    }
    private DatabaseManager.Drawing drawing;

    public event Action OnClick;

    public void Initialize(string drawingId = null)
    {
        loadDrawingButton = GetComponent<Button>();
        loadDrawingButton.onClick.RemoveAllListeners();

        drawingNameText = GetComponentInChildren<TMP_Text>();
        drawingNameText.text = DrawingName;

        OnClick = null;

        if (drawingId != null)
        {
            drawing = DatabaseManager.Instance.GetDrawingById(drawingId);

            DrawingName = drawing != null ? drawing.Name : "Unknown Drawing";

            loadDrawingButton.onClick.AddListener(() => {
                Debug.Log($"SessionDrawingButton: Load drawing with ID: {drawingId}");
                string sceneToLoad = LobbyManager.GetSceneNameForGameType((GameType)drawing.GameType);
                if (sceneToLoad != null)
                {
                    PlayerPrefs.SetString("DrawingToLoad", drawing.Path);
                    PlayerPrefs.SetInt("LoadMode", 1);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene(sceneToLoad);
                }
                OnClick?.Invoke();
            });
        }
        else
        {
            loadDrawingButton.interactable = false;
            Debug.LogWarning("SessionDrawingButton: No drawing ID provided, button disabled.");
        }
    }
}

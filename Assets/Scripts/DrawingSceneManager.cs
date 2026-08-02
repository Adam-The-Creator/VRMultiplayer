using UnityEngine;

public class DrawingSceneManager : MonoBehaviour
{
    private string drawingPath = null;
    private const string PlayerPrefShowSadBoy = "ShowSadChild";
    private const string SadBoyName = "SadChild";
    private const string PlayerPrefsShowSadGirl = "ShowSadGirl";
    private const string SadGirlName = "SadGirl";

    void Start()
    {
        drawingPath = PlayerPrefs.GetString("DrawingToLoad", null);
        if (!string.IsNullOrEmpty(drawingPath))
        {
            Debug.Log($"DrawingSceneManager: Drawing loaded: {drawingPath}");
            LoadDrawing(drawingPath);
        }
        else
        {
            Debug.Log("DrawingSceneManager: No drawing path found in PlayerPrefs.");
        }

        string activeSessionId = DataAnalysisManager.GetCurrentSessionID();

        // Handle Sad Child visibility
        bool showSadChild = DatabaseManager.Instance.GetShowBoyForSession(activeSessionId);
        GameObject sadChild = GameObject.Find(SadBoyName);
        if (sadChild != null)
        {
            sadChild.SetActive(showSadChild);
            Debug.Log($"DrawingSceneManager: Sad Child visibility set to: {showSadChild}");
        }

        // Handle Sad Girl visibility
        bool showSadGirl = DatabaseManager.Instance.GetShowGirlForSession(activeSessionId);
        GameObject sadGirl = GameObject.Find(SadGirlName);
        if (sadGirl != null)
        {
            sadGirl.SetActive(showSadGirl);
            Debug.Log($"DrawingSceneManager: Sad Girl visibility set to: {showSadGirl}");
        }
    }

    private void LoadDrawing(string drawingPath)
    {
        FileHandler.Instance.ImportDrawingFromJSON(drawingPath, GameObject.FindGameObjectWithTag("Drawing"));
    }
}

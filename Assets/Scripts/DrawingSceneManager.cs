using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;

public class DrawingSceneManager : MonoBehaviour
{
    private string drawingId = null;
    private const string PlayerPrefShowSadBoy = "ShowSadChild";
    private const string SadBoyName = "SadChild";
    private const string PlayerPrefsShowSadGirl = "ShowSadGirl";
    private const string SadGirlName = "SadGirl";

    // Converted to async void to handle database fetches
    async void Start()
    {
        SceneManager.SetActiveScene(gameObject.scene);

        // The scene's session is decided once, below: the loaded drawing's session, or else the active one.
        SessionContext.SceneSessionId = null;

        drawingId = PlayerPrefs.GetString("DrawingToLoad", null);
        if (!string.IsNullOrEmpty(drawingId))
        {
            Debug.Log($"DrawingSceneManager: Loading drawing ID: {drawingId}");
            await LoadDrawingAsync(drawingId);
        }
        else
        {
            Debug.Log("DrawingSceneManager: No drawing ID found in PlayerPrefs. Starting fresh.");
            _ = ClearDrawingAsync();
        }

        // A loaded drawing has set its own session; a new drawing starts in the session that is active now.
        string sceneSessionId = await SessionContext.GetSceneSessionIdAsync();

        if (!string.IsNullOrEmpty(sceneSessionId))
        {
            // Handle Sad Child visibility
            bool showSadChild = await DatabaseManager.Instance.GetShowBoyForSession(sceneSessionId);
            GameObject sadChild = GameObject.Find(SadBoyName);
            if (sadChild != null)
            {
                sadChild.SetActive(showSadChild);
                Debug.Log($"DrawingSceneManager: Sad Child visibility set to: {showSadChild}");
            }

            // Handle Sad Girl visibility
            bool showSadGirl = await DatabaseManager.Instance.GetShowGirlForSession(sceneSessionId);
            GameObject sadGirl = GameObject.Find(SadGirlName);
            if (sadGirl != null)
            {
                sadGirl.SetActive(showSadGirl);
                Debug.Log($"DrawingSceneManager: Sad Girl visibility set to: {showSadGirl}");
            }
        }
    }

    private async Task LoadDrawingAsync(string id)
    {
        // Fetch the heavy 3D data from MongoDB
        Drawing drawingData = await DatabaseManager.Instance.GetDrawingDataById(id);

        if (drawingData == null)
        {
            Debug.LogError($"DrawingSceneManager: Failed to load drawing data for ID: {id}");
            return;
        }

        // The drawing stays in its own session, even if that is no longer the active one.
        if (!string.IsNullOrEmpty(drawingData.metadata?.sessionID)) SessionContext.SceneSessionId = drawingData.metadata.sessionID;

        VRDrawing vrDrawingComponent = await WaitForVRDrawingAsync();
        if (vrDrawingComponent == null)
        {
            Debug.LogError("DrawingSceneManager: No VRDrawing object found - cannot show the loaded drawing.");
            return;
        }

        // Builds the strokes (objects and erase colliders) and keeps whatever the other players already sent
        // while the drawing was being fetched.
        vrDrawingComponent.LoadDrawing(drawingData);

        // Handle PlacedModels restoration (if your scene utilizes the ModelPlacer script)
        if (drawingData.placedModels != null && drawingData.placedModels.Count > 0)
        {
            // Assuming your ModelPlacer script has a corresponding load method
            // ModelPlacer modelPlacer = FindObjectOfType<ModelPlacer>();
            // if (modelPlacer != null) modelPlacer.LoadPlacedModels(drawingData.placedModels);
            Debug.Log($"DrawingSceneManager: Restored {drawingData.placedModels.Count} placed models.");
        }

        Debug.Log("Drawing Scene successfully loaded from database.");
    }

    private async Task ClearDrawingAsync()
    {
        VRDrawing vrDrawingComponent = await WaitForVRDrawingAsync();
        if (vrDrawingComponent == null)
        {
            Debug.LogError("DrawingSceneManager: No VRDrawing object found - nothing to clear.");
            return;
        }

        vrDrawingComponent.ClearAll();
    }

    // The drawing lives in Core and is always active, but never wait forever for it.
    private static async Task<VRDrawing> WaitForVRDrawingAsync(float timeoutSeconds = 5f)
    {
        float start = Time.realtimeSinceStartup;
        VRDrawing found = FindObjectOfType<VRDrawing>();
        while (found == null && Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            await Task.Yield();
            found = FindObjectOfType<VRDrawing>();
        }
        return found;
    }
}
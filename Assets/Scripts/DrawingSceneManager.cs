using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

using Assets.Database.DatabaseManagement;
using Assets.Database.DatabaseManagement.MongoDB;

public class DrawingSceneManager : MonoBehaviour
{
    [SerializeField] private Material defaultLineMaterial;
    private string drawingId = null;
    private const string PlayerPrefShowSadBoy = "ShowSadChild";
    private const string SadBoyName = "SadChild";
    private const string PlayerPrefsShowSadGirl = "ShowSadGirl";
    private const string SadGirlName = "SadGirl";

    // Converted to async void to handle database fetches
    async void Start()
    {
        SceneManager.SetActiveScene(gameObject.scene);

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

        // Fetch session info asynchronously
        string activeSessionId = await DataAnalysisManager.GetCurrentSessionIDAsync();

        if (!string.IsNullOrEmpty(activeSessionId))
        {
            // Handle Sad Child visibility
            bool showSadChild = await DatabaseManager.Instance.GetShowBoyForSession(activeSessionId);
            GameObject sadChild = GameObject.Find(SadBoyName);
            if (sadChild != null)
            {
                sadChild.SetActive(showSadChild);
                Debug.Log($"DrawingSceneManager: Sad Child visibility set to: {showSadChild}");
            }

            // Handle Sad Girl visibility
            bool showSadGirl = await DatabaseManager.Instance.GetShowGirlForSession(activeSessionId);
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

        VRDrawing vrDrawingComponent = await WaitForVRDrawingAsync();
        if (vrDrawingComponent == null)
        {
            Debug.LogError("DrawingSceneManager: No VRDrawing object found - cannot show the loaded drawing.");
            return;
        }

        // Clean up existing children if any
        GameObject drawingObject = vrDrawingComponent.gameObject;
        foreach (Transform child in drawingObject.transform)
        {
            Destroy(child.gameObject);
        }

        // Reconstruct the LineRenderers visually
        if (drawingData.lines != null)
        {
            foreach (var ld in drawingData.lines)
            {
                // Determine if the line is erased
                bool isErased = ld.status == Status.ERASED;

                var lineObject = new GameObject(ld.id);
                lineObject.transform.SetParent(drawingObject.transform);

                var lr = lineObject.AddComponent<LineRenderer>();
                if (defaultLineMaterial != null) lr.material = defaultLineMaterial;
                else lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
                lr.useWorldSpace = true;

                lr.positionCount = ld.points.Count;
                lr.startWidth = ld.startWidth;
                lr.endWidth = ld.endWidth;

                // Convert custom LineColor DTO back to Unity's Color type
                lr.startColor = ld.startColor != null ? ld.startColor.ToColor() : Color.black;
                lr.endColor = ld.endColor != null ? ld.endColor.ToColor() : Color.black;

                for (int i = 0; i < ld.points.Count; i++)
                {
                    lr.SetPosition(i, ld.points[i].ToVector3());
                }

                // Add collision so they can be erased later
                if (lineObject.GetComponent<MeshCollider>() == null)
                {
                    var meshCollider = lineObject.AddComponent<MeshCollider>();
                    Mesh bakedMesh = new();
                    lr.BakeMesh(bakedMesh, true);
                    meshCollider.sharedMesh = bakedMesh;

                    if (isErased) meshCollider.enabled = false;
                }

                if (isErased) lr.enabled = false;

                lineObject.tag = "Line";
            }
        }

        vrDrawingComponent.drawing = drawingData;

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

        foreach (Transform child in vrDrawingComponent.transform)
        {
            Destroy(child.gameObject);
        }
        vrDrawingComponent.drawing.lines.Clear();
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
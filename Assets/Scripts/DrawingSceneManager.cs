using UnityEngine;
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
        drawingId = PlayerPrefs.GetString("DrawingToLoad", null);
        if (!string.IsNullOrEmpty(drawingId))
        {
            Debug.Log($"DrawingSceneManager: Loading drawing ID: {drawingId}");
            await LoadDrawingAsync(drawingId);
        }
        else
        {
            Debug.Log("DrawingSceneManager: No drawing ID found in PlayerPrefs. Starting fresh.");
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

        GameObject drawingObject = GameObject.FindGameObjectWithTag("Drawing");
        if (drawingObject == null)
        {
            Debug.LogError("DrawingSceneManager: Drawing container not found in scene!");
            return;
        }

        // Clean up existing children if any
        foreach (Transform child in drawingObject.transform)
        {
            Destroy(child.gameObject);
        }

        // Reconstruct the LineRenderers visually
        if (drawingData.lines != null)
        {
            foreach (var ld in drawingData.lines)
            {
                // Optionally skip rendering erased lines
                if (ld.status == Status.ERASED) continue;

                var lineObject = new GameObject(ld.id);
                lineObject.transform.SetParent(drawingObject.transform);

                var lr = lineObject.AddComponent<LineRenderer>();

                // You may want to assign your specific drawing material here instead of Default
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.positionCount = ld.points.Count;
                lr.startWidth = ld.startWidth;
                lr.endWidth = ld.endWidth;

                // Convert your custom LineColor DTO back to Unity's Color type
                lr.startColor = ld.startColor.ToColor();
                lr.endColor = ld.endColor.ToColor();

                for (int i = 0; i < ld.points.Count; i++)
                {
                    lr.SetPosition(i, ld.points[i].ToVector3());
                }

                // Add collision so they can be erased later
                if (lineObject.GetComponent<MeshCollider>() == null)
                {
                    var meshCollider = lineObject.AddComponent<MeshCollider>();
                    Mesh bakedMesh = new Mesh();
                    lr.BakeMesh(bakedMesh, true);
                    meshCollider.sharedMesh = bakedMesh;
                }

                lineObject.tag = "Line";
            }
        }

        // Apply the data payload to the local VRDrawing component so it can be updated
        if (drawingObject.TryGetComponent<VRDrawing>(out var vrDrawingComponent))
        {
            vrDrawingComponent.drawing = drawingData;
        }

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
}
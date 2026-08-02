using DrawingData;
using System;
using System.IO;
using UnityEngine;

public class FileHandler : MonoBehaviour
{
    public class Folder
    {
        public string path;
        public string name;
        public static implicit operator string(Folder folder) => folder.path;
        public Folder(string path, string name)
        {
            this.path = path;
            this.name = name;
        }
    }

    public static FileHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static readonly Folder DocumentsFolder = new(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
        Path.GetFileName(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
    );

    public static readonly Folder GameRootFolder = new(Path.Combine(DocumentsFolder, "VRDrawing3D"), "VRDrawing3D");

    public static readonly Folder DrawingsFolder = new(Path.Combine(GameRootFolder, "Drawings"), "Drawings");

    public static readonly Folder ReportsFolder = new(Path.Combine(GameRootFolder, "Reports"), "Reports");

    public string ExportToJSON(GameObject drawingObject, string userId, string drawingName, GameType gameType = GameType.InnerChild)
    {
        if (drawingObject == null)
        {
            Debug.LogError("Drawing object to save is null!");
            return null;
        }

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID is null or empty, cannot save drawing context correctly.");
            return null;
        }

        if (string.IsNullOrEmpty(drawingName))
        {
            Debug.LogWarning("Drawing name is null or empty, using a default name.");
            drawingName = $"Drawing_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        string userDirectory = Path.Combine(DrawingsFolder, userId);
        if (!Directory.Exists(userDirectory))
        {
            try { Directory.CreateDirectory(userDirectory); }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create user directory '{userDirectory}': {e.Message}");
                return null;
            }
        }

        string drawingFileName = string.Join("_", drawingName.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(drawingFileName)) drawingFileName = $"VRDrawing_{DateTime.Now:yyyyMMdd_HHmmss}";

        string drawingFolderPath = Path.Combine(userDirectory, drawingFileName);
        if (!Directory.Exists(drawingFolderPath))
        {
            try
            {
                Directory.CreateDirectory(drawingFolderPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create drawing folder '{drawingFolderPath}': {e.Message}");
                return null;
            }
        }

        string jsonDrawingPath = Path.Combine(drawingFolderPath, drawingFileName + (drawingFileName.EndsWith(".json") ? "" : ".json"));
        string currentSessionId = DataAnalysisManager.GetCurrentSessionID() ?? "";
        int showBoy = Convert.ToInt32(DatabaseManager.Instance.GetShowBoyForSession(currentSessionId));
        int showGirl = Convert.ToInt32(DatabaseManager.Instance.GetShowGirlForSession(currentSessionId));
        GameObject sadBoy = GameObject.Find("SadChild");
        GameObject sadGirl = GameObject.Find("SadGirl");
        Transform childTransform = null;
        if ((showBoy == 1) && !(showGirl == 1) && sadBoy != null)
        {
            childTransform = sadBoy.transform;
        }
        else if (!(showBoy == 1) && (showGirl == 1) && sadGirl != null)
        {
            childTransform = sadGirl.transform;
        }
        
        Vector3 childPosition;
        if (childTransform != null)
        {
            childPosition = childTransform.position;
        }
        else if (sadBoy != null && sadGirl != null)
        {
            childPosition = 0.5f * (sadBoy.transform.position + sadGirl.transform.position);
        }
        else if (sadBoy != null)
        {
            childPosition = sadBoy.transform.position;
        }
        else if (sadGirl != null)
        {
            childPosition = sadGirl.transform.position;
        }
        else
        {
            childPosition = Vector3.zero;
            Debug.LogWarning("FileHandler: Neither SadChild nor SadGirl found in scene. Using Vector3.zero for sadChildCoordinates.");
        }
        
        SadChildCoordinates sadChildCoordinates = new(childPosition);

        if (!drawingObject.TryGetComponent<VRDrawing>(out var vrDrawingComponent))
        {
            Debug.LogError("FileHandler: Drawing object does not have VRDrawing component. Adding it...");
            vrDrawingComponent = drawingObject.AddComponent<VRDrawing>();
        }

        if (vrDrawingComponent.drawing == null)
        {
            Debug.LogWarning("FileHandler: VRDrawing.drawing is null. Initializing it...");
            vrDrawingComponent.drawing = new DrawingData.Drawing
            {
                metadata = new DrawingData.Metadata(),
                lines = new System.Collections.Generic.List<DrawingData.Line>()
            };
        }

        if (vrDrawingComponent.drawing.metadata == null)
        {
            vrDrawingComponent.drawing.metadata = new DrawingData.Metadata();
        }

        if (vrDrawingComponent.drawing.lines == null)
        {
            vrDrawingComponent.drawing.lines = new System.Collections.Generic.List<DrawingData.Line>();
        }

        if (vrDrawingComponent.drawing.placedModels == null)
        {
            vrDrawingComponent.drawing.placedModels = new System.Collections.Generic.List<DrawingData.PlacedModel>();
        }

        ModelPlacer modelPlacer = FindObjectOfType<ModelPlacer>();
        if (modelPlacer != null)
        {
            var placedModelsData = modelPlacer.GetPlacedModelsData();
            vrDrawingComponent.drawing.placedModels = placedModelsData;
        }
        else
        {
            vrDrawingComponent.drawing.placedModels = new System.Collections.Generic.List<DrawingData.PlacedModel>();
        }

        vrDrawingComponent.drawing.metadata.userId = userId;
        vrDrawingComponent.drawing.metadata.version = "0";
        vrDrawingComponent.drawing.metadata.date = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        vrDrawingComponent.drawing.metadata.gameType = gameType;
        vrDrawingComponent.drawing.metadata.sessionId = currentSessionId;
        vrDrawingComponent.drawing.metadata.showBoy = showBoy;
        vrDrawingComponent.drawing.metadata.showGirl = showGirl;
        vrDrawingComponent.drawing.metadata.sadChildCoordinates = sadChildCoordinates;

        var drawingData = vrDrawingComponent.drawing;

        string json = JsonUtility.ToJson(drawingData, prettyPrint: true);

        try
        {
            File.WriteAllText(jsonDrawingPath, json);
            Debug.Log($"Drawing saved to JSON: {jsonDrawingPath}");
            return jsonDrawingPath;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write JSON file to {jsonDrawingPath}: {e.Message}");
            return null;
        }
    }

    public void ImportDrawingFromJSON(string jsonPath, GameObject drawingObject, bool destroyCurrentDrawing = false)
    {
        if (!File.Exists(jsonPath)) { Debug.LogError("No JSON file!"); return; }

        string json = File.ReadAllText(jsonPath);
        var drawingData = JsonUtility.FromJson<DrawingData.Drawing>(json);

        if (destroyCurrentDrawing)
        {
            foreach (Transform child in drawingObject.transform) Destroy(child.gameObject);
            Debug.Log("Child objects of drawing object destroyed.");
        }

        foreach (var ld in drawingData.lines)
        {
            var lineObject = new GameObject(ld.id);
            lineObject.AddComponent<VRDrawing>();
            lineObject.transform.SetParent(drawingObject.transform);
            var lr = lineObject.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.positionCount = ld.points.Count;
            lr.startWidth = ld.startWidth;
            lr.endWidth = ld.endWidth;
            lr.startColor = ld.startColor;
            lr.endColor = ld.endColor;

            for (int i = 0; i < ld.points.Count; i++) lr.SetPosition(i, ld.points[i].ToVector3());

            if (lineObject.GetComponent<MeshCollider>() == null)
            {
                var meshCollider = lineObject.AddComponent<MeshCollider>();
                Mesh bakedMesh = new();
                lr.BakeMesh(bakedMesh, true);
                meshCollider.sharedMesh = bakedMesh;
            }

            lineObject.tag = "Line";

            drawingObject.GetComponent<VRDrawing>().drawing = drawingData;
        }

        if (drawingData.placedModels != null && drawingData.placedModels.Count > 0)
        {
            ModelPlacer modelPlacer = FindObjectOfType<ModelPlacer>();
            if (modelPlacer != null)
            {
                modelPlacer.LoadPlacedModels(drawingData.placedModels);
                Debug.Log($"Loaded {drawingData.placedModels.Count} placed models from JSON");
            }
            else
            {
                Debug.LogWarning("FileHandler: ModelPlacer not found in scene. Cannot load placed models.");
            }
        }

        Debug.Log("Drawing loaded from JSON");
    }
}
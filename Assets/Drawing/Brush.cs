using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using Assets.Database.DatabaseManagement.MongoDB;

public class Brush : MonoBehaviour
{
    public enum DrawingMode
    {
        None = 0,
        Draw = 1,
        Erase = 2,
        Grab = 3
    }


    [Header("Brush properties")]
    [SerializeField] private Transform brushTip;
    public Material tipMaterial;
    public Material paintingMaterial;
    [Range(0.0001f, 1f)] public float brushStartWidth = .003f;
    [Range(0.0001f, 1f)] public float brushEndWidth = .003f;
    public Color brushStartColor = Color.blue;
    public Color brushEndColor = Color.blue;
    public Hand handType;

    [Header("XR interaction")]
    [SerializeField] private InputActionReference paintingInputAction;

    [Header("Painting properties")]
    [SerializeField] private LineRenderer currentLine;
    [SerializeField] private List<string> pointTimestamps = new();  // TODO: IS IT NECESSARY?
    [SerializeField, Range(0.0001f, 0.1f)] float drawingTreshold = 0.01f;
    private int index = 0;
    private bool isPainting = false;
    public DrawingMode drawingMode = DrawingMode.None;
    [SerializeField] private DrawingHandMenuLeft drawingHandMenuLeft;
    [SerializeField] private DrawingHandMenuRight drawingHandMenuRight;
    [SerializeField] public GameObject drawingObject;


    private void Start()
    {
        if (drawingObject == null)
        {
            VRDrawing target = FindObjectOfType<VRDrawing>();
            if (target != null) drawingObject = target.gameObject;
        }
        tipMaterial.color = brushStartColor;
        drawingMode = DrawingMode.None;
    }

    private void Update()
    {
        if (ConfigurationManager.CurrentDrawingConfig.DrawingBlockedScenes.Contains(SceneManager.GetActiveScene().name)) return;

        isPainting = (float)paintingInputAction.action.ReadValue<float>() > 0.5f;

        if (!drawingHandMenuLeft.handMenuEnableState && !drawingHandMenuRight.handMenuEnableState)
        {
            if (drawingMode == DrawingMode.Draw)
            {
                if (isPainting) Paint();
                else if (currentLine != null)
                {
                    if (currentLine.gameObject.GetComponent<MeshCollider>() == null)
                    {
                        MeshCollider meshCollider = currentLine.gameObject.AddComponent<MeshCollider>();
                        Mesh bakedMesh = new();
                        currentLine.BakeMesh(bakedMesh, useTransform: true);
                        meshCollider.sharedMesh = bakedMesh;
                    }
                    currentLine.gameObject.tag = "Line";
                    currentLine = null;
                }
            }
            else if (drawingMode == DrawingMode.Erase)
            {
                if (isPainting) Erase();
            }
        }
    }

    public void SetStartColor(Color color)
    {
        brushStartColor = color;
        if (tipMaterial != null && transform.Find("Tip") != null && transform.Find("Tip").GetComponent<MeshRenderer>() != null)
        {
            Material tipMaterialInstance = gameObject.transform.Find("Tip").GetComponent<MeshRenderer>().material;
            tipMaterialInstance.color = color;
        }
        else Debug.LogWarning("Brush tip material or Tip GameObject/MeshRenderer not found for ColorPickerStart.");
    }

    public void SetEndColor(Color color)
    {
        brushEndColor = color;
    }

    private void Paint()
    {
        if (drawingObject == null)
        {
            VRDrawing target = FindObjectOfType<VRDrawing>();
            if (target != null) drawingObject = target.gameObject;
            else
            {
                Debug.LogWarning("Paint skipped: VRDrawing object not yet available in scene.");
                return;
            }
        }
        if (currentLine == null)
        {
            /* Initialize the LineRenderer with the first position */
            index = 0;
            currentLine = new GameObject(name: $"{GetUniqueLineID()}").AddComponent<LineRenderer>();
            currentLine.material = paintingMaterial;
            currentLine.useWorldSpace = true;
            currentLine.startColor = brushStartColor;
            currentLine.endColor = brushEndColor;
            currentLine.startWidth = brushStartWidth;
            currentLine.endWidth = brushEndWidth;
            currentLine.positionCount = 1;
            currentLine.SetPosition(0, brushTip.position);
            currentLine.transform.SetParent(drawingObject.transform);

            /* Bundle the data and send it to the Server via RPC */
            if (drawingObject.TryGetComponent<VRDrawing>(out VRDrawing vrDrawingData))
            {
                Enum.TryParse(handType.ToString(), out Hand parsedHand);
                string playerID = AuthManager.GetCurrentUserID();

                Line newLine = new()
                {
                    id = currentLine.name,
                    points = new List<Point>(),
                    startWidth = brushStartWidth,
                    endWidth = brushEndWidth,
                    startColor = new LineColor(brushStartColor),
                    endColor = new LineColor(brushEndColor),
                    hand = parsedHand,
                    userID = playerID,
                    history = new List<LineEvent>
                    {
                        new() {
                            eventType = LineEventType.DRAW,
                            invoker = playerID,
                            timestamp = GetTimestamp(),
                            hand = parsedHand
                        }
                    },
                    status = Status.DRAWN
                };

                pointTimestamps.Clear();
                string initialTimestamp = GetTimestamp();
                pointTimestamps.Add(initialTimestamp);
                newLine.points.Add(new Point(brushTip.position, initialTimestamp));

                // Dispatch to network
                vrDrawingData.AddNewLine(newLine);
            }
            else
            {
                //Suppress annoying error messages in scenes where drawing is blocked
                if (ConfigurationManager.CurrentDrawingConfig.DrawingBlockedScenes.Contains(SceneManager.GetActiveScene().name)) Debug.LogWarning("Drawing is blocked in this scene.");
                else Debug.LogError("The drawingObject does not have a VRDrawing component attached.");
            }
        }
        else
        {
            /* Add new points to the current line locally */
            var currentPosition = currentLine.GetPosition(index);
            if (Vector3.Distance(currentPosition, brushTip.position) > drawingTreshold)
            {
                index++;
                currentLine.positionCount = index + 1;
                currentLine.SetPosition(index, brushTip.position);

                /* Dispatch the new point to the network via RPC */
                if (drawingObject.TryGetComponent<VRDrawing>(out VRDrawing vrDrawingData))
                {
                    string pointTimestamp = GetTimestamp();
                    pointTimestamps.Add(pointTimestamp);

                    Point newPoint = new(brushTip.position, pointTimestamp);
                    vrDrawingData.AddNewPointToLine(currentLine.name, newPoint);
                }
                else
                {
                    if (ConfigurationManager.CurrentDrawingConfig.DrawingBlockedScenes.Contains(SceneManager.GetActiveScene().name)) Debug.LogWarning("Drawing is blocked in this scene.");
                    else Debug.LogError("The drawingObject does not have a VRDrawing component attached.");
                }
            }
        }
    }

    private void Erase()
    {
        if (Physics.Raycast(brushTip.position, brushTip.forward, out RaycastHit hit, 100f))
        {
            if (hit.collider.CompareTag("Line"))
            {
                if (drawingObject.TryGetComponent<VRDrawing>(out VRDrawing vrDrawingData))
                {
                    Enum.TryParse(handType.ToString(), out Hand parsedHand);
                    string playerID = AuthManager.GetCurrentUserID();

                    if (ConfigurationManager.CurrentDrawingConfig.DestroyLineOnErase)
                    {
                        // If fully destroying, we might want a new CmdDestroyLine, 
                        // but for now we will still flag it erased on the network so the DB knows.
                        vrDrawingData.EraseLine(hit.collider.gameObject.name, playerID, GetTimestamp(), parsedHand);
                        Destroy(hit.collider.gameObject);
                    }
                    else
                    {
                        // Notify the server about the erasure
                        vrDrawingData.EraseLine(hit.collider.gameObject.name, playerID, GetTimestamp(), parsedHand);

                        // Disable the renderer locally to make it instantly invisible
                        if (hit.collider.gameObject.TryGetComponent<Renderer>(out Renderer lineRenderer))
                        {
                            lineRenderer.enabled = false;
                        }

                        // Disable the collider locally to prevent further interaction
                        hit.collider.enabled = false;
                    }
                }
            }
        }
    }

    private string GetTimestamp() => DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_") + ((int)(DateTime.Now.Millisecond)).ToString("D3");
    private string GetUniqueLineID() => $"Line_{GetTimestamp()}_{Guid.NewGuid()}";
}

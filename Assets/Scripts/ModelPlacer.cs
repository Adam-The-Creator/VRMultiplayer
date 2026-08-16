//using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;
//using UnityEngine.XR.Interaction.Toolkit.Interactors;
//using UnityEngine.InputSystem;
//using System.Linq;
//using System.Collections.Generic;
//using DrawingData;

//public class ModelPlacer : MonoBehaviour
//{
//    [Header("VR Interaction")]
//    [SerializeField] private XRRayInteractor leftRayInteractor;
//    [SerializeField] private XRRayInteractor rightRayInteractor;
//    [SerializeField] private InputActionReference leftActivateAction;
//    [Tooltip("Auto-detect Activate action from XRI Default Input Actions. If false, uses manually assigned action.")]
//    [SerializeField] private bool autoDetectActions = true;
    
//    [Header("Drawing Mode Check")]
//    [SerializeField] private Brush brushObject;
//    [SerializeField] private DrawingHandMenuLeft drawingHandMenuLeft;
//    [Tooltip("Only allow placement when drawing mode is set to Grab")]
//    [SerializeField] private bool requireGrabMode = true;

//    [Header("Placement Settings")]
//    [SerializeField] private float placementDistance = 5f;
//    [SerializeField] private LayerMask placementLayerMask = -1;
//    [SerializeField] private bool placeOnSurface = true;
//    [SerializeField] private Transform placementParent;
    
//    [Header("Keyboard/Mouse Testing")]
//    [SerializeField] private bool enableKeyboardMouseTesting = true;
//    [SerializeField] private UnityEngine.InputSystem.Key placeKey = UnityEngine.InputSystem.Key.Space;

//    [Header("Visual Feedback")]
//    [SerializeField] private GameObject placementPreviewPrefab;
//    [SerializeField] private Material previewMaterial;

//    private ModelManager modelManager;
//    private bool isPlacementModeActive = false;
//    private GameObject currentPreview;
//    private XRRayInteractor activeRayInteractor;
//    private InputAction leftActivateActionDirect;
//    private float lastPlacementTime = 0f;
//    private List<GameObject> placedModels = new List<GameObject>();
//    private Dictionary<GameObject, string> placedModelNames = new Dictionary<GameObject, string>();
//    private const float PLACEMENT_COOLDOWN = 0.2f;

//    void Start()
//    {
//        modelManager = ModelManager.Instance;
//        if (modelManager == null)
//        {
//            modelManager = FindObjectOfType<ModelManager>();
//        }

//        if (brushObject == null)
//        {
//            brushObject = FindObjectOfType<Brush>();
//        }
//        if (brushObject == null)
//        {
//            Debug.LogWarning("ModelPlacer: Brush object not found. Will not check for Grab mode (fallback).");
//        }
        
//        if (drawingHandMenuLeft == null)
//        {
//            drawingHandMenuLeft = FindObjectOfType<DrawingHandMenuLeft>();
//        }

//        if (leftRayInteractor == null)
//        {
//            leftRayInteractor = FindRayInteractor("Left");
//            if (leftRayInteractor == null)
//            {
//                leftRayInteractor = FindRayInteractorByHandedness(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Left);
//            }
//        }
//        if (rightRayInteractor == null)
//        {
//            rightRayInteractor = FindRayInteractor("Right");
//            if (rightRayInteractor == null)
//            {
//                rightRayInteractor = FindRayInteractorByHandedness(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Right);
//            }
//        }

//        if (autoDetectActions)
//        {
//            if (leftActivateAction == null)
//            {
//                AutoDetectActivateActions();
//            }
//        }
//    }

//    void Update()
//    {
//        if (isPlacementModeActive)
//        {
//            UpdatePlacementPreview();
            
//            if (leftActivateAction == null && leftActivateActionDirect == null)
//            {
//                CheckForPlacementInput();
//            }
            
//            if (enableKeyboardMouseTesting)
//            {
//                if (Keyboard.current != null && Keyboard.current[placeKey].wasPressedThisFrame)
//                {
//                    bool isMoving = (Keyboard.current[Key.W].isPressed || 
//                                    Keyboard.current[Key.A].isPressed || 
//                                    Keyboard.current[Key.S].isPressed || 
//                                    Keyboard.current[Key.D].isPressed);
                    
//                    if (!isMoving)
//                    {
//                        TryPlaceModelWithMouse();
//                    }
//                }
//            }
//        }
//    }
    
//    private void TryPlaceModelWithMouse()
//    {
//        if (!isPlacementModeActive) return;
        
//        if (!IsGrabModeActive())
//        {
//            if (Time.frameCount % 30 == 0)
//            {
//                Debug.LogWarning("ModelPlacer: Grab mode not active. Please select 'Grab' mode from the LEFT hand menu.");
//            }
//            return;
//        }
        
//        if (modelManager == null) return;
        
//        GameObject selectedPrefab = modelManager.GetSelectedModelPrefab();
//        if (selectedPrefab == null)
//        {
//            Debug.LogWarning("ModelPlacer: No model selected for keyboard/mouse placement");
//            return;
//        }
        
//        if (leftRayInteractor != null)
//        {
//            TryPlaceModel(leftRayInteractor);
//            return;
//        }
        
//        Camera mainCamera = Camera.main;
//        if (mainCamera == null)
//        {
//            mainCamera = FindObjectOfType<Camera>();
//        }
        
//        if (mainCamera == null)
//        {
//            Debug.LogWarning("ModelPlacer: No camera or controller found for keyboard/mouse placement");
//            return;
//        }
        
//        Vector3 rayOrigin = mainCamera.transform.position;
//        Vector3 rayDirection = mainCamera.transform.forward;
//        float maxDistance = 30f;
//        Ray ray = new Ray(rayOrigin, rayDirection);
//        RaycastHit hit;
        
//        bool hasHit = Physics.Raycast(ray, out hit, maxDistance, placementLayerMask);
        
//        if (!hasHit)
//        {
//            hasHit = Physics.Raycast(ray, out hit, maxDistance);
//        }
        
//        if (!hasHit)
//        {
//            hasHit = Physics.Raycast(ray, out hit, 50f);
//        }
        
//        if (hasHit && hit.collider != null)
//        {
//            if (hit.distance > 30f)
//            {
//                Debug.LogWarning($"ModelPlacer: Hit surface is too far away ({hit.distance:F1} units). Point camera closer to the surface. Max distance: 30 units.");
//                return;
//            }
            
//            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("UI") || 
//                hit.collider.GetComponent<UnityEngine.UI.Graphic>() != null ||
//                hit.collider.GetComponent<UnityEngine.Canvas>() != null)
//            {
//                Debug.LogWarning("ModelPlacer: Cannot place model on UI element. Point camera at a 3D surface instead.");
//                return;
//            }
            
//            GameObject hitObject = hit.collider.gameObject;
//            string hitName = hitObject.name.ToLower();
            
//            if (HasTeleportTag(hitObject))
//            {
//                Debug.LogWarning($"ModelPlacer: Cannot place model on teleport surface '{hitObject.name}'. Point camera at a regular surface instead.");
//                return;
//            }
            
//            if (hitName.Contains("teleport") && !hitName.Contains("floor") && !hitName.Contains("ground"))
//            {
//                Debug.LogWarning($"ModelPlacer: Cannot place model on teleport surface '{hitObject.name}'. Point camera at a regular surface instead.");
//                return;
//            }
            
//            PlaceModel(selectedPrefab, hit.point, hit.normal);
//        }
//        else
//        {
//            Debug.LogWarning("ModelPlacer: Camera raycast didn't hit anything. Point camera at a surface and press Space.");
//        }
//    }

//    private void CheckForPlacementInput()
//    {
//        if (!IsGrabModeActive())
//        {
//            return;
//        }
//    }

//    private bool IsSelectPressed(XRRayInteractor interactor)
//    {
//        return false;
//    }

//    private XRRayInteractor FindRayInteractor(string handName)
//    {
//        XRRayInteractor[] interactors = FindObjectsOfType<XRRayInteractor>(true);
//        foreach (var interactor in interactors)
//        {
//            if (interactor.name.Contains(handName, System.StringComparison.OrdinalIgnoreCase))
//            {
//                return interactor;
//            }
            
//            Transform parent = interactor.transform.parent;
//            while (parent != null)
//            {
//                if (parent.name.Contains(handName, System.StringComparison.OrdinalIgnoreCase))
//                {
//                    return interactor;
//                }
//                parent = parent.parent;
//            }
//        }
//        return null;
//    }

//    private XRRayInteractor FindRayInteractorByHandedness(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness handedness)
//    {
//        XRRayInteractor[] interactors = FindObjectsOfType<XRRayInteractor>(true);
//        foreach (var interactor in interactors)
//        {
//            if (interactor.handedness == handedness)
//            {
//                return interactor;
//            }
//        }
//        return null;
//    }

//    private void AutoDetectActivateActions()
//    {
//        InputActionAsset inputActionAsset = null;
        
//        inputActionAsset = Resources.Load<InputActionAsset>("XRI Default Input Actions");
        
//        #if UNITY_EDITOR
//        if (inputActionAsset == null)
//        {
//            string[] guids = UnityEditor.AssetDatabase.FindAssets("XRI Default Input Actions t:InputActionAsset");
//            if (guids.Length > 0)
//            {
//                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
//                inputActionAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
//            }
//        }
//        #endif
        
//        if (inputActionAsset == null)
//        {
//            InputActionAsset[] allAssets = Resources.LoadAll<InputActionAsset>("");
//            foreach (var asset in allAssets)
//            {
//                if (asset.name.Contains("XRI") && asset.name.Contains("Input Actions"))
//                {
//                    inputActionAsset = asset;
//                    break;
//                }
//            }
//        }
        
//        if (inputActionAsset != null)
//        {
//            var leftActionMap = inputActionAsset.FindActionMap("XRI Left Interaction");
            
//            if (leftActionMap != null && this.leftActivateAction == null)
//            {
//                var leftActivate = leftActionMap.FindAction("Activate");
//                if (leftActivate != null)
//                {
//                    this.leftActivateActionDirect = leftActivate;
//                }
//                else
//                {
//                    Debug.LogWarning("ModelPlacer: Found 'XRI Left Interaction' map but 'Activate' action not found");
//                }
//            }
//            else if (leftActionMap == null)
//            {
//                Debug.LogWarning("ModelPlacer: 'XRI Left Interaction' action map not found in asset");
//            }
//        }
//        else
//        {
//            Debug.LogWarning("ModelPlacer: Could not find XRI Default Input Actions asset. Manual assignment required.");
//        }
//    }

//    public void EnablePlacementMode()
//    {
//        isPlacementModeActive = true;
//        lastPlacementTime = 0f;
        
//        if (modelManager == null)
//        {
//            Debug.LogError("ModelPlacer: ModelManager not found! Cannot place models.");
//            return;
//        }
        
//        GameObject selectedPrefab = modelManager.GetSelectedModelPrefab();
//        if (selectedPrefab == null)
//        {
//            Debug.LogWarning("ModelPlacer: No model selected! Please select a model from the dropdown first.");
//            return;
//        }
        
//        if (requireGrabMode && !IsGrabModeActive())
//        {
//            Debug.LogWarning("ModelPlacer: Drawing mode must be set to 'Grab' to place models. Please select Grab mode from the drawing menu dropdown.");
//            isPlacementModeActive = false;
//            return;
//        }
        
//        SubscribeToInputActions();
        
//        if (leftRayInteractor == null && rightRayInteractor == null)
//        {
//            if (leftRayInteractor == null)
//            {
//                leftRayInteractor = FindRayInteractor("Left");
//                if (leftRayInteractor == null)
//                {
//                    leftRayInteractor = FindRayInteractorByHandedness(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Left);
//                }
//            }
//            if (rightRayInteractor == null)
//            {
//                rightRayInteractor = FindRayInteractor("Right");
//                if (rightRayInteractor == null)
//                {
//                    rightRayInteractor = FindRayInteractorByHandedness(UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Right);
//                }
//            }
            
//            if (leftRayInteractor == null && rightRayInteractor == null)
//            {
//                Debug.LogError("ModelPlacer: No ray interactors found! Please assign them manually in the inspector or ensure XRRayInteractor components exist in the scene.");
//            }
//        }
        
//        if (leftActivateAction == null && leftActivateActionDirect == null)
//        {
//            Debug.LogWarning("ModelPlacer: No left activate (trigger) action found! Manual assignment required.");
//            if (!autoDetectActions)
//            {
//                Debug.LogWarning("ModelPlacer: 'Auto Detect Actions' is disabled. Enable it for automatic detection.");
//            }
//        }
//    }

//    public void DisablePlacementMode()
//    {
//        isPlacementModeActive = false;
//        UnsubscribeFromInputActions();
        
//        if (currentPreview != null)
//        {
//            Destroy(currentPreview);
//            currentPreview = null;
//        }
//    }
    
//    private void SubscribeToInputActions()
//    {
//        if (leftActivateAction != null && leftActivateAction.action != null)
//        {
//            if (!leftActivateAction.action.enabled)
//            {
//                leftActivateAction.action.Enable();
//            }
//            leftActivateAction.action.performed += OnLeftActivatePressed;
//        }
//        else if (leftActivateActionDirect != null)
//        {
//            if (!leftActivateActionDirect.enabled)
//            {
//                leftActivateActionDirect.Enable();
//            }
//            leftActivateActionDirect.performed += OnLeftActivatePressed;
//        }
//        else
//        {
//            Debug.LogWarning("ModelPlacer: Left activate (trigger) action not available! Placement will not work.");
//        }
//    }
    
//    private void UnsubscribeFromInputActions()
//    {
//        if (leftActivateAction != null && leftActivateAction.action != null)
//        {
//            leftActivateAction.action.performed -= OnLeftActivatePressed;
//        }
//        if (leftActivateActionDirect != null)
//        {
//            leftActivateActionDirect.performed -= OnLeftActivatePressed;
//        }
//    }

//    private void UpdatePlacementPreview()
//    {
//        if (modelManager == null) return;

//        GameObject selectedPrefab = modelManager.GetSelectedModelPrefab();
//        if (selectedPrefab == null) return;

//        XRRayInteractor rayInteractor = GetActiveRayInteractor();
//        if (rayInteractor == null) return;

//        RaycastHit hit;
//        bool hasHit = false;

//        if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
//        {
//            hasHit = true;
//        }
//        else
//        {
//            float maxDistance = Mathf.Max(placementDistance, 30f);
//            Ray ray = new Ray(rayInteractor.transform.position, rayInteractor.transform.forward);
//            hasHit = Physics.Raycast(ray, out hit, maxDistance, placementLayerMask);
//            if (!hasHit)
//            {
//                hasHit = Physics.Raycast(ray, out hit, maxDistance);
//            }
//        }

//        if (hasHit && hit.collider != null)
//        {
//            if (currentPreview == null)
//            {
//                CreatePreview(selectedPrefab);
//            }

//            if (currentPreview != null)
//            {
//                currentPreview.transform.position = hit.point;
//                if (placeOnSurface)
//                {
//                    currentPreview.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, hit.normal), hit.normal);
//                }
//            }
//        }
//        else
//        {
//            if (currentPreview != null)
//            {
//                Destroy(currentPreview);
//                currentPreview = null;
//            }
//        }
//    }

//    private void CreatePreview(GameObject prefab)
//    {
//        if (previewMaterial == null)
//        {
//            previewMaterial = new Material(Shader.Find("Standard"));
//            previewMaterial.SetFloat("_Mode", 3);
//            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
//            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
//            previewMaterial.SetInt("_ZWrite", 0);
//            previewMaterial.DisableKeyword("_ALPHATEST_ON");
//            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
//            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
//            previewMaterial.renderQueue = 3000;
//            Color previewColor = Color.white;
//            previewColor.a = 0.5f;
//            previewMaterial.color = previewColor;
//        }

//        currentPreview = Instantiate(prefab);
//        currentPreview.name = "PlacementPreview";

//        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
//        foreach (Renderer renderer in renderers)
//        {
//            Material[] materials = new Material[renderer.materials.Length];
//            for (int i = 0; i < materials.Length; i++)
//            {
//                materials[i] = previewMaterial;
//            }
//            renderer.materials = materials;
//        }

//        Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();
//        foreach (Collider collider in colliders)
//        {
//            collider.enabled = false;
//        }
//    }

//    private XRRayInteractor GetActiveRayInteractor()
//    {
//        return leftRayInteractor;
//    }

//    private void OnLeftActivatePressed(InputAction.CallbackContext context)
//    {
//        if (!isPlacementModeActive) return;
        
//        if (!IsGrabModeActive())
//        {
//            if (Time.frameCount % 30 == 0)
//            {
//                Debug.LogWarning("ModelPlacer: Cannot place - Grab mode not selected. Please select 'Grab' from the LEFT hand menu.");
//            }
//            return;
//        }
        
//        if (Time.time - lastPlacementTime < PLACEMENT_COOLDOWN)
//        {
//            return;
//        }
        
//        if (leftRayInteractor == null)
//        {
//            Debug.LogWarning("ModelPlacer: Left ray interactor is null! Cannot place model.");
//            return;
//        }
        
//        if (IsRayHittingUI(leftRayInteractor))
//        {
//            return;
//        }
        
//        if (IsRayHittingTeleport(leftRayInteractor))
//        {
//            return;
//        }
        
//        TryPlaceModel(leftRayInteractor);
//    }
    
//    private bool HasTeleportTag(GameObject obj)
//    {
//        if (obj == null) return false;
        
//        string tag = obj.tag;
//        return tag == "TeleportationArea" || 
//               tag == "TeleportationAnchor" ||
//               tag == "Teleport";
//    }
    
//    private bool IsRayHittingTeleport(XRRayInteractor rayInteractor)
//    {
//        if (rayInteractor == null) return false;
        
//        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
//        {
//            GameObject hitObject = hit.collider.gameObject;
            
//            if (HasTeleportTag(hitObject))
//            {
//                return true;
//            }
            
//            string objectName = hitObject.name.ToLower();
//            if (objectName.Contains("teleport") && !objectName.Contains("floor") && !objectName.Contains("ground"))
//            {
//                return true;
//            }
//            if (objectName.Contains("anchor") && !objectName.Contains("floor") && !objectName.Contains("ground"))
//            {
//                Transform anchorParent = hitObject.transform.parent;
//                bool isTeleportAnchor = false;
//                int anchorDepth = 0;
//                while (anchorParent != null && anchorDepth < 3)
//                {
//                    string parentName = anchorParent.name.ToLower();
//                    if (HasTeleportTag(anchorParent.gameObject) ||
//                        parentName.Contains("teleport"))
//                    {
//                        isTeleportAnchor = true;
//                        break;
//                    }
//                    anchorParent = anchorParent.parent;
//                    anchorDepth++;
//                }
//                if (isTeleportAnchor)
//                {
//                    return true;
//                }
//            }
            
//            Transform checkTransform = hitObject.transform;
//            int componentDepth = 0;
//            while (checkTransform != null && componentDepth < 5)
//            {
//                var components = checkTransform.GetComponents<Component>();
//                foreach (var component in components)
//                {
//                    if (component != null)
//                    {
//                        string componentType = component.GetType().Name.ToLower();
//                        if (componentType.Contains("teleport"))
//                        {
//                            return true;
//                        }
//                    }
//                }
                
//                checkTransform = checkTransform.parent;
//                componentDepth++;
//            }
//        }
        
//        if (rayInteractor.interactablesHovered != null && rayInteractor.interactablesHovered.Count > 0)
//        {
//            foreach (var interactable in rayInteractor.interactablesHovered)
//            {
//                if (interactable != null && interactable.transform != null)
//                {
//                    GameObject interactableObj = interactable.transform.gameObject;
//                    string objName = interactableObj.name.ToLower();
                    
//                    if (HasTeleportTag(interactableObj) ||
//                        objName.Contains("teleport") || objName.Contains("anchor"))
//                    {
//                        return true;
//                    }
//                }
//            }
//        }
        
//        return false;
//    }
    
//    private bool IsRayHittingUI(XRRayInteractor rayInteractor)
//    {
//        if (rayInteractor == null) return false;
        
//        try
//        {
//            var method = typeof(XRRayInteractor).GetMethod("TryGetCurrentUIRaycastResult");
//            if (method != null)
//            {
//                object[] parameters = new object[1];
//                bool result = (bool)method.Invoke(rayInteractor, parameters);
//                if (result && parameters[0] != null)
//                {
//                    var uiResult = parameters[0];
//                    var isValidProperty = uiResult.GetType().GetProperty("isValid");
//                    if (isValidProperty != null)
//                    {
//                        return (bool)isValidProperty.GetValue(uiResult);
//                    }
//                }
//            }
//        }
//        catch
//        {
//        }
        
//        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
//        {
//            GameObject hitObject = hit.collider.gameObject;
//            if (hitObject.layer == LayerMask.NameToLayer("UI"))
//            {
//                return true;
//            }
//            if (hitObject.GetComponent<UnityEngine.UI.Graphic>() != null ||
//                hitObject.GetComponent<UnityEngine.Canvas>() != null ||
//                hitObject.GetComponentInParent<UnityEngine.Canvas>() != null)
//            {
//                return true;
//            }
//        }
        
//        return false;
//    }
    
//    private bool IsGrabModeActive()
//    {
//        if (!requireGrabMode)
//        {
//            return true;
//        }
        
//        if (drawingHandMenuLeft == null)
//        {
//            drawingHandMenuLeft = FindObjectOfType<DrawingHandMenuLeft>();
//        }
        
//        if (drawingHandMenuLeft != null)
//        {
//            Brush.DrawingMode currentMode = drawingHandMenuLeft.drawingMode;
//            bool isGrabMode = currentMode == Brush.DrawingMode.Grab;
            
//            if (!isGrabMode)
//            {
//                if (Time.frameCount % 60 == 0)
//                {
//                    Debug.LogWarning($"ModelPlacer: Left hand drawing mode is '{currentMode}' (expected 'Grab'). Please select 'Grab' mode from the LEFT hand menu dropdown.");
//                }
//            }
//            return isGrabMode;
//        }
        
//        if (brushObject == null)
//        {
//            brushObject = FindObjectOfType<Brush>();
//        }
        
//        if (brushObject == null)
//        {
//            if (Time.frameCount % 60 == 0)
//            {
//                Debug.LogWarning("ModelPlacer: Brush object and left hand menu are null, cannot check Grab mode. Blocking placement for safety.");
//            }
//            return false;
//        }
        
//        Brush.DrawingMode brushMode = brushObject.drawingMode;
//        bool isGrabModeFallback = brushMode == Brush.DrawingMode.Grab;
        
//        if (!isGrabModeFallback && Time.frameCount % 60 == 0)
//        {
//            Debug.LogWarning($"ModelPlacer: Drawing mode is {brushMode}, not Grab. Please select Grab mode from the drawing menu.");
//        }
        
//        return isGrabModeFallback;
//    }

//    private void TryPlaceModel(XRRayInteractor rayInteractor)
//    {
//        if (modelManager == null)
//        {
//            Debug.LogError("ModelPlacer: ModelManager is null!");
//            return;
//        }

//        GameObject selectedPrefab = modelManager.GetSelectedModelPrefab();
//        if (selectedPrefab == null)
//        {
//            Debug.LogWarning("ModelPlacer: No model selected. Please select a model from the dropdown first.");
//            return;
//        }

//        if (rayInteractor == null)
//        {
//            Debug.LogWarning("ModelPlacer: Ray interactor is null!");
//            return;
//        }

//        RaycastHit hit;
//        bool hasHit = false;

//        if (rayInteractor.TryGetCurrent3DRaycastHit(out hit))
//        {
//            hasHit = true;
//        }
//        else
//        {
//            Vector3 rayOrigin = rayInteractor.transform.position;
//            Vector3 rayDirection = rayInteractor.transform.forward;
//            float maxDistance = Mathf.Min(Mathf.Max(placementDistance, 10f), 30f);
            
//            Ray ray = new Ray(rayOrigin, rayDirection);
            
//            hasHit = Physics.Raycast(ray, out hit, maxDistance, placementLayerMask);
            
//            if (!hasHit)
//            {
//                hasHit = Physics.Raycast(ray, out hit, maxDistance);
//            }
            
//            if (!hasHit)
//            {
//                hasHit = Physics.Raycast(ray, out hit, 100f);
//            }
//        }

//        if (hasHit && hit.collider != null)
//        {
//            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("UI") || 
//                hit.collider.GetComponent<UnityEngine.UI.Graphic>() != null ||
//                hit.collider.GetComponent<UnityEngine.Canvas>() != null)
//            {
//                Debug.LogWarning("ModelPlacer: Cannot place model on UI element. Point at a 3D surface instead.");
//                return;
//            }
            
//            GameObject hitObject = hit.collider.gameObject;
//            string hitName = hitObject.name.ToLower();
            
//            if (HasTeleportTag(hitObject))
//            {
//                Debug.LogWarning($"ModelPlacer: Cannot place model on teleport surface '{hitObject.name}'. Point at a regular surface instead.");
//                return;
//            }
            
//            if (hitName.Contains("teleport") && !hitName.Contains("floor") && !hitName.Contains("ground"))
//            {
//                Debug.LogWarning($"ModelPlacer: Cannot place model on teleport surface '{hitObject.name}'. Point at a regular surface instead.");
//                return;
//            }
//            if (hitName.Contains("anchor") && !hitName.Contains("floor") && !hitName.Contains("ground"))
//            {
//                Transform checkParent = hitObject.transform.parent;
//                bool isTeleportAnchor = false;
//                int checkDepth = 0;
//                while (checkParent != null && checkDepth < 3)
//                {
//                    string parentName = checkParent.name.ToLower();
//                    if (HasTeleportTag(checkParent.gameObject) ||
//                        (parentName.Contains("teleport") && !parentName.Contains("floor") && !parentName.Contains("ground")))
//                    {
//                        isTeleportAnchor = true;
//                        break;
//                    }
//                    checkParent = checkParent.parent;
//                    checkDepth++;
//                }
//                if (isTeleportAnchor)
//                {
//                    Debug.LogWarning($"ModelPlacer: Cannot place model on teleport anchor '{hitObject.name}'. Point at a regular surface instead.");
//                    return;
//                }
//            }
            
//            Transform parentCheck = hitObject.transform.parent;
//            int parentDepth = 0;
//            while (parentCheck != null && parentDepth < 5)
//            {
//                string parentName = parentCheck.name.ToLower();
//                if (HasTeleportTag(parentCheck.gameObject) &&
//                    !parentName.Contains("floor") && !parentName.Contains("ground"))
//                {
//                    Debug.LogWarning($"ModelPlacer: Cannot place model on teleport surface (parent: '{parentCheck.name}'). Point at a regular surface instead.");
//                    return;
//                }
//                if (parentName.Contains("teleport") && 
//                    !parentName.Contains("floor") && !parentName.Contains("ground"))
//                {
//                    Debug.LogWarning($"ModelPlacer: Cannot place model on teleport surface (parent: '{parentCheck.name}'). Point at a regular surface instead.");
//                    return;
//                }
//                parentCheck = parentCheck.parent;
//                parentDepth++;
//            }
            
//            if (hit.distance > 30f)
//            {
//                Debug.LogWarning($"ModelPlacer: Hit surface is too far away ({hit.distance:F1} units). Point closer to the surface. Max distance: 30 units.");
//                return;
//            }
            
//            lastPlacementTime = Time.time;
//            PlaceModel(selectedPrefab, hit.point, hit.normal);
//        }
//        else
//        {
//            Debug.LogError("ModelPlacer: No valid surface found to place model. Make sure you're pointing at a surface with a collider.");
//        }
//    }

//    private void PlaceModel(GameObject prefab, Vector3 position, Vector3 normal)
//    {
//        GameObject newModel = Instantiate(prefab, position, Quaternion.identity);
//        placedModels.Add(newModel);
//        placedModelNames[newModel] = prefab.name;
        
//        Renderer[] renderers = newModel.GetComponentsInChildren<Renderer>();
//        Renderer[] prefabRenderers = prefab.GetComponentsInChildren<Renderer>();
        
//        for (int i = 0; i < renderers.Length && i < prefabRenderers.Length; i++)
//        {
//            if (renderers[i] != null && prefabRenderers[i] != null)
//            {
//                Material[] originalMaterials = prefabRenderers[i].sharedMaterials;
//                if (originalMaterials != null && originalMaterials.Length > 0)
//                {
//                    bool hasInvalidMaterial = false;
//                    for (int j = 0; j < originalMaterials.Length; j++)
//                    {
//                        if (originalMaterials[j] == null)
//                        {
//                            Debug.LogWarning($"ModelPlacer: Material {j} is null on renderer {renderers[i].name}");
//                            hasInvalidMaterial = true;
//                        }
//                        else if (originalMaterials[j].shader == null)
//                        {
//                            Debug.LogWarning($"ModelPlacer: Material {originalMaterials[j].name} has null shader on renderer {renderers[i].name}");
//                            hasInvalidMaterial = true;
//                        }
//                    }
                    
//                    if (!hasInvalidMaterial)
//                    {
//                        renderers[i].sharedMaterials = originalMaterials;
//                    }
//                }
//                else
//                {
//                    Debug.LogWarning($"ModelPlacer: No materials found on prefab renderer {prefabRenderers[i].name}. Model may appear purple.");
//                }
//            }
//        }

//        if (placeOnSurface)
//        {
//            newModel.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, normal), normal);
//        }

//        if (placementParent != null)
//        {
//            newModel.transform.SetParent(placementParent);
//        }
//        else
//        {
//            GameObject environment = GameObject.Find("Environment");
            
//            if (environment == null)
//            {
//                try
//                {
//                    environment = GameObject.FindGameObjectWithTag("Environment");
//                }
//                catch (UnityException)
//                {
//                }
//            }
            
//            if (environment == null)
//            {
//                environment = GameObject.Find("Drawing");
                
//                if (environment == null)
//                {
//                    try
//                    {
//                        environment = GameObject.FindGameObjectWithTag("Drawing");
//                    }
//                    catch (UnityException)
//                    {
//                    }
//                }
//            }
            
//            if (environment != null)
//            {
//                newModel.transform.SetParent(environment.transform);
//            }
//            else
//            {
//                Debug.LogWarning("ModelPlacer: Could not find Environment or Drawing object. Model placed without parent.");
//            }
//        }
//    }
    
//    public void DeleteLastPlacedModel()
//    {
//        if (placedModels.Count == 0)
//        {
//            Debug.LogWarning("ModelPlacer: No models to delete.");
//            return;
//        }
        
//        GameObject lastModel = placedModels[placedModels.Count - 1];
//        if (lastModel != null)
//        {
//            placedModels.RemoveAt(placedModels.Count - 1);
//            if (placedModelNames.ContainsKey(lastModel))
//            {
//                placedModelNames.Remove(lastModel);
//            }
//            Destroy(lastModel);
//        }
//        else
//        {
//            placedModels.RemoveAt(placedModels.Count - 1);
//            Debug.LogWarning("ModelPlacer: Last model was already destroyed. Removed from list.");
//        }
//    }
    
//    public void DeleteAllPlacedModels()
//    {
//        if (placedModels.Count == 0)
//        {
//            return;
//        }
        
//        foreach (GameObject model in placedModels)
//        {
//            if (model != null)
//            {
//                Destroy(model);
//            }
//        }
        
//        placedModels.Clear();
//        placedModelNames.Clear();
//    }
    
//    public int GetPlacedModelCount()
//    {
//        placedModels.RemoveAll(model => model == null);
//        var nullKeys = placedModelNames.Keys.Where(k => k == null).ToList();
//        foreach (var key in nullKeys)
//        {
//            placedModelNames.Remove(key);
//        }
//        return placedModels.Count;
//    }

//    public List<DrawingData.PlacedModel> GetPlacedModelsData()
//    {
//        List<DrawingData.PlacedModel> modelsData = new List<DrawingData.PlacedModel>();
//        placedModels.RemoveAll(model => model == null);
//        var nullKeys = placedModelNames.Keys.Where(k => k == null).ToList();
//        foreach (var key in nullKeys)
//        {
//            placedModelNames.Remove(key);
//        }

//        foreach (GameObject model in placedModels)
//        {
//            if (model != null && placedModelNames.ContainsKey(model))
//            {
//                string modelName = placedModelNames[model];
//                Vector3 position = model.transform.position;
//                Quaternion rotation = model.transform.rotation;
//                Vector3 scale = model.transform.localScale;
//                modelsData.Add(new DrawingData.PlacedModel(modelName, position, rotation, scale));
//            }
//        }

//        return modelsData;
//    }

//    public void LoadPlacedModels(List<DrawingData.PlacedModel> modelsData)
//    {
//        if (modelsData == null || modelsData.Count == 0) return;

//        DeleteAllPlacedModels();

//        foreach (var modelData in modelsData)
//        {
//            if (modelManager == null)
//            {
//                modelManager = ModelManager.Instance;
//                if (modelManager == null)
//                {
//                    modelManager = FindObjectOfType<ModelManager>();
//                }
//            }

//            if (modelManager == null)
//            {
//                Debug.LogError("ModelPlacer: Cannot load placed models - ModelManager not found!");
//                return;
//            }

//            List<string> modelNames = modelManager.GetModelNames();
//            int modelIndex = -1;
//            for (int i = 0; i < modelNames.Count; i++)
//            {
//                if (modelNames[i] == modelData.modelName)
//                {
//                    modelIndex = i;
//                    break;
//                }
//            }

//            if (modelIndex == -1)
//            {
//                Debug.LogWarning($"ModelPlacer: Model '{modelData.modelName}' not found in ModelManager. Skipping.");
//                continue;
//            }

//            GameObject prefab = modelManager.GetModelPrefab(modelIndex);
//            if (prefab == null)
//            {
//                Debug.LogWarning($"ModelPlacer: Prefab for model '{modelData.modelName}' is null. Skipping.");
//                continue;
//            }

//            Vector3 position = modelData.GetPosition();
//            Quaternion rotation = modelData.GetRotation();
//            Vector3 scale = modelData.GetScale();

//            GameObject newModel = Instantiate(prefab, position, rotation);
//            newModel.transform.localScale = scale;
//            placedModels.Add(newModel);
//            placedModelNames[newModel] = modelData.modelName;

//            if (placementParent != null)
//            {
//                newModel.transform.SetParent(placementParent);
//            }
//            else
//            {
//                GameObject environment = GameObject.Find("Environment");
//                if (environment == null)
//                {
//                    try
//                    {
//                        environment = GameObject.FindGameObjectWithTag("Environment");
//                    }
//                    catch (UnityException) { }
//                }
//                if (environment == null)
//                {
//                    environment = GameObject.Find("Drawing");
//                    if (environment == null)
//                    {
//                        try
//                        {
//                            environment = GameObject.FindGameObjectWithTag("Drawing");
//                        }
//                        catch (UnityException) { }
//                    }
//                }
//                if (environment != null)
//                {
//                    newModel.transform.SetParent(environment.transform);
//                }
//            }
//        }
//    }

//    void OnDestroy()
//    {
//        UnsubscribeFromInputActions();
        
//        if (leftActivateActionDirect != null)
//        {
//            leftActivateActionDirect.Disable();
//        }
//    }
//}

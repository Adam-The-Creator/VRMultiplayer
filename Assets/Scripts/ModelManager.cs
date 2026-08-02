using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ModelManager : MonoBehaviour
{
    public static ModelManager Instance { get; private set; }

    [Header("Model Settings")]
    [SerializeField] private string modelsResourcePath = "Models";
    [SerializeField] private bool loadOnStart = true;

    private List<GameObject> modelPrefabs = new List<GameObject>();
    private List<string> modelNames = new List<string>();
    private bool isLoadingModels = false;
    private int selectedModelIndex = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (loadOnStart)
        {
            LoadModelsFromResources();
        }
    }

    public void LoadModelsFromResources()
    {
        if (isLoadingModels)
        {
            Debug.LogWarning("ModelManager: Already loading models, please wait...");
            return;
        }

        StartCoroutine(LoadModelsCoroutine());
    }

    private IEnumerator LoadModelsCoroutine()
    {
        isLoadingModels = true;
        modelPrefabs.Clear();
        modelNames.Clear();

        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(modelsResourcePath);

        if (loadedPrefabs == null || loadedPrefabs.Length == 0)
        {
            Debug.LogWarning($"ModelManager: No prefabs found in Resources/{modelsResourcePath}");
            isLoadingModels = false;
            yield break;
        }

        foreach (GameObject prefab in loadedPrefabs)
        {
            if (prefab != null)
            {
                modelPrefabs.Add(prefab);
                modelNames.Add(prefab.name);
            }
            yield return null;
        }

        isLoadingModels = false;

        if (modelPrefabs.Count == 0)
        {
            Debug.LogError("ModelManager: No models were successfully loaded!");
        }
    }

    public List<string> GetModelNames()
    {
        List<string> names = new List<string>();
        names.Add("None");
        names.AddRange(modelNames);
        return names;
    }

    public bool IsLoadingModels()
    {
        return isLoadingModels;
    }

    public GameObject GetModelPrefab(int index)
    {
        if (index == 0)
        {
            return null;
        }

        int actualIndex = index - 1;

        if (actualIndex < 0 || actualIndex >= modelPrefabs.Count)
        {
            Debug.LogWarning($"ModelManager: Invalid model index {index}");
            return null;
        }

        return modelPrefabs[actualIndex];
    }

    public void SetSelectedModelIndex(int index)
    {
        selectedModelIndex = index;
    }

    public int GetSelectedModelIndex()
    {
        return selectedModelIndex;
    }

    public GameObject GetSelectedModelPrefab()
    {
        return GetModelPrefab(selectedModelIndex);
    }

    public int GetModelCount()
    {
        return modelPrefabs.Count;
    }

    public void ReloadModels()
    {
        LoadModelsFromResources();
    }
}

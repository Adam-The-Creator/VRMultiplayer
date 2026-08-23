using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class DataAnalysis : MonoBehaviour
{
    public static DataAnalysis Instance { get; private set; }
    private static readonly string analyzerToolPath = Path.Combine(Application.streamingAssetsPath, "VRDrawing3DAnalyzer.exe");
    public TMP_Text debugText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (debugText == null) debugText = GameObject.FindGameObjectWithTag("DebugText").GetComponent<TMP_Text>();
    }

    public static void GenerateReportsForSessionAsync(string sessionId, Action<bool> onFinished)
    {
        Instance.StartCoroutine(Instance.RunAnalysisCoroutine(sessionId, onFinished));
    }

    [Obsolete("Report generator is obsolete")]
    private IEnumerator RunAnalysisCoroutine(string sessionId, Action<bool> onFinished)
    {
        UnityEngine.Debug.LogError($"[DataAnalysis] Report generator is obsolete");
        yield return null;
    }

}

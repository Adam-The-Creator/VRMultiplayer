using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
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

    private IEnumerator RunAnalysisCoroutine(string sessionId, Action<bool> onFinished)
    {
        bool success = true;
        List<DatabaseManager.Drawing> drawings = DatabaseManager.Instance.GetDrawingsForSession(sessionId);
        List<DatabaseManager.Drawing> inconsistentDrawings = new();
        debugText.text = "";

        if (!File.Exists(analyzerToolPath))
        {
            UnityEngine.Debug.LogError($"[DataAnalysis] The Data Analyzer Tool not found: {analyzerToolPath}");
            debugText.text += $"[DataAnalysis] The Data Analyzer Tool not found: {analyzerToolPath}\n\n";
            onFinished?.Invoke(false);
            yield break;
        }

        foreach (var drawing in drawings)
        {
            UnityEngine.Debug.Log($"[DataAnalysis] Analyzing Drawing ID: {drawing.Id} for Session ID: {sessionId}");
            debugText.text += $"[DataAnalysis] Analyzing Drawing ID: {drawing.Id} for Session ID: {sessionId}\n\n";

            if (!File.Exists(drawing.Path))
            {
                UnityEngine.Debug.LogWarning($"[DataAnalysis] The drawing not found. ID: {drawing.Id}, Path: {drawing.Path}");
                debugText.text += $"[DataAnalysis] The drawing not found. ID: {drawing.Id}, Path: {drawing.Path}\n\n";
                success = false;
                inconsistentDrawings.Add(drawing);
                continue;
            }

            var reportPath = Path.Combine(
                FileHandler.ReportsFolder,
                $"{DatabaseManager.Instance.GetSessionNameById(sessionId)}",
                drawing.Id
            );

            try
            {
                string reportDirectory = Path.GetDirectoryName(reportPath);
                if (!Directory.Exists(reportDirectory)) Directory.CreateDirectory(reportDirectory);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DataAnalysis] Failed to create report directory: {ex.Message}");
                debugText.text += $"[DataAnalysis] Failed to create report directory: {ex.Message}\n\n";
                success = false;
                inconsistentDrawings.Add(drawing);
                continue;
            }

            StringBuilder stdOutBuilder = new();
            StringBuilder stdErrBuilder = new();
            object lockObj = new();

            string safeExePath = analyzerToolPath.Replace("/", "\\");
            string safeWorkingDirectory = Path.GetDirectoryName(safeExePath).Replace("/", "\\");
            string safeJsonPath = drawing.Path.Replace("/", "\\");
            string safeOutputPath = reportPath.Replace("/", "\\");
            string safeDbPath = DatabaseManager.DatabasePath().Replace("/", "\\");

            string processArgs = $"--generate-report --json \"{safeJsonPath}\" --output \"{safeOutputPath}\" --database \"{safeDbPath}\"";
            debugText.text += processArgs + "\n\n";

            var psi = new ProcessStartInfo()
            {
                FileName = safeExePath,
                Arguments = processArgs,
                WorkingDirectory = safeWorkingDirectory,

                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lock (lockObj) { stdOutBuilder.AppendLine(e.Data); }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lock (lockObj) { stdErrBuilder.AppendLine(e.Data); }
                }
            };

            bool processStarted = false;

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                processStarted = true;
                debugText.text += $"[DataAnalysis] Process started\n\n";
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DataAnalysis] Error during executing the external tool. (Drawing ID: {drawing.Id}): {ex.Message}");
                debugText.text += $"[DataAnalysis] Error during executing the external tool. (Drawing ID: {drawing.Id}): {ex.Message}\n\n";
                success = false;
            }

            if (processStarted)
            {
                while (!process.HasExited)
                {
                    yield return null;
                }

                bool ok = process.ExitCode == 0;
                success &= ok;

                UnityEngine.Debug.Log($"[DataAnalysis] Report for drawing {drawing.Id}: Success = {ok}");
                debugText.text += $"[DataAnalysis] Report for drawing {drawing.Id}: Success = {ok}; ExitCode = {process.ExitCode}\n\n";
            }

            string stdOut = stdOutBuilder.ToString();
            string stdErr = stdErrBuilder.ToString();

            debugText.text += $"[DataAnalysis] STD_OUT:\n{stdOut}\n\n";
            debugText.text += $"[DataAnalysis] STD_ERR:\n{stdErr}\n\n";

            if (!string.IsNullOrEmpty(stdOut))
                UnityEngine.Debug.Log(stdOut);

            if (!string.IsNullOrEmpty(stdErr))
                UnityEngine.Debug.LogError(stdErr);
        }


        string logFileName = $"DataAnalysisLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string logFilePath = Path.Combine(FileHandler.ReportsFolder, logFileName);

        try
        {
            File.WriteAllText(logFilePath, debugText.text);
            UnityEngine.Debug.Log($"[DataAnalysis] Log file successfully saved to: {logFilePath}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[DataAnalysis] Failed to save log file: {ex.Message}");
            debugText.text += $"[DataAnalysis] Failed to save log file: {ex.Message}\n\n";
        }

        onFinished?.Invoke(success);
    }
}

using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// Turns the XR Device Simulator on only when no XR runtime is running (no headset).
public class XRDeviceSimulatorAutoEnable : MonoBehaviour
{
    [Tooltip("The XR Device Simulator GameObject. Leave it INACTIVE in the scene.")]
    [SerializeField] private GameObject deviceSimulator;
    [Tooltip("Turn the simulator on even when a headset is present (testing only).")]
    [SerializeField] private bool forceSimulator;

    private void Awake()
    {
        if (deviceSimulator == null)
        {
            Debug.LogError("[XR] XRDeviceSimulatorAutoEnable: no simulator assigned.");
            return;
        }

        var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        bool xrRunning = manager != null && manager.activeLoader != null;

        bool force = forceSimulator || HasArg("-simulator");
        bool forbid = HasArg("-nosimulator");
        bool useSimulator = !forbid && (force || !xrRunning);

        deviceSimulator.SetActive(useSimulator);
        Debug.Log($"[XR] loader='{(xrRunning ? manager.activeLoader.name : "none")}' " +
                  $"isDeviceActive={XRSettings.isDeviceActive} forced={force} -> simulator {(useSimulator ? "ON" : "OFF")}");
    }

    private static bool HasArg(string name)
    {
        foreach (string a in Environment.GetCommandLineArgs())
            if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
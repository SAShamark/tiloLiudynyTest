using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features.Meta;

public class EntryPoint : MonoBehaviour
{
    [SerializeField] private GameManager _gameManager;

    [Header("System")]
    [Tooltip("Headset refresh rate (Hz). Physics runs at the same rate so held items and hands move smoothly.")]
    [SerializeField] private float _refreshRate = 72f;

    private void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.RoundToInt(_refreshRate);
        Time.fixedDeltaTime = 1f / _refreshRate;
        RequestDisplayRefreshRate();
    }

    private void Start()
    {
        _gameManager.Initialize();
    }

    private void OnDestroy()
    {
        _gameManager.Dispose();
    }

    private void RequestDisplayRefreshRate()
    {
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        foreach (var display in displays)
        {
            display.TryRequestDisplayRefreshRate(_refreshRate);
        }
    }
}

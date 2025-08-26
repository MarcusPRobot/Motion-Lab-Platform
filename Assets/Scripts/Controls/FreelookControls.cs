using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;      // new Input System
using Unity.Cinemachine;            // CM3

[RequireComponent(typeof(CinemachineInputAxisController))]
public class HoldRMBToLook : MonoBehaviour
{
    CinemachineInputAxisController inputCtrl;

    // Cache per-axis original gains so we can restore them
    class AxisCache {
        public CinemachineInputAxisController.Reader reader;
        public float gain;
#if ENABLE_LEGACY_INPUT_MANAGER
        public float legacyGain;
#endif
        public bool isRadial; // we won't gate this one
    }
    readonly List<AxisCache> axes = new();

    void Awake()
    {
        inputCtrl = GetComponent<CinemachineInputAxisController>();
        BuildCache();
    }

    void OnEnable() => BuildCache();

    void BuildCache()
    {
        axes.Clear();
        if (inputCtrl == null) return;

        // Controllers are auto-discovered by CM3
        foreach (var c in inputCtrl.Controllers)
        {
            var r = c.Input as CinemachineInputAxisController.Reader;
            if (r == null) continue;

            // Heuristic: skip any axis that is labeled "Radial"
            bool isRadial = !string.IsNullOrEmpty(c.Name) &&
                            c.Name.IndexOf("radial", StringComparison.OrdinalIgnoreCase) >= 0;

            axes.Add(new AxisCache {
                reader = r,
                gain = r.Gain,
#if ENABLE_LEGACY_INPUT_MANAGER
                legacyGain = r.LegacyGain,
#endif
                isRadial = isRadial
            });
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        bool held = Mouse.current.leftButton.isPressed;   // change to rightButton if you prefer

        foreach (var a in axes)
        {
            if (a.reader == null) continue;

            // Keep Radial axis active for zoom, gate only look axes
            float g = (held || a.isRadial) ? a.gain : 0f;
            a.reader.Gain = g;
#if ENABLE_LEGACY_INPUT_MANAGER
            a.reader.LegacyGain = (held || a.isRadial) ? a.legacyGain : 0f;
#endif
        }
    }
}

[RequireComponent(typeof(CinemachineCamera))]
public class MouseWheelOrbitRadius : MonoBehaviour
{
    [SerializeField] private CinemachineOrbitalFollow orbital;
    [Header("Zoom settings")]
    [SerializeField] private float sensitivity = 0.02f; // try 0.02–0.2
    [SerializeField] private float minRadius = 2f;
    [SerializeField] private float maxRadius = 50f;
    [SerializeField] private bool requireRMB = false;  // set true if you only want zoom while RMB held

    float _pending;

    void Awake()
    {
        if (!orbital)
            orbital = GetComponent<CinemachineOrbitalFollow>() 
                   ?? GetComponentInChildren<CinemachineOrbitalFollow>(true);
    }

    void Update()
    {
        if (orbital == null || Mouse.current == null) return;
        if (requireRMB && !(Mouse.current.rightButton?.isPressed ?? false)) return;

        float scrollY = Mouse.current.scroll.ReadValue().y; // up > 0, down < 0
        if (Mathf.Abs(scrollY) < 0.001f) return;

        // Up = closer (smaller radius) → subtract
        _pending += -scrollY * sensitivity;
    }

    void LateUpdate()
    {
        if (orbital == null || Mathf.Abs(_pending) < 1e-6f) return;
        orbital.Radius = Mathf.Clamp(orbital.Radius + _pending, minRadius, maxRadius);
        _pending = 0f;
    }
}

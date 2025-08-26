using UnityEngine;
using UnityEngine.InputSystem;   // ← new Input System
using Unity.Cinemachine;        // ← Cinemachine 3.x namespace

// Put this on the same GameObject as your CinemachineCamera.
// Make sure that object also has a CinemachineInputAxisController component.
[RequireComponent(typeof(CinemachineInputAxisController))]
public class HoldRMBToLook : MonoBehaviour
{
    CinemachineInputAxisController inputCtrl;

    void Awake()
    {
        inputCtrl = GetComponent<CinemachineInputAxisController>();
    }

    void Update()
    {
        // Mobile or build-target with no mouse? bail out.
        if (Mouse.current == null) return;

        // Enable axis reading only while LMB is held
        inputCtrl.enabled = Mouse.current.leftButton.isPressed;
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

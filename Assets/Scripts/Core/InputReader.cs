using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    [Header("Input Action References")]
    [Tooltip("Assign a Vector2 action bound to Mouse Delta / Left Stick")]
    [SerializeField] private InputActionReference _aimAction;

    [Tooltip("Assign a Vector2 action bound to Mouse Scroll Wheel / Right Stick")]
    [SerializeField] private InputActionReference _zoomAction;

    [Tooltip("Assign a Button action bound to Left Click / South Button")]
    [SerializeField] private InputActionReference _submitAction;

    [Tooltip("Assign a Button action bound to Right Click / East Button")]
    [SerializeField] private InputActionReference _cancelAction;

    [Tooltip("Assign a Button action bound to Esc / Start Button")]
    [SerializeField] private InputActionReference _pauseAction;

    // Used to ignore input
    public static bool IsInputBlocked { get; set; } = false;

    // ==========================================
    // BROADCAST EVENTS (for game states and cameras)
    // ==========================================
    public static event Action<Vector2> OnAimAxisChanged;
    public static event Action<float> OnZoomAxisChanged;
    public static event Action OnSubmit;
    public static event Action OnCancel;
    public static event Action OnPause;

    private void OnEnable()
    {
        if (_submitAction != null)
        {
            _submitAction.action.Enable();
            _submitAction.action.performed += HandleSubmit;
        }

        if (_cancelAction != null)
        {
            _cancelAction.action.Enable();
            _cancelAction.action.performed += HandleCancel;
        }

        if (_pauseAction != null)
        {
            _pauseAction.action.Enable();
            _pauseAction.action.performed += HandlePause;
        }

        if (_aimAction != null) _aimAction.action.Enable();
        if (_zoomAction != null) _zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        if (_submitAction != null)
        {
            _submitAction.action.performed -= HandleSubmit;
            _submitAction.action.Disable();
        }

        if (_cancelAction != null)
        {
            _cancelAction.action.performed -= HandleCancel;
            _cancelAction.action.Disable();
        }

        if (_pauseAction != null)
        {
            _pauseAction.action.performed -= HandlePause;
            _pauseAction.action.Disable();
        }

        if (_aimAction != null) _aimAction.action.Disable();
        if (_zoomAction != null) _zoomAction.action.Disable();
    }

    private void Update()
    {
        // Abort if inputs are blocked by Pause Menu, etc.
        if (IsInputBlocked) return;

        // Always read aim input
        if (_aimAction != null && _aimAction.action.enabled)
        {
            Vector2 mouseDelta = _aimAction.action.ReadValue<Vector2>();

            // Invoke the event if any movement is detected
            if (Mathf.Abs(mouseDelta.x) > 0.01f || Mathf.Abs(mouseDelta.y) > 0.01f)
            {
                OnAimAxisChanged?.Invoke(mouseDelta);
            }
        }

        // Always read zoom input
        if (_zoomAction != null && _zoomAction.action.enabled)
        {
            float scrollInput = _zoomAction.action.ReadValue<Vector2>().y;

            // Invoke the event if any movement is detected
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                OnZoomAxisChanged?.Invoke(scrollInput);
            }
        }
    }

    // ==========================================
    // TRANSLATORS (Input System -> C# Events)
    // ==========================================

    private void HandleSubmit(InputAction.CallbackContext context)
    {
        if (IsInputBlocked) return;

        if (context.ReadValueAsButton())
        {
            OnSubmit?.Invoke();
        }
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        if (IsInputBlocked) return;

        if (context.ReadValueAsButton())
        {
            OnCancel?.Invoke();
        }
    }

    private void HandlePause(InputAction.CallbackContext context)
    {
        if (context.ReadValueAsButton())
        {
            OnPause?.Invoke();
        }
    }

    // ==========================================
    // SAFEGUARDS
    // ==========================================

    private void OnDestroy()
    {
        // Wipe static event subscriptions when the scene unloads
        OnAimAxisChanged = null;
        OnZoomAxisChanged = null;
        OnSubmit = null;
        OnCancel = null;
        OnPause = null;
    }
}
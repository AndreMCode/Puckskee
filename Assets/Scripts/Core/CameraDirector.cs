using Unity.Cinemachine;
using UnityEngine;
// using static UnityEngine.UIElements.UxmlAttributeDescription;

public class CameraDirector : MonoBehaviour
{
    [Header("Match Setup Cameras")]
    [Tooltip("Player 1's camera spawn point and orientation.")]
    [SerializeField] private CinemachineCamera _vcamSpawnP1;

    [Tooltip("Player 2's camera spawn point and orientation.")]
    [SerializeField] private CinemachineCamera _vcamSpawnP2;

    [Tooltip("Camera positioned high above, a top-down view of the full map.")]
    [SerializeField] private CinemachineCamera _vcamTopDown;

    [Header("Gameplay Cameras")]
    [Tooltip("Camera that follows the puck while it slides.")]
    [SerializeField] private CinemachineCamera _vcamFollow;
    [Tooltip("Camera that orbits the puck while aiming.")]
    [SerializeField] private CinemachineCamera _vcamAiming;

    [Header("Offset Phase Cameras")]
    [Tooltip("Camera that views the collision point in State_AimOffset")]
    [SerializeField] private CinemachineCamera _vcamOffset;

    [Tooltip("A dynamic anchor for the Offset camera.")]
    [SerializeField] private Transform _offsetProxyTarget;

    [Header("Aiming Camera Controls")]
    [SerializeField] private float _aimYawSpeed = 48f;
    [SerializeField] private float _aimPitchSpeed = 32f;
    [SerializeField] private float _aimMinPitch = 25f;
    [SerializeField] private float _aimMaxPitch = 80f;
    [SerializeField] private float _zoomSpeed = 2f;
    [SerializeField] private float _minZoomDistance = 2f;
    [SerializeField] private float _maxZoomDistance = 48f;

    [Header("Offset Camera Controls")]
    [SerializeField] private float _defaultOffsetHeight = 24f;
    [SerializeField] private float _offsetZoomSpeed = 2f;
    [SerializeField] private float _minOffsetZoom = 4f;
    [SerializeField] private float _maxOffsetZoom = 48f;

    [Header("Follow Camera Controls")]
    [SerializeField] private float _followYawSpeed = 64;
    [SerializeField] private float _followPitchSpeed = 48f;
    [SerializeField] private float _followMinPitch = 25f;
    [SerializeField] private float _followMaxPitch = 60f;
    [SerializeField] private float _followZoomSpeed = 2f;
    [SerializeField] private float _minFollowZoom = 16f;
    [SerializeField] private float _maxFollowZoom = 64f;

    private CinemachineOrbitalFollow _aimOrbitalFollow;
    private CinemachineOrbitalFollow _offsetOrbitalFollow;
    private CinemachineOrbitalFollow _followOrbitalFollow;

    private void Awake()
    {
        // Reset all camera priorities
        ResetAllPriorities();

        // Grab components
        if (_vcamAiming != null)
        {
            _aimOrbitalFollow = _vcamAiming.GetComponent<CinemachineOrbitalFollow>();
        }

        if (_vcamOffset != null)
        {
            _offsetOrbitalFollow = _vcamOffset.GetComponent<CinemachineOrbitalFollow>();
        }

        if (_vcamFollow != null)
        {
            _followOrbitalFollow = _vcamFollow.GetComponent<CinemachineOrbitalFollow>();
        }

        // Set initial camera priority
        if (_vcamSpawnP1 != null)
        {
            _vcamSpawnP1.Priority = 10;
        }
    }

    // ==========================================
    // AIMING CAMERA CONTROL
    // ==========================================

    public void FocusAiming(Transform puckTransform, Vector3 initialDirection)
    {
        ResetAllPriorities();

        if (_vcamAiming != null)
        {
            _vcamAiming.Follow = puckTransform;
            _vcamAiming.LookAt = puckTransform;
            _vcamAiming.Priority = 10;
        }

        // Calculate the world-space angle of the initial direction and snap the camera behind it
        if (_aimOrbitalFollow != null)
        {
            float yawAngle = Vector3.SignedAngle(Vector3.forward, initialDirection, Vector3.up);
            _aimOrbitalFollow.HorizontalAxis.Value = yawAngle;
        }
    }

    public void AdjustAimOrbit(float deltaX, float deltaY)
    {
        if (_aimOrbitalFollow == null) return;

        // Use mouse delta / left stick to orbit the camera around the puck
        float newYaw = _aimOrbitalFollow.HorizontalAxis.Value + (deltaX * _aimYawSpeed * SaveManager.CameraSensX * Time.deltaTime);
        float newPitch = _aimOrbitalFollow.VerticalAxis.Value - (deltaY * _aimPitchSpeed * SaveManager.CameraSensY * Time.deltaTime);

        // Clamp pitch
        newPitch = Mathf.Clamp(newPitch, _aimMinPitch, _aimMaxPitch);

        // Apply
        _aimOrbitalFollow.HorizontalAxis.Value = newYaw;
        _aimOrbitalFollow.VerticalAxis.Value = newPitch;
    }

    public void AdjustAimZoom(float scrollDelta)
    {
        if (_aimOrbitalFollow == null) return;

        // Grab component orbital radius
        float currentRadius = _aimOrbitalFollow.Radius;

        // Use scroll delta / right stick to modify radius and clamp the value
        float newRadius = Mathf.Clamp(currentRadius - (scrollDelta * _zoomSpeed * SaveManager.ZoomSens), _minZoomDistance, _maxZoomDistance);

        // Apply
        _aimOrbitalFollow.Radius = newRadius;
    }

    public Vector3 GetCurrentAimDirection()
    {
        if (_vcamAiming == null) return Vector3.forward;

        // Grab the exact direction of the Aiming camera's forward
        Vector3 camForward = _vcamAiming.transform.forward;
        camForward.y = 0; // Flatten it to the XZ plane

        if (camForward.sqrMagnitude < 0.001f) return _vcamAiming.transform.up; // Edge case safety

        return camForward.normalized;
    }

    // ==========================================
    // OFFSET CAMERA CONTROL
    // ==========================================

    public void FocusOffsetAim(Vector3 collisionPoint, Vector3 incomingDirection)
    {
        ResetAllPriorities();

        if (_vcamOffset != null && _offsetProxyTarget != null)
        {            
            // Reposition the anchor object and lock the its rotation
            _offsetProxyTarget.SetPositionAndRotation(collisionPoint, Quaternion.identity);

            // Have the camera track the anchor
            _vcamOffset.Follow = _offsetProxyTarget;
            _vcamOffset.LookAt = _offsetProxyTarget;

            if (_offsetOrbitalFollow != null)
            {
                _offsetOrbitalFollow.Radius = _defaultOffsetHeight;

                // Grab the yaw angle (y-rotation) of the incoming shot
                float yawAngle = Vector3.SignedAngle(Vector3.forward, incomingDirection, Vector3.up);

                // Snap the camera's Y-rotation to align with the shot
                _offsetOrbitalFollow.HorizontalAxis.Value = yawAngle;
            }

            // Trigger camera blend
            _vcamOffset.Priority = 10;
        }
    }

    public void AdjustOffsetZoom(float scrollDelta)
    {
        if (_offsetOrbitalFollow == null) return;

        // Grab component orbital radius
        float currentRadius = _offsetOrbitalFollow.Radius;

        // Use scroll delta / right stick to modify radius and clamp the value
        float newRadius = Mathf.Clamp(currentRadius - (scrollDelta * _offsetZoomSpeed * SaveManager.ZoomSens), _minOffsetZoom, _maxOffsetZoom);

        // Apply
        _offsetOrbitalFollow.Radius = newRadius;
    }

    // ==========================================
    // FOLLOW CAMERA CONTROL
    // ==========================================

    public void AdjustFollowOrbit(float deltaX, float deltaY)
    {
        if (_followOrbitalFollow == null) return;

        // Use mouse delta / left stick to orbit the camera around the puck
        float newYaw = _followOrbitalFollow.HorizontalAxis.Value + (deltaX * _followYawSpeed * SaveManager.CameraSensX * Time.deltaTime);
        float newPitch = _followOrbitalFollow.VerticalAxis.Value - (deltaY * _followPitchSpeed * SaveManager.CameraSensY * Time.deltaTime);

        // Clamp pitch
        newPitch = Mathf.Clamp(newPitch, _followMinPitch, _followMaxPitch);

        // Apply
        _followOrbitalFollow.HorizontalAxis.Value = newYaw;
        _followOrbitalFollow.VerticalAxis.Value = newPitch;
    }

    public void AdjustFollowZoom(float scrollDelta)
    {
        if (_followOrbitalFollow == null) return;

        // Grab component orbital radius
        float currentRadius = _followOrbitalFollow.Radius;

        // Use scroll delta / right stick to modify radius and clamp the value
        float newRadius = Mathf.Clamp(currentRadius - (scrollDelta * _followZoomSpeed * SaveManager.ZoomSens), _minFollowZoom, _maxFollowZoom);

        // Apply
        _followOrbitalFollow.Radius = newRadius;
    }

    // ==========================================
    // MATCH SETUP CINEMATIC COMMANDS
    // ==========================================

    public void CutToSpawnOrientation(int playerID)
    {
        ResetAllPriorities();

        // Elevate the priority of the active player's spawn camera
        if (playerID == 1 && _vcamSpawnP1 != null) _vcamSpawnP1.Priority = 10;
        else if (playerID == 2 && _vcamSpawnP2 != null) _vcamSpawnP2.Priority = 10;
    }

    public void BlendToTopDownView()
    {
        ResetAllPriorities();

        // Elevate the priority of the Top-Down camera.
        if (_vcamTopDown != null) _vcamTopDown.Priority = 10;
    }

    public void BlendToSpawnOrientation(int playerID)
    {
        ResetAllPriorities();

        // Elevate the priority of the active player's spawn camera
        if (playerID == 1 && _vcamSpawnP1 != null) _vcamSpawnP1.Priority = 10;
        else if (playerID == 2 && _vcamSpawnP2 != null) _vcamSpawnP2.Priority = 10;
    }

    // ==========================================
    // GAMEPLAY COMMANDS
    // ==========================================

    public void FocusFollow(Transform target)
    {
        ResetAllPriorities();

        // Assign Follow camera targets and elevate priority
        if (_vcamFollow != null)
        {
            _vcamFollow.Follow = target;
            _vcamFollow.LookAt = target;
            _vcamFollow.Priority = 10;
        }
    }

    public void ReturnToAimingCamera()
    {
        ResetAllPriorities();

        // Elevate the Aiming camera priority
        if (_vcamAiming != null)
        {
            _vcamAiming.Priority = 10;
            // Do not adjust Follow, LookAt, etc. to retain previous orientation
        }
    }

    // ==========================================
    // UTILITY
    // ==========================================

    private void ResetAllPriorities()
    {
        // Setup Cameras
        if (_vcamSpawnP1 != null) _vcamSpawnP1.Priority = 0;
        if (_vcamSpawnP2 != null) _vcamSpawnP2.Priority = 0;
        if (_vcamTopDown != null) _vcamTopDown.Priority = 0;

        // Gameplay Cameras
        if (_vcamFollow != null) _vcamFollow.Priority = 0;
        if (_vcamAiming != null) _vcamAiming.Priority = 0;
        if (_vcamOffset != null) _vcamOffset.Priority = 0;
    }
}
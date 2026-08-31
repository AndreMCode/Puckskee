using UnityEngine;

public class State_AimOffset : IGameState
{
    private GameStateManager _context;
    private PuckMovementController _activePuck;

    private float _currentSpinOffset;
    private float _spinSensitivity = 4;
    private Vector3 _hitNormal;

    public State_AimOffset(GameStateManager context) => _context = context;

    public void Enter()
    {
        Debug.Log("[State] Aim Offset Started.");

        _activePuck = _context.GetActivePuck();
        _currentSpinOffset = _context.CurrentSpinOffset; // Grab the existing offset

        // Calculate exactly where the puck is currently aiming to hit
        Vector3 startPos = _activePuck.transform.position;
        Vector3 direction = _context.CurrentLaunchDirection;
        float maxDist = _activePuck.Motor.GetMaxTravelDistance(_context.MaxLaunchForce);

        // Fetch collision point and the surface normal, or open space endpoint
        Vector3 collisionPoint = _context.TrajectoryManager.GetFirstHitPoint(startPos, direction, _activePuck.Motor.Radius, maxDist);
        _hitNormal = _context.TrajectoryManager.GetFirstHitNormal(startPos, direction, _activePuck.Motor.Radius, maxDist);

        // Instruct the Camera Director to sweep to the collision point
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.FocusOffsetAim(collisionPoint, direction);
        }

        if (_context.GameHUD != null) _context.GameHUD.ShowOffsetSlider(true);

        InputReader.OnAimAxisChanged += HandleOffsetAdjust;
        InputReader.OnZoomAxisChanged += HandleZoom;
        InputReader.OnSubmit += HandleSubmit;
        InputReader.OnCancel += HandleCancel;
    }

    public void UpdateState()
    {
        // Always update trajectory line
        if (_context.TrajectoryManager != null)
        {
            float maxDist = _activePuck.Motor.GetMaxTravelDistance(_context.MaxLaunchForce);

            _context.TrajectoryManager.ShowTrajectory(
                _activePuck.transform.position,
                _context.CurrentLaunchDirection,
                _currentSpinOffset,
                _activePuck.Motor.Radius,
                maxDist
            );
        }

        if (_context.GameHUD != null)
        {
            // Using hardcoded offset range/limit (also in GameStateManager)
            _context.GameHUD.UpdateOffsetVisual(_currentSpinOffset, 60f);
        }
    }

    public void Exit()
    {
        if (_context.GameHUD != null) _context.GameHUD.ShowOffsetSlider(false);

        InputReader.OnAimAxisChanged -= HandleOffsetAdjust;
        InputReader.OnZoomAxisChanged -= HandleZoom;
        InputReader.OnSubmit -= HandleSubmit;
        InputReader.OnCancel -= HandleCancel;
    }

    // ==========================================
    // INPUT HANDLERS
    // ==========================================

    private void HandleOffsetAdjust(Vector2 delta)
    {
        // Calculate the natural reflection without any offset applied
        Vector3 reflectDir = Vector3.Reflect(_context.CurrentLaunchDirection, _hitNormal);

        // Find the angle of this base reflection relative to the wall's normal
        float baseAngle = Vector3.SignedAngle(_hitNormal, reflectDir, Vector3.up);

        // Define the absolute maximum bounds to prevent pointing parallel/inside the wall
        float minSafeOffset = -89.9f - baseAngle;
        float maxSafeOffset = 89.9f - baseAngle;

        // Combine with the global +/- 60 degree allowance
        float finalMin = Mathf.Max(minSafeOffset, -60f);
        float finalMax = Mathf.Min(maxSafeOffset, 60f);

        // Apply mouse/left stick movement
        _currentSpinOffset -= delta.x * _spinSensitivity * SaveManager.SpinSens * Time.deltaTime;

        // Clamp
        _currentSpinOffset = Mathf.Clamp(_currentSpinOffset, finalMin, finalMax);

        _context.SetSpinOffset(_currentSpinOffset);
    }

    private void HandleZoom(float scrollDelta)
    {
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.AdjustOffsetZoom(scrollDelta);
        }
    }

    private void HandleSubmit()
    {
        _context.ChangeState(new State_PowerMinigame(_context));
    }

    private void HandleCancel()
    {
        // Reset the offset back to 0
        _currentSpinOffset = 0f;
        _context.SetSpinOffset(0f);

        // Return to previous state
        _context.ChangeState(new State_AimDirection(_context));
    }
}
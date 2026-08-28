using UnityEngine;

public class State_AimDirection : IGameState
{
    private GameStateManager _context;
    private PuckMovementController _activePuck;

    public State_AimDirection(GameStateManager context) => _context = context;

    public void Enter()
    {
        _activePuck = _context.GetActivePuck();

        // Grab the correct initial direction for this turn
        Vector3 initialDir = _context.CurrentPlayer == 1 ? _context.P1AimDirection : _context.P2AimDirection;

        // Have the camera snap behind that direction
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.FocusAiming(_activePuck.transform, initialDir);
        }

        InputReader.OnAimAxisChanged += HandleOrbitalAim;
        InputReader.OnZoomAxisChanged += HandleZoom;
        InputReader.OnSubmit += HandleSubmit;
    }

    public void UpdateState()
    {
        if (_context.TrajectoryManager != null && _context.CameraDirector != null)
        {
            // Draw the laser based on where the camera is looking
            Vector3 currentAim = _context.CameraDirector.GetCurrentAimDirection();
            float maxDist = _activePuck.GetMaxTravelDistance(_context.MaxLaunchForce);

            _context.TrajectoryManager.ShowTrajectory(
                _activePuck.transform.position,
                currentAim,
                _context.CurrentSpinOffset,
                _activePuck.Radius,
                maxDist
            );
        }
    }

    public void Exit()
    {
        InputReader.OnAimAxisChanged -= HandleOrbitalAim;
        InputReader.OnZoomAxisChanged -= HandleZoom;
        InputReader.OnSubmit -= HandleSubmit;
    }

    // ==========================================
    // ACTION HANDLERS
    // ==========================================

    private void HandleOrbitalAim(Vector2 delta)
    {
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.AdjustAimOrbit(delta.x, delta.y);
        }
    }

    private void HandleZoom(float scrollDelta)
    {
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.AdjustAimZoom(scrollDelta);
        }
    }

    private void HandleSubmit()
    {
        // Save the direction vector
        Vector3 finalAim = _context.CameraDirector.GetCurrentAimDirection();
        _context.CurrentLaunchDirection = finalAim;

        _context.ChangeState(new State_AimOffset(_context));
    }
}
using UnityEngine;

public class State_Action : IGameState
{
    private GameStateManager _context;
    private PuckMovementController _activePuck;

    public State_Action(GameStateManager context) => _context = context;

    public void Enter()
    {
        Debug.Log("[State] Action Started. Puck is in motion.");

        _activePuck = _context.GetActivePuck();

        // --- MULTIPLAYER PATH RECORDING & BOOST ---
        if (_context.IsMultiplayer)
        {
            PuckMovementController inactivePuck = _context.CurrentPlayer == 1 ? _context.PuckP2 : _context.PuckP1;

            // Only setup boosts and record new paths if we are past Turn 1
            if (_context.CurrentTurn > 1)
            {
                // Give the active puck the opponent's last recorded path to check against
                if (inactivePuck != null && _activePuck.PathManager != null)
                {
                    if (inactivePuck.TryGetComponent<PathRecorder>(out var opponentRecorder))
                    {
                        _activePuck.PathManager.SetupBoostCheck(opponentRecorder);
                    }
                }

                if (_activePuck.PathManager != null)
                {
                    _activePuck.PathManager.StartPathRecording();
                }
            }
        }

        // Calculate the final launch force vector
        Vector3 launchForce = _context.CurrentLaunchDirection * _context.CalculatedLaunchForce;

        // Launch
        _activePuck.Motor.Launch(launchForce, _context.CurrentSpinOffset);

        // Assign target for camera
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.FocusFollow(_activePuck.transform);
        }

        InputReader.OnAimAxisChanged += HandleCameraOrbit;
        InputReader.OnZoomAxisChanged += HandleCameraZoom;

        // Movement listener
        GameEvents.OnPuckStopped += HandlePuckStopped;
    }

    public void UpdateState() { }

    public void Exit()
    {
        InputReader.OnAimAxisChanged -= HandleCameraOrbit;
        InputReader.OnZoomAxisChanged -= HandleCameraZoom;
        GameEvents.OnPuckStopped -= HandlePuckStopped;
    }

    // ==========================================
    // INPUT HANDLERS
    // ==========================================

    private void HandleCameraOrbit(Vector2 delta)
    {
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.AdjustFollowOrbit(delta.x, delta.y);
        }
    }

    private void HandleCameraZoom(float scrollDelta)
    {
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.AdjustFollowZoom(scrollDelta);
        }
    }

    // ==========================================
    // PHYSICS HANDLERS
    // ==========================================

    private void HandlePuckStopped()
    {
        // Verify the entire board has settled
        if (_context.IsMultiplayer)
        {
            if (_context.PuckP1 != null && _context.PuckP1.Motor.IsMoving) return;
            if (_context.PuckP2 != null && _context.PuckP2.Motor.IsMoving) return;
        }

        Debug.Log("[State] All active pucks have come to a complete stop.");

        // Stop path recording
        if (_context.IsMultiplayer)
        {
            PuckPathManager activePath = _activePuck.GetComponent<PuckPathManager>();
            if (activePath != null)
            {
                activePath.StopPathRecording();
            }
        }

        _context.ChangeState(new State_Evaluation(_context));
    }
}
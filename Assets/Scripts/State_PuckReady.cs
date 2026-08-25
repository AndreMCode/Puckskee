using UnityEngine;
using UnityEngine.AdaptivePerformance;

public class State_PuckReady : IGameState
{
    private GameStateManager _context;
    private PuckMovementController _activePuck;

    public State_PuckReady(GameStateManager context) => _context = context;

    public void Enter()
    {
        Debug.Log("[State] Puck Ready. Awaiting final launch confirmation.");

        _activePuck = _context.GetActivePuck();

        if (_context.GameHUD != null)
        {
            // Reverse-calculate the percentage from the stored launch force
            float averagePower = _context.CalculatedLaunchForce / _context.MaxLaunchForce;

            // Display ready message
            _context.GameHUD.ShowLaunchPrompt(true, averagePower);
        }

        InputReader.OnSubmit += HandleLaunch;
    }

    public void UpdateState()
    {
        // Continue rendering the trajectory lines
        if (_context.TrajectoryManager != null)
        {
            _context.TrajectoryManager.ShowTrajectory(
                _activePuck.transform.position,
                _context.CurrentLaunchDirection,
                _context.CurrentSpinOffset,
                _activePuck.Radius
            );
        }
    }

    public void Exit()
    {
        InputReader.OnSubmit -= HandleLaunch;

        // Hide the ready message
        if (_context.GameHUD != null)
        {
            _context.GameHUD.ShowLaunchPrompt(false);
        }

        // Clear the trajectory lines
        if (_context.TrajectoryManager != null)
        {
            _context.TrajectoryManager.HideTrajectory();
        }
    }

    // ==========================================
    // INPUT HANDLERS
    // ==========================================

    private void HandleLaunch()
    {
        _context.ChangeState(new State_Action(_context));
    }
}
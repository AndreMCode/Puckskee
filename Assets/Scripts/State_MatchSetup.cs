using UnityEngine;

public class State_MatchSetup : IGameState
{
    private GameStateManager _context;

    private float _timer = 0f;
    private int _phase = 0;

    // Cinematic timings
    private readonly float _startHoldTime = 1.0f;
    private readonly float _topDownHoldTime = 3.0f;
    private readonly float _returnHoldTime = 1.0f;

    public State_MatchSetup(GameStateManager context)
    {
        _context = context;
    }

    public void Enter()
    {
        Debug.Log("[State] Match Setup & Map Reveal Started.");

        _context.ResetMatchData();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Restore visibility and hide boost paths
        if (_context.IsMultiplayer)
        {
            if (_context.PuckP1 != null)
            {
                _context.PuckP1.SetGhost(false);
                _context.PuckP1.PathManager.HidePath();
                _context.PuckP1.PathManager.SetMaxLaunchForce(_context.MaxLaunchForce);
            }
            if (_context.PuckP2 != null)
            {
                _context.PuckP2.SetGhost(false);
                _context.PuckP2.PathManager.HidePath();
                _context.PuckP2.PathManager.SetMaxLaunchForce(_context.MaxLaunchForce);
            }
        }

        // Wipe any previous data from the HUD
        if (_context.GameHUD != null)
        {
            _context.GameHUD.ResetForNewMatch();
            _context.GameHUD.SetupMatchUI(_context.IsMultiplayer);
            _context.PuckP1.PathManager.SetMaxLaunchForce(_context.MaxLaunchForce);
        }

        _timer = 0f;
        _phase = 0;

        // Snap to spawn orientation
        _context.CameraDirector.CutToSpawnOrientation(_context.CurrentPlayer);
    }

    public void UpdateState()
    {
        _timer += Time.deltaTime;

        // Phase 0: Initial hold
        if (_phase == 0 && _timer >= _startHoldTime)
        {
            _phase++;
            _timer = 0f;

            _context.CameraDirector.BlendToTopDownView();
        }
        // Phase 1: Top-down hold
        else if (_phase == 1 && _timer >= _topDownHoldTime)
        {
            _phase++;
            _timer = 0f;

            _context.CameraDirector.BlendToSpawnOrientation(_context.CurrentPlayer);
        }
        // Phase 2: Return to initial and hold
        else if (_phase == 2 && _timer >= _returnHoldTime)
        {
            _context.ChangeState(new State_TurnSetup(_context));
        }
    }

    public void Exit() { }
}
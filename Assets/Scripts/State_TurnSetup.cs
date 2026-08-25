using UnityEngine;

public class State_TurnSetup : IGameState
{
    private GameStateManager _context;

    // The distance threshold to determine if the inactive puck is blocking the active one
    private readonly float _ghostThresholdDistance = 0.5f;

    public State_TurnSetup(GameStateManager context)
    {
        _context = context;
    }

    public void Enter()
    {
        Debug.Log($"[State] Turn Setup Started for Player {_context.CurrentPlayer}");

        // Wipe any spin offset leftover from the last player's shot
        _context.SetSpinOffset(0f);

        _context.IncrementTurn();
        GameEvents.OnPlayerSwapped?.Invoke(_context.CurrentPlayer);

        // Identify who is playing and who is waiting
        PuckMovementController activePuck = _context.GetActivePuck();
        PuckMovementController inactivePuck = _context.CurrentPlayer == 1 ? _context.PuckP2 : _context.PuckP1;

        if (activePuck != null)
        {
            // Display current player's distance
            GameEvents.OnDistanceUpdated?.Invoke(activePuck.PlayerID, activePuck.TotalDistance);
        }

        // ==========================================
        // MULTIPLAYER GHOST PATH VISUALS
        // ==========================================

        if (_context.IsMultiplayer)
        {
            // Only show the opponent's path if we are past Turn 1
            if (_context.CurrentTurn > 1 && inactivePuck != null)
            {
                inactivePuck.ShowPath();
            }

            // Always hide the active player's old path
            if (activePuck != null) activePuck.HidePath();
        }

        // ==========================================
        // FIRST TURN SPAWNING LOGIC
        // ==========================================

        if (_context.CurrentPlayer == 1 && !_context.P1HasSpawned)
        {
            if (_context.P1Spawn != null) activePuck.SpawnAt(_context.P1Spawn);
            _context.P1HasSpawned = true;
        }
        else if (_context.CurrentPlayer == 2 && !_context.P2HasSpawned)
        {
            if (_context.P2Spawn != null) activePuck.SpawnAt(_context.P2Spawn);
            _context.P2HasSpawned = true;
        }

        // ==========================================
        // GHOSTING LOGIC WITH GOAL IMMUNITY
        // ==========================================

        if (inactivePuck != null && activePuck != null)
        {
            // Determine if the inactive puck has already scored and finished the game
            bool isInactivePuckScored = (_context.CurrentPlayer == 1) ? _context.P2Finished : _context.P1Finished;

            if (isInactivePuckScored)
            {
                // It scored earlier, so it must remain a ghost.
                Debug.Log("[State] Opponent puck is already scored. Maintaining ghost.");
            }
            else
            {
                // If the inactive puck is sitting exactly where the active puck needs to shoot from,
                // we turn the inactive puck into a trigger (ghost) so they don't immediately collide.
                float distance = Vector3.Distance(activePuck.transform.position, inactivePuck.transform.position);

                if (distance <= _ghostThresholdDistance)
                {
                    inactivePuck.SetGhost(true);
                    Debug.Log($"[State] Player {_context.CurrentPlayer}'s path is blocked. Proximity ghosting active!");
                }
                else
                {
                    inactivePuck.SetGhost(false);
                }
            }
        }

        _context.ChangeState(new State_AimDirection(_context));
    }

    public void UpdateState() { }

    public void Exit() { }
}
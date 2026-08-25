using UnityEngine;
using static GameStateManager;

public class State_Evaluation : IGameState
{
    private GameStateManager _context;
    private PuckMovementController _activePuck;

    public State_Evaluation(GameStateManager context) => _context = context;

    public void Enter()
    {
        Debug.Log("[State] Evaluation Started.");
        _activePuck = _context.GetActivePuck();

        if (_context.CurrentPlayer == 1)
        {
            _context.P1AimDirection = _activePuck.GetLastTravelDirection();
        }
        else
        {
            _context.P2AimDirection = _activePuck.GetLastTravelDirection();
        }

        // Check if the puck is physically inside the Goal Zone
        bool isInGoal = _context.LevelGoalZone.IsPuckInside(_activePuck.gameObject);

        if (isInGoal)
        {
            HandleGoalScored();
        }
        else
        {
            HandleMiss();
        }
    }

    public void UpdateState() { }
    public void Exit() { }

    // ==========================================
    // EVALUATION LOGIC
    // ==========================================

    private void HandleGoalScored()
    {
        GameEvents.OnGoalScored?.Invoke();

        // Mark this player as finished and save their score
        _context.MarkPlayerFinished(_context.CurrentPlayer, _activePuck.TotalDistance);
        Debug.Log($"[Evaluation] Player {_context.CurrentPlayer} SCORED!");

        // Single-player evaluation
        if (!_context.IsMultiplayer)
        {
            int finalTurns = _context.P1TurnCount;
            float finalDistance = _activePuck.TotalDistance;

            SaveManager.UpdateRecord(_context.CurrentLevelData.StageNumber, finalTurns, finalDistance);

            TriggerGameOver();
            return;
        }

        // Ghost the active puck while the opponent catches up
        _activePuck.SetGhost(true);

        // Opponent tracker
        bool opponentFinished = (_context.CurrentPlayer == 1) ? _context.P2Finished : _context.P1Finished;
        int opponentID = (_context.CurrentPlayer == 1) ? 2 : 1;

        if (opponentFinished)
        {
            TriggerGameOver();
            return;
        }

        // Multi-player evaluation
        if (_context.CurrentDifficulty == GameStateManager.GameDifficulty.Easy_Turns)
        {
            int currentTurns = (_context.CurrentPlayer == 1) ? _context.P1TurnCount : _context.P2TurnCount;
            int opponentTurns = (_context.CurrentPlayer == 1) ? _context.P2TurnCount : _context.P1TurnCount;

            if (opponentTurns >= currentTurns)
            {
                // Opponent turn count is already equal to or higher than the active player, game over
                TriggerGameOver();
            }
            else
            {
                // Opponent has turns remaining to win or tie, continue
                _context.SetActivePlayer(opponentID);
                _context.ChangeState(new State_TurnSetup(_context));
            }
        }
        else // Hard Mode
        {
            float currentDistance = _activePuck.TotalDistance;
            PuckMovementController opponentPuck = (_context.CurrentPlayer == 1) ? _context.PuckP2 : _context.PuckP1;
            float opponentDistance = opponentPuck.TotalDistance;

            if (opponentDistance > currentDistance)
            {
                // Opponent has already exceeded the active player's distance, game over
                TriggerGameOver();
            }
            else
            {
                // Opponent still has a distance budget to win or tie, continue
                _context.SetActivePlayer(opponentID);
                _context.ChangeState(new State_TurnSetup(_context));
            }
        }
    }

    private void HandleMiss()
    {
        Debug.Log($"[Evaluation] Player {_context.CurrentPlayer} missed the goal zone.");

        // Opponent tracker
        int currentTurns = (_context.CurrentPlayer == 1) ? _context.P1TurnCount : _context.P2TurnCount;
        int opponentID = (_context.CurrentPlayer == 1) ? 2 : 1;

        // Single-player case
        if (!_context.IsMultiplayer)
        {
            bool easyModeGameOver = _context.CurrentDifficulty == GameStateManager.GameDifficulty.Easy_Turns && currentTurns >= _context.CurrentLevelData.MaxTurns;
            bool hardModeGameOver = _context.CurrentDifficulty != GameStateManager.GameDifficulty.Easy_Turns && _activePuck.TotalDistance >= _context.CurrentLevelData.MaxDistance;

            if (easyModeGameOver || hardModeGameOver)
            {
                TriggerGameOver();
            }
            else
            {
                _context.ChangeState(new State_TurnSetup(_context));
            }
            return;
        }

        bool opponentFinished = (_context.CurrentPlayer == 1) ? _context.P2Finished : _context.P1Finished;

        // Multi-player case
        if (opponentFinished)
        {
            // ==========================================
            // CONSECUTIVE CATCH-UP PHASE
            // The opponent is sitting in the goal zone, active player is trying to catch up
            // ==========================================
            if (_context.CurrentDifficulty == GameStateManager.GameDifficulty.Easy_Turns)
            {
                int opponentTurns = (_context.CurrentPlayer == 1) ? _context.P2TurnCount : _context.P1TurnCount;

                if (currentTurns >= opponentTurns)
                {
                    // The active player tied or lost, game over
                    TriggerGameOver();
                }
                else
                {
                    // The active player gets another turn
                    _context.ChangeState(new State_TurnSetup(_context));
                }
            }
            else // Hard Mode
            {
                PuckMovementController opponentPuck = (_context.CurrentPlayer == 1) ? _context.PuckP2 : _context.PuckP1;
                float opponentFinalDistance = opponentPuck.TotalDistance;

                if (_activePuck.TotalDistance > opponentFinalDistance)
                {
                    // The active player lost, game over
                    TriggerGameOver();
                }
                else
                {
                    // The active player gets another turn
                    _context.ChangeState(new State_TurnSetup(_context));
                }
            }
        }
        else
        {
            // ==========================================
            // NORMAL PHASE
            // Neither player has scored yet. Normal back-and-forth swapping.
            // ==========================================

            bool easyModeFinished = _context.CurrentDifficulty == GameStateManager.GameDifficulty.Easy_Turns && currentTurns >= _context.CurrentLevelData.MaxTurns;
            bool hardModeFinished = _context.CurrentDifficulty != GameStateManager.GameDifficulty.Easy_Turns && _activePuck.TotalDistance >= _context.CurrentLevelData.MaxDistance;

            if (easyModeFinished || hardModeFinished)
            {
                _context.MarkPlayerFinished(_context.CurrentPlayer, _activePuck.TotalDistance);
            }

            // Swap players
            _context.SetActivePlayer(opponentID);
            _context.ChangeState(new State_TurnSetup(_context));
        }
    }

    private void TriggerGameOver()
    {
        _context.ChangeState(new State_GameOver(_context));
    }
}
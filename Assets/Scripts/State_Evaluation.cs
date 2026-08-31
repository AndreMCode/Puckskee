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
            _context.P1AimDirection = _activePuck.Motor.GetLastTravelDirection();
        }
        else
        {
            _context.P2AimDirection = _activePuck.Motor.GetLastTravelDirection();
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
        _context.MarkPlayerFinished(_context.CurrentPlayer, _activePuck.MatchData.TotalDistance);
        Debug.Log($"[Evaluation] Player {_context.CurrentPlayer} SCORED!");

        // Single-player evaluation
        if (!_context.IsMultiplayer)
        {
            int finalTurns = _context.P1TurnCount;
            float finalDistance = _activePuck.MatchData.TotalDistance;

            SaveManager.UpdateRecord(_context.CurrentLevelData.StageNumber, finalTurns, finalDistance);

            TriggerGameOver();
            return;
        }

        // Ghost the active puck while the opponent catches up
        _activePuck.SetGhost(true);

        // Opponent tracker
        bool opponentFinished = (_context.CurrentPlayer == 1) ? _context.P2Finished : _context.P1Finished;
        int opponentID = (_context.CurrentPlayer == 1) ? 2 : 1;
        int currentTurns = (_context.CurrentPlayer == 1) ? _context.P1TurnCount : _context.P2TurnCount;
        int opponentTurns = (_context.CurrentPlayer == 1) ? _context.P2TurnCount : _context.P1TurnCount;

        if (opponentFinished)
        {
            TriggerGameOver();
            return;
        }

        // Multi-player evaluation
        if (_context.CurrentDifficulty == GameStateManager.GameDifficulty.Easy_Turns)
        {
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
            int par = _context.CurrentLevelData.MaxTurns;
            float currentFinalScore = CalculateEffectiveScore(_activePuck.MatchData.TotalDistance, currentTurns, par);

            PuckMovementController opponentPuck = (_context.CurrentPlayer == 1) ? _context.PuckP2 : _context.PuckP1;
            float opponentCurrentScore = CalculateEffectiveScore(opponentPuck.MatchData.TotalDistance, opponentTurns, par);

            if (opponentCurrentScore > currentFinalScore)
            {
                // Opponent's current effective score is already worse than the active player's final score
                TriggerGameOver();
            }
            else
            {
                // Opponent still has a score budget to win or tie, continue
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
        int opponentTurns = (_context.CurrentPlayer == 1) ? _context.P2TurnCount : _context.P1TurnCount;
        int opponentID = (_context.CurrentPlayer == 1) ? 2 : 1;
        int par = _context.CurrentLevelData.MaxTurns;

        // Single-player case
        if (!_context.IsMultiplayer)
        {
            bool easyModeGameOver = _context.CurrentDifficulty == GameStateManager.GameDifficulty.Easy_Turns && currentTurns >= _context.CurrentLevelData.MaxTurns;

            float currentScore = CalculateEffectiveScore(_activePuck.MatchData.TotalDistance, currentTurns, par);
            bool hardModeGameOver = _context.CurrentDifficulty != GameStateManager.GameDifficulty.Easy_Turns && currentScore >= _context.CurrentLevelData.MaxDistance;

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
                float currentScore = CalculateEffectiveScore(_activePuck.MatchData.TotalDistance, currentTurns, par);
                float opponentFinalScore = CalculateEffectiveScore(opponentPuck.MatchData.TotalDistance, opponentTurns, par);

                if (currentScore > opponentFinalScore)
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

            float currentScore = CalculateEffectiveScore(_activePuck.MatchData.TotalDistance, currentTurns, par);
            bool hardModeFinished = _context.CurrentDifficulty != GameStateManager.GameDifficulty.Easy_Turns && currentScore >= _context.CurrentLevelData.MaxDistance;

            if (easyModeFinished || hardModeFinished)
            {
                _context.MarkPlayerFinished(_context.CurrentPlayer, _activePuck.MatchData.TotalDistance);
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

    // Par calculator
    private float CalculateEffectiveScore(float distance, int turns, int par)
    {
        float currentTurnWeight = _context.CurrentLevelData.TurnWeight;

        float effectiveScore = distance + ((turns - par) * currentTurnWeight);
        return Mathf.Max(0f, effectiveScore);
    }
}
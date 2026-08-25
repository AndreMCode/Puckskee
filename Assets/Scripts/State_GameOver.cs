using UnityEngine;
using static GameStateManager;

public class State_GameOver : IGameState
{
    private GameStateManager _context;

    private float _timer = 0f;
    private readonly float _resultDelay = 1.5f; // Hardcoded value for now
    private bool _resultsDisplayed = false;

    private string _finalResultMessage;

    public State_GameOver(GameStateManager context) => _context = context;

    public void Enter()
    {
        Debug.Log("[State] Game Over. Waiting to display results...");

        _timer = 0f;
        _resultsDisplayed = false;

        // Calculate the results
        _finalResultMessage = DetermineMatchResults();
    }

    public void UpdateState()
    {
        // Popup delay timer
        if (!_resultsDisplayed)
        {
            _timer += Time.deltaTime;
            if (_timer >= _resultDelay)
            {
                _resultsDisplayed = true;
                Debug.Log("[State] Displaying Match Results.");

                // Broadcast result to the UI
                GameEvents.OnGameOver?.Invoke(_finalResultMessage);
            }
        }
    }

    public void Exit() { }

    private string DetermineMatchResults()
    {
        if (!_context.IsMultiplayer)
        {
            // Single-player
            if (_context.CurrentDifficulty == GameDifficulty.Easy_Turns)
            {
                if (!_context.P1Finished) return "Out of Turns! You Lose!";
                return $"     Game!\nRating: {GetMedalTurns(_context.CurrentTurn)}";
            }
            else // Hard Mode
            {
                if (!_context.P1Finished) return "Exceeded Distance! You Lose!";
                return $"     Game!\nRating: {GetMedalDistance(_context.P1FinalDistance)}";
            }
        }
        else
        {
            // Multi-player
            if (_context.CurrentDifficulty == GameDifficulty.Easy_Turns)
            {
                if (_context.P1Finished && _context.P2Finished) return "It's a Tie!";
                if (_context.P1Finished) return "Player 1 Wins! (Fewer Turns)";
                if (_context.P2Finished) return "Player 2 Wins! (Fewer Turns)";
                return "Nobody reached the goal!";
            }
            else // Hard Mode (lowest distance)
            {
                if (!_context.P1Finished && !_context.P2Finished) return "Nobody scored. Draw!";
                if (_context.P1Finished && !_context.P2Finished) return "Player 1 Wins! (Lower Distance)";
                if (!_context.P1Finished && _context.P2Finished) return "Player 2 Wins! (Lower Distance)";

                // Both finished, compare distances
                if (_context.P1FinalDistance < _context.P2FinalDistance) return "Player 1 Wins (Lower Distance)!";
                if (_context.P2FinalDistance < _context.P1FinalDistance) return "Player 2 Wins (Lower Distance)!";

                return "Incredible! A Perfect Tie!";
            }
        }
    }

    private string GetMedalTurns(int turns)
    {
        if (turns <= _context.CurrentLevelData.GoldTurns) return "Gold";
        if (turns <= _context.CurrentLevelData.SilverTurns) return "Silver";
        if (turns <= _context.CurrentLevelData.BronzeTurns) return "Bronze";
        return "None";
    }

    private string GetMedalDistance(float distance)
    {
        if (distance <= _context.CurrentLevelData.GoldDistance) return "Gold";
        if (distance <= _context.CurrentLevelData.SilverDistance) return "Silver";
        if (distance <= _context.CurrentLevelData.BronzeDistance) return "Bronze";
        return "None";
    }
}
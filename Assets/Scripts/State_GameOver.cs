using UnityEngine;
using static GameStateManager;

public class State_GameOver : IGameState
{
    private GameStateManager _context;

    private float _timer = 0f;
    private readonly float _resultDelay = 1.5f; // Hardcoded value for now
    private bool _resultsDisplayed = false;

    private string _finalResultMessage;
    private string _mathBreakdownMessage;

    public State_GameOver(GameStateManager context) => _context = context;

    public void Enter()
    {
        Debug.Log("[State] Game Over. Waiting to display results...");

        _timer = 0f;
        _resultsDisplayed = false;

        _mathBreakdownMessage = "";

        // Calculate the results
        DetermineMatchResults();
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
                GameEvents.OnGameOver?.Invoke(_finalResultMessage, _mathBreakdownMessage);
            }
        }
    }

    public void Exit() { }

    private void DetermineMatchResults()
    {
        int par = _context.CurrentLevelData.MaxTurns;

        if (!_context.IsMultiplayer)
        {
            // Single-player
            if (_context.CurrentDifficulty == GameDifficulty.Easy_Turns)
            {
                if (!_context.P1Finished) 
                {
                    _finalResultMessage = "Out of Turns! You Lose!";
                    return;
                }
                _finalResultMessage = $"     Game!\nRating: {GetMedalTurns(_context.P1TurnCount)}";
            }
            else // Hard Mode
            {
                if (!_context.P1Finished) 
                {
                    _finalResultMessage = "Exceeded Distance! You Lose!";
                    return;
                }

                float effectiveScore = CalculateEffectiveScore(_context.P1FinalDistance, _context.P1TurnCount, par, out _mathBreakdownMessage);
                _finalResultMessage = $"     Game!\nRating: {GetMedalDistance(effectiveScore)}";
            }
        }
        else
        {
            // Multi-player
            if (_context.CurrentDifficulty == GameDifficulty.Easy_Turns)
            {
                if (_context.P1Finished && _context.P2Finished) _finalResultMessage = "It's a Tie!";
                else if (_context.P1Finished) _finalResultMessage = "Player 1 Wins! (Fewer Turns)";
                else if (_context.P2Finished) _finalResultMessage = "Player 2 Wins! (Fewer Turns)";
                else _finalResultMessage = "Nobody reached the goal!";
            }
            else // Hard Mode (lowest effective score)
            {
                float p1Distance = _context.P1Finished ? _context.P1FinalDistance : _context.PuckP1.TotalDistance;
                float p2Distance = _context.P2Finished ? _context.P2FinalDistance : _context.PuckP2.TotalDistance;

                float p1Score = CalculateEffectiveScore(p1Distance, _context.P1TurnCount, par, out string p1Breakdown);
                float p2Score = CalculateEffectiveScore(p2Distance, _context.P2TurnCount, par, out string p2Breakdown);

                if (!_context.P1Finished && !_context.P2Finished) 
                {
                    _mathBreakdownMessage = $"P1: {p1Breakdown} (Failed)\nP2: {p2Breakdown} (Failed)";
                    _finalResultMessage = "Nobody scored. Draw!";
                    return;
                }
                
                if (_context.P1Finished && !_context.P2Finished) 
                {
                    _mathBreakdownMessage = $"P1: {p1Breakdown}\nP2: {p2Breakdown} (Failed)";
                    _finalResultMessage = "Player 1 Wins! (Opponent Failed)";
                    return;
                }
                
                if (!_context.P1Finished && _context.P2Finished) 
                {
                    _mathBreakdownMessage = $"P1: {p1Breakdown} (Failed)\nP2: {p2Breakdown}";
                    _finalResultMessage = "Player 2 Wins! (Opponent Failed)";
                    return;
                }

                // Both finished, calculate and compare effective scores
                _mathBreakdownMessage = $"P1: {p1Breakdown}\nP2: {p2Breakdown}";

                if (p1Score < p2Score) _finalResultMessage = "Player 1 Wins (Better Score)!";
                else if (p2Score < p1Score) _finalResultMessage = "Player 2 Wins (Better Score)!";
                else _finalResultMessage = "Incredible! A Perfect Tie!";
            }
        }
    }

    private float CalculateEffectiveScore(float distance, int turns, int par, out string breakdown)
    {
        float currentTurnWeight = _context.CurrentLevelData.TurnWeight;
        float effectiveScore = distance + ((turns - par) * currentTurnWeight);

        // Ensure score doesn't drop below 0 if player finishes wildly under par with a tiny distance
        effectiveScore = Mathf.Max(0f, effectiveScore);

        breakdown = $"{distance:F1}m + (({turns} - {par}) * {currentTurnWeight}) = {effectiveScore:F1}m";
        return effectiveScore;
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
using UnityEngine;

public class State_PowerMinigame : IGameState
{
    public enum PowerPhase { Pendulum, Fill, Orbit, Complete }

    private GameStateManager _context;

    private PowerPhase _currentPhase;
    private float _timer;
    private float _reviewTimer;

    // Stored Power Values
    private float _pendulumPower;
    private float _fillPower;
    private float _orbitPower;

    // Tuning speeds
    private float _pendulumSpeed = 2f;
    private float _fillSpeed = 1.5f;
    private float _orbitSpeed = 1f;

    // For the UI
    private float _currentUIValue;

    public State_PowerMinigame(GameStateManager context) => _context = context;

    public void Enter()
    {
        Debug.Log("[State] Megaton Power Sequence Started.");

        // Return the camera to the aiming perspective
        if (_context.CameraDirector != null)
        {
            _context.CameraDirector.ReturnToAimingCamera();
        }

        // Initialize phase 1
        _currentPhase = PowerPhase.Pendulum;
        _timer = 0.5f; // Starts pendulum needle in the center

        _pendulumPower = 0f;
        _fillPower = 0f;
        _orbitPower = 0f;

        // Initialize the minigame
        if (_context.GameHUD != null)
        {
            _context.GameHUD.StartMegatonSequence();
        }

        InputReader.OnSubmit += HandleSubmit;
        InputReader.OnCancel += HandleCancel;
    }

    public void UpdateState()
    {
        // Intercept the update loop if we are in the review phase
        if (_currentPhase == PowerPhase.Complete)
        {
            _reviewTimer -= Time.deltaTime;
            if (_reviewTimer <= 0f)
            {
                _context.ChangeState(new State_PuckReady(_context));
            }
            return; // Skip the rest of the method so meters stay frozen
        }

        _timer += Time.deltaTime;

        // Calculate the live minigame values
        switch (_currentPhase)
        {
            case PowerPhase.Pendulum:
                float sin = Mathf.Sin(_timer * _pendulumSpeed * Mathf.PI);
                _currentUIValue = (sin + 1f) / 2f; // Maps -1 to 1 into 0.0 to 1.0
                break;

            case PowerPhase.Fill:
                _currentUIValue = Mathf.PingPong(_timer * _fillSpeed, 1.0f);
                break;

            case PowerPhase.Orbit:
                _currentUIValue = (_timer * _orbitSpeed) % 1f;
                break;
        }

        // Update the HUD
        if (_context.GameHUD != null)
        {
            _context.GameHUD.UpdateMegatonMeter(_currentPhase, _currentUIValue);
        }
    }

    public void Exit()
    {
        if (_context.GameHUD != null)
        {
            _context.GameHUD.HideMegatonSequence();
        }

        InputReader.OnSubmit -= HandleSubmit;
        InputReader.OnCancel -= HandleCancel;
    }

    // ==========================================
    // INPUT HANDLERS
    // ==========================================

    private void HandleSubmit()
    {
        switch (_currentPhase)
        {
            case PowerPhase.Pendulum:
                _pendulumPower = CalculateAccuracy(PowerPhase.Pendulum, _currentUIValue);
                Debug.Log($"[Megaton] Pendulum Locked: {_pendulumPower:F2}");

                // Push the score to the UI
                if (_context.GameHUD != null)
                    _context.GameHUD.ShowPhaseScore(PowerPhase.Pendulum, _pendulumPower);

                // Move to Fill
                _currentPhase = PowerPhase.Fill;
                _timer = 0f;
                if (_context.GameHUD != null) _context.GameHUD.AdvanceMegatonPhase(PowerPhase.Fill);
                break;

            case PowerPhase.Fill:
                _fillPower = CalculateAccuracy(PowerPhase.Fill, _currentUIValue);
                Debug.Log($"[Megaton] Fill Locked: {_fillPower:F2}");

                // Push the score to the UI
                if (_context.GameHUD != null)
                    _context.GameHUD.ShowPhaseScore(PowerPhase.Fill, _fillPower);

                // Move to Orbit
                _currentPhase = PowerPhase.Orbit;
                _timer = 0f;
                if (_context.GameHUD != null) _context.GameHUD.AdvanceMegatonPhase(PowerPhase.Orbit);
                break;

            case PowerPhase.Orbit:
                _orbitPower = CalculateAccuracy(PowerPhase.Orbit, _currentUIValue);
                Debug.Log($"[Megaton] Orbit Locked: {_orbitPower:F2}");

                // Push the score to the UI
                if (_context.GameHUD != null)
                    _context.GameHUD.ShowPhaseScore(PowerPhase.Orbit, _orbitPower);

                // Finalize and calculate the average of all 3 phases
                float averagePower = (_pendulumPower + _fillPower + _orbitPower) / 3f;
                _context.SetCalculatedLaunchForce(averagePower * _context.MaxLaunchForce);

                // Lock into the review phase and start the countdown
                _currentPhase = PowerPhase.Complete;
                _reviewTimer = 1.5f; // Hardcoded for now
                break;
        }
    }

    private void HandleCancel()
    {
        // Only allow cancel during Phase 1
        if (_currentPhase == PowerPhase.Pendulum)
        {
            Debug.Log("[Megaton] Cancelled. Returning to Offset Adjust.");

            if (_context.GameHUD != null)
            {
                _context.GameHUD.HideMegatonSequence();
            }

            _context.ChangeState(new State_AimOffset(_context));
        }
        else
        {
            Debug.Log("[Megaton] Cannot cancel! Sequence is already locked in.");
        }
    }

    // ==========================================
    // MATH LOGIC
    // ==========================================

    private float CalculateAccuracy(PowerPhase phase, float rawUIValue)
    {
        // Translates the raw UI visual position into an actual power/accuracy percentage (0.0 to 1.0)
        if (phase == PowerPhase.Pendulum)
        {
            // For Pendulum, 0.5 (the center) is a perfect 1.0 score. 0.0 and 1.0 are 0 score.
            float distanceFromCenter = Mathf.Abs(rawUIValue - 0.5f);
            return 1f - (distanceFromCenter * 2f);
        }
        else if (phase == PowerPhase.Orbit)
        {
            // For Orbit, 0.0 (or 1.0) is the perfect score (the intersection point).
            float dist = Mathf.Abs(rawUIValue - 0f);
            if (dist > 0.5f) dist = 1f - dist;
            return 1f - (dist * 2f);
        }

        // For Fill, the visual value perfectly matches the power value
        return rawUIValue;
    }
}
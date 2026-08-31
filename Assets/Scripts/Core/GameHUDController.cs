using UnityEngine;
using UnityEngine.UIElements;
using Puckskee.UI;

[RequireComponent(typeof(UIDocument))]
public class GameHUDController : MonoBehaviour
{
    private UIDocument _uiDocument;

    // Player 1 UI
    private VisualElement _p1Container;
    private Label _p1TurnLabel;
    private Label _p1DistanceLabel;
    private Slider _p1MassSlider;
    private Label _p1MassLabel;
    private Slider _p1FrictionSlider;
    private Label _p1FrictionLabel;

    // Player 2 UI
    private VisualElement _p2Container;
    private Label _p2TurnLabel;
    private Label _p2DistanceLabel;
    private Slider _p2MassSlider;
    private Label _p2MassLabel;
    private Slider _p2FrictionSlider;
    private Label _p2FrictionLabel;

    // General UI & State
    private Label _playerLabel;
    private int _activePlayerID = 1;

    // Offset UI
    private VisualElement _offsetContainer;
    private VisualElement _offsetDot;

    // Minigame UI
    private VisualElement _megatonContainer;
    private PendulumMeter _pendulumMeter;
    private FillMeter _fillMeter;
    private OrbitLoop _orbitLoop;
    private Label _strikePrompt;

    // Minigame UI labels
    private Label _pendulumScore;
    private Label _fillScore;
    private Label _orbitScore;

    // Game Over UI
    private VisualElement _medalContainer;
    private Label _medalLabel;
    private Label _mathBreakdownLabel;
    private bool _isGameOverActive = false;

    // Throttling distance allocations
    private float _lastP1Distance = -1f;
    private float _lastP2Distance = -1f;
    private readonly float _distanceUpdateThreshold = 0.05f; // Only update if distance changes by 5cm

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        // Player 1 queries
        _p1Container = root.Q<VisualElement>("p1-stats-container");
        _p1TurnLabel = root.Q<Label>("p1-turns-value");
        _p1DistanceLabel = root.Q<Label>("p1-distance-value");
        _p1MassSlider = root.Q<Slider>("p1-mass-slider");
        _p1MassLabel = root.Q<Label>("p1-mass-value");
        _p1FrictionSlider = root.Q<Slider>("p1-friction-slider");
        _p1FrictionLabel = root.Q<Label>("p1-friction-value");

        // Player 2 queries
        _p2Container = root.Q<VisualElement>("p2-stats-container");
        _p2TurnLabel = root.Q<Label>("p2-turns-value");
        _p2DistanceLabel = root.Q<Label>("p2-distance-value");
        _p2MassSlider = root.Q<Slider>("p2-mass-slider");
        _p2MassLabel = root.Q<Label>("p2-mass-value");
        _p2FrictionSlider = root.Q<Slider>("p2-friction-slider");
        _p2FrictionLabel = root.Q<Label>("p2-friction-value");

        _playerLabel = root.Q<Label>("player-label");

        // Offset Slider queries
        _offsetContainer = root.Q<VisualElement>("offset-slider-container");
        _offsetDot = root.Q<VisualElement>("offset-dot");

        // Minigame HUD queries
        _megatonContainer = root.Q<VisualElement>("meter-container");
        _pendulumMeter = root.Q<PendulumMeter>("pendulum");
        _fillMeter = root.Q<FillMeter>("fill");
        _orbitLoop = root.Q<OrbitLoop>("orbit");
        _strikePrompt = root.Q<Label>("strike-prompt");

        // UI score feedback
        _pendulumScore = root.Q<Label>("pendulum-score");
        _fillScore = root.Q<Label>("fill-score");
        _orbitScore = root.Q<Label>("orbit-score");

        // Game Over queries
        _medalContainer = root.Q<VisualElement>("medal-container");
        _medalLabel = root.Q<Label>("medal-label");
        _mathBreakdownLabel = root.Q<Label>("math-breakdown-label");

        // Hide elements on awake
        HideMegatonSequence();
        ShowOffsetSlider(false);
        ShowPlayerBanner(false);
        if (_medalContainer != null) _medalContainer.style.display = DisplayStyle.None;
    }

    private void OnEnable()
    {
        GameEvents.OnDistanceUpdated += HandleDistanceUpdated;
        GameEvents.OnMassUpdated += HandleMassUpdated;
        GameEvents.OnFrictionUpdated += HandleFrictionUpdated;
        GameEvents.OnPlayerSwapped += HandlePlayerSwapped;
        GameEvents.OnTurnChanged += HandleTurnChanged;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnPauseToggled += HandlePauseToggled;
    }

    private void OnDisable()
    {
        GameEvents.OnDistanceUpdated -= HandleDistanceUpdated;
        GameEvents.OnMassUpdated -= HandleMassUpdated;
        GameEvents.OnFrictionUpdated -= HandleFrictionUpdated;
        GameEvents.OnPlayerSwapped -= HandlePlayerSwapped;
        GameEvents.OnTurnChanged -= HandleTurnChanged;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnPauseToggled -= HandlePauseToggled;
    }

    // ==========================================
    // PASSIVE EVENT HANDLERS
    // ==========================================

    private void HandleMassUpdated(int playerID, float newMass)
    {
        if (playerID == 1)
            UpdateStatSlider(_p1MassSlider, _p1MassLabel, newMass);
        else if (playerID == 2)
            UpdateStatSlider(_p2MassSlider, _p2MassLabel, newMass);
    }

    private void HandleFrictionUpdated(int playerID, float newFriction)
    {
        if (playerID == 1)
            UpdateStatSlider(_p1FrictionSlider, _p1FrictionLabel, newFriction);
        else if (playerID == 2)
            UpdateStatSlider(_p2FrictionSlider, _p2FrictionLabel, newFriction);
    }

    private void UpdateStatSlider(Slider slider, Label label, float value)
    {
        if (slider == null || label == null) return;

        slider.value = value;
        label.text = value.ToString("F1");

        // Map value between lowValue and highValue to position label over the slider position
        float normalizedValue = Mathf.InverseLerp(slider.lowValue, slider.highValue, value);

        // Changed from Left to Translate to prevent CPU layout thrashing
        label.style.translate = new Translate(new Length(normalizedValue * 100f, LengthUnit.Percent), 0);
    }

    private void HandlePlayerSwapped(int playerID)
    {
        _activePlayerID = playerID;

        if (_playerLabel != null)
        {
            _playerLabel.text = $"PLAYER {playerID}";
            ShowPlayerBanner(true);
        }
    }

    private void HandleDistanceUpdated(int playerID, float newDistance)
    {
        // Throttle distance updates to the HUD (optimization)
        if (playerID == 1 && _p1DistanceLabel != null)
        {
            if (Mathf.Abs(newDistance - _lastP1Distance) >= _distanceUpdateThreshold)
            {
                _p1DistanceLabel.text = $"{newDistance:F2}m";
                _lastP1Distance = newDistance;
            }
        }
        else if (playerID == 2 && _p2DistanceLabel != null)
        {
            if (Mathf.Abs(newDistance - _lastP2Distance) >= _distanceUpdateThreshold)
            {
                _p2DistanceLabel.text = $"{newDistance:F2}m";
                _lastP2Distance = newDistance;
            }
        }
    }

    private void HandleTurnChanged(int currentTurn)
    {
        if (_activePlayerID == 1 && _p1TurnLabel != null)
            _p1TurnLabel.text = currentTurn.ToString();
        else if (_activePlayerID == 2 && _p2TurnLabel != null)
            _p2TurnLabel.text = currentTurn.ToString();
    }

    private void HandlePauseToggled(bool isPaused)
    {
        if (_medalContainer == null) return;

        if (isPaused)
        {
            // Always hide the medal container so the pause menu is
            // unobstructed if opened after a game has concluded
            _medalContainer.style.display = DisplayStyle.None;
        }
        else
        {
            // Display the medal container if we unpause after a game has concluded
            if (_isGameOverActive)
            {
                _medalContainer.style.display = DisplayStyle.Flex;
            }
        }
    }

    private void HandleGameOver(string resultMessage, string mathBreakdown = "")
    {
        if (_medalContainer != null && _medalLabel != null)
        {
            _isGameOverActive = true;
            _medalLabel.text = resultMessage;

            if (_mathBreakdownLabel != null)
            {
                _mathBreakdownLabel.text = mathBreakdown;
                _mathBreakdownLabel.style.display = string.IsNullOrEmpty(mathBreakdown) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            _medalContainer.style.display = DisplayStyle.Flex;
        }
    }

    public void ShowPlayerBanner(bool show)
    {
        if (_playerLabel != null) _playerLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ==========================================
    // OFFSET SLIDER LOGIC
    // ==========================================

    public void ShowOffsetSlider(bool show)
    {
        if (_offsetContainer != null)
        {
            _offsetContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void UpdateOffsetVisual(float currentOffset, float maxAllowedOffset)
    {
        if (_offsetDot == null) return;

        // Map the aim offset limits directly to a 0% to 100% position on the slider track
        float normalizedValue = Mathf.InverseLerp(maxAllowedOffset, -maxAllowedOffset, currentOffset);

        // Changed from Left to Translate to prevent CPU layout thrashing
        _offsetDot.style.translate = new Translate(new Length(normalizedValue * 100f, LengthUnit.Percent), 0);
    }

    // ==========================================
    // MINIGAME LOGIC
    // ==========================================

    public void StartMegatonSequence()
    {
        if (_megatonContainer != null) _megatonContainer.style.display = DisplayStyle.Flex;
        if (_strikePrompt != null) _strikePrompt.style.display = DisplayStyle.None;

        if (_pendulumScore != null) _pendulumScore.text = "";
        if (_fillScore != null) _fillScore.text = "";
        if (_orbitScore != null) _orbitScore.text = "";

        if (_pendulumMeter != null) _pendulumMeter.Value = 0.5f;
        if (_fillMeter != null) _fillMeter.Value = 0f;
        if (_orbitLoop != null) _orbitLoop.Progress = 0f;
    }

    public void UpdateMegatonMeter(State_PowerMinigame.PowerPhase phase, float value)
    {
        switch (phase)
        {
            case State_PowerMinigame.PowerPhase.Pendulum:
                if (_pendulumMeter != null) _pendulumMeter.Value = value;
                break;
            case State_PowerMinigame.PowerPhase.Fill:
                if (_fillMeter != null) _fillMeter.Value = value;
                break;
            case State_PowerMinigame.PowerPhase.Orbit:
                if (_orbitLoop != null) _orbitLoop.Progress = value;
                break;
        }
    }

    public void AdvanceMegatonPhase(State_PowerMinigame.PowerPhase newPhase)
    {
        // Future visuals (etc.) dependent on phase
    }

    public void ShowPhaseScore(State_PowerMinigame.PowerPhase phase, float accuracyPercentage)
    {
        int displayPercent = Mathf.RoundToInt(accuracyPercentage * 100f);

        switch (phase)
        {
            case State_PowerMinigame.PowerPhase.Pendulum:
                if (_pendulumScore != null) _pendulumScore.text = $"{displayPercent}%";
                break;
            case State_PowerMinigame.PowerPhase.Fill:
                if (_fillScore != null) _fillScore.text = $"{displayPercent}%";
                break;
            case State_PowerMinigame.PowerPhase.Orbit:
                if (_orbitScore != null) _orbitScore.text = $"{displayPercent}%";
                break;
        }
    }

    public void HideMegatonSequence()
    {
        if (_megatonContainer != null) _megatonContainer.style.display = DisplayStyle.None;
        if (_strikePrompt != null) _strikePrompt.style.display = DisplayStyle.None;
    }

    public void ShowLaunchPrompt(bool show, float powerPercentage = -1f)
    {
        if (_strikePrompt != null)
        {
            if (show)
            {
                if (powerPercentage >= 0f)
                {
                    int displayPercent = Mathf.RoundToInt(powerPercentage * 100f);
                    _strikePrompt.text = $"{displayPercent}% POWER. FIRE WHEN READY!";
                }
                else
                {
                    // Fallback
                    _strikePrompt.text = "FIRE WHEN READY";
                }
            }

            _strikePrompt.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // ==========================================
    // MATCH RESET LOGIC
    // ==========================================

    public void SetupMatchUI(bool isMultiplayer)
    {
        if (_p2Container != null)
        {
            _p2Container.style.display = isMultiplayer ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void ResetForNewMatch()
    {
        _activePlayerID = 1; // Default to P1
        _isGameOverActive = false;

        if (_medalContainer != null) _medalContainer.style.display = DisplayStyle.None;

        HideMegatonSequence();
        ShowOffsetSlider(false);
        ShowPlayerBanner(false);
        ShowLaunchPrompt(false);

        // Zero out P1
        if (_p1DistanceLabel != null) _p1DistanceLabel.text = "0.00m";
        if (_p1TurnLabel != null) _p1TurnLabel.text = "0";
        UpdateStatSlider(_p1MassSlider, _p1MassLabel, 1.8f);
        UpdateStatSlider(_p1FrictionSlider, _p1FrictionLabel, 1.8f);

        // Zero out P2
        if (_p2DistanceLabel != null) _p2DistanceLabel.text = "0.00m";
        if (_p2TurnLabel != null) _p2TurnLabel.text = "0";
        UpdateStatSlider(_p2MassSlider, _p2MassLabel, 1.8f);
        UpdateStatSlider(_p2FrictionSlider, _p2FrictionLabel, 1.8f);
    }
}
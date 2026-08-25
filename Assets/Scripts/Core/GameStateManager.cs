using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    [Header("Puck & Spawn References")]
    [SerializeField] private PuckMovementController _puckP1;
    [SerializeField] private PuckMovementController _puckP2;
    [SerializeField] private Transform _p1Spawn;
    [SerializeField] private Transform _p2Spawn;

    [Header("Level Environment References")]
    [SerializeField] private GoalZone _goalZone;
    [SerializeField] private CameraDirector _cameraDirector;
    [SerializeField] private GameHUDController _gameHUD;

    [Header("Level Settings")]
    [SerializeField] private float _maxLaunchForce = 50f;

    [Header("Systems")]
    [SerializeField] private AimTrajectoryManager _trajectoryManager;

    [Header("Level Data")]
    [Tooltip("LevelConfig ScriptableObject for this specific map.")]
    public LevelConfig CurrentLevelData;

    // For testing, later set by MainMenu
    [Header("Game Mode Settings")]
    public GameDifficulty CurrentDifficulty = GameDifficulty.Easy_Turns;
    public bool IsMultiplayer = true;
    // ---- ---- ---- ----

    public enum GameDifficulty { Easy_Turns, Hard_Distance }

    // Public Context Properties (Accessible by active states)
    public PuckMovementController PuckP1 => _puckP1;
    public PuckMovementController PuckP2 => _puckP2;
    public Transform P1Spawn => _p1Spawn;
    public Transform P2Spawn => _p2Spawn;
    public GoalZone LevelGoalZone => _goalZone;
    public CameraDirector CameraDirector => _cameraDirector;
    public GameHUDController GameHUD => _gameHUD;
    public float MaxLaunchForce => _maxLaunchForce;
    public AimTrajectoryManager TrajectoryManager => _trajectoryManager;

    // Match State Tracking Variables
    public int CurrentPlayer { get; private set; } = 1;
    public int CurrentTurn { get; private set; } = 1;
    public float CalculatedLaunchForce { get; private set; }
    public float CurrentSpinOffset { get; private set; }
    public int P1TurnCount { get; private set; }
    public int P2TurnCount { get; private set; }
    public bool P1HasSpawned { get; set; }
    public bool P2HasSpawned { get; set; }
    public bool P1Finished { get; private set; }
    public bool P2Finished { get; private set; }
    public float P1FinalDistance { get; private set; }
    public float P2FinalDistance { get; private set; }
    public Vector3 P1AimDirection { get; set; }
    public Vector3 P2AimDirection { get; set; }
    public Vector3 CurrentLaunchDirection { get; set; }

    private IGameState _currentState;

    private void Awake()
    {
        ResetMatchData();

        // Load settings set by MainMenu
        CurrentDifficulty = MatchSettings.Difficulty;
        IsMultiplayer = MatchSettings.IsMultiplayer;

        // Clean up the board based on game mode
        if (IsMultiplayer)
        {
            Debug.Log("[GameManager] Starting in 2-Player Mode");
            if (PuckP2 != null) PuckP2.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("[GameManager] Starting in 1-Player Mode");
            if (PuckP2 != null) PuckP2.gameObject.SetActive(false);
        }

        Debug.Log($"[GameState] Match Initialized. Mode: {(IsMultiplayer ? "2P" : "1P")} | Difficulty: {CurrentDifficulty}");
    }

    private void Start()
    {
        // Initialize to MatchSetup State
        ChangeState(new State_MatchSetup(this));
    }

    private void Update()
    {
        _currentState?.UpdateState();
    }

    public void ChangeState(IGameState newState)
    {
        _currentState?.Exit();
        _currentState = newState;

        Debug.Log($"[GameStateManager] Swapped State to: {newState.GetType().Name}");
        _currentState?.Enter();
    }

    public PuckMovementController GetActivePuck()
    {
        return CurrentPlayer == 1 ? _puckP1 : _puckP2;
    }

    public void SetActivePlayer(int playerID)
    {
        CurrentPlayer = playerID;

        // Synchronize the pucks' active state with the current turn
        if (PuckP1 != null) PuckP1.IsActivePuck = (CurrentPlayer == 1);
        if (PuckP2 != null) PuckP2.IsActivePuck = (CurrentPlayer == 2);

        GameEvents.OnPlayerSwapped?.Invoke(CurrentPlayer);
    }

    public void IncrementTurn()
    {
        if (CurrentPlayer == 1)
        {
            P1TurnCount++;
            CurrentTurn = P1TurnCount;
        }
        else
        {
            P2TurnCount++;
            CurrentTurn = P2TurnCount;
        }

        // Notify the HUD
        GameEvents.OnTurnChanged?.Invoke(CurrentTurn);
    }

    public void SetCalculatedLaunchForce(float force)
    {
        CalculatedLaunchForce = force;
    }

    public void SetSpinOffset(float offset)
    {
        // Hardcoded range for now (also in State_AimOffset)
        CurrentSpinOffset = Mathf.Clamp(offset, -60f, 60f);
    }

    public void MarkPlayerFinished(int playerID, float finalDistance)
    {
        if (playerID == 1)
        {
            P1Finished = true;
            P1FinalDistance = finalDistance;
        }
        else
        {
            P2Finished = true;
            P2FinalDistance = finalDistance;
        }
    }

    public void ResetMatchData()
    {
        CurrentTurn = 0;
        P1TurnCount = 0;
        P2TurnCount = 0;
        CurrentPlayer = 1;
        CalculatedLaunchForce = 0f;
        CurrentSpinOffset = 0f;

        P1Finished = false;
        P2Finished = false;
        P1FinalDistance = 0f;
        P2FinalDistance = 0f;

        P1HasSpawned = false;
        P2HasSpawned = false;

        // Hide pucks momentarily (reposition)
        Vector3 bullpenPosition = new(0, -1f, 0);

        // Assign IDs and set P1 as the default active puck
        if (PuckP1 != null)
        {
            PuckP1.PlayerID = 1;
            PuckP1.IsActivePuck = true;
            PuckP1.ResetDistance();
            PuckP1.transform.position = bullpenPosition;
        }

        if (PuckP2 != null)
        {
            PuckP2.PlayerID = 2;
            PuckP2.IsActivePuck = false;
            PuckP2.ResetDistance();
            PuckP2.transform.position = bullpenPosition;
        }

        // Set the initial aim direction for Turn 1 to match the spawn orientation
        if (P1Spawn != null) P1AimDirection = P1Spawn.forward;

        // If P2 uses P1's spawn, just use P1Spawn.forward here too
        if (P2Spawn != null) P2AimDirection = P2Spawn.forward;
        else if (P1Spawn != null) P2AimDirection = P1Spawn.forward;
    }

    // ==========================================
    // DATA MODIFIER API
    // ==========================================

    public void AdjustActivePlayerTurn(int amount)
    {
        // Apply offset and clamp to a minimum of Turn 1
        if (CurrentPlayer == 1)
        {
            P1TurnCount = Mathf.Max(1, P1TurnCount + amount);
            CurrentTurn = P1TurnCount;
        }
        else
        {
            P2TurnCount = Mathf.Max(1, P2TurnCount + amount);
            CurrentTurn = P2TurnCount;
        }

        Debug.Log($"[GameState] Player {CurrentPlayer} turn adjusted by {amount}. Current Turn is now {CurrentTurn}.");

        // Notify the HUD
        GameEvents.OnTurnChanged?.Invoke(CurrentTurn);
    }
}
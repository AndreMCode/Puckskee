using UnityEngine;

public class Mod_ChangeTurn : MonoBehaviour, IPuckModifier
{
    public enum TurnChange { Decrement = -1, Increment = 1 }

    [Header("Turn Modification")]
    [Tooltip("Adds or removes a turn from the current player.")]
    [SerializeField] private TurnChange _turnAdjustment = TurnChange.Decrement;
    [SerializeField] private string _labelName = "Turn Shift";

    private GameStateManager _gameStateManager;

    public string ModName => _labelName;

    private void Awake()
    {
        _gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (_gameStateManager == null)
        {
            Debug.LogError("[Mod_ChangeTurn] Could not find GameStateManager in the scene!");
        }
    }

    public void ApplyModifier(PuckMovementController puck)
    {
        if (_gameStateManager != null)
        {
            // Cast the enum back to an integer (+1 or -1) and pass it to the manager
            _gameStateManager.AdjustActivePlayerTurn((int)_turnAdjustment);
        }
    }
}
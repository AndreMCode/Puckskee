using UnityEngine;

public class Mod_ChangeDamp : MonoBehaviour, IPuckModifier
{
    [Tooltip("Positive value adds friction, negative makes it slide further.")]
    [SerializeField] private float _dampingChange = 0.2f;
    [SerializeField] private float _minimumDamping = 0.1f;
    [SerializeField] private float _maximumDamping = 5.0f;
    [SerializeField] private string _labelName = "Modify Friction";

    public string ModName => _labelName;

    public void ApplyModifier(PuckMovementController puck)
    {
        puck.Motor.AdjustDamping(_dampingChange, _minimumDamping, _maximumDamping);
        GameEvents.OnFrictionUpdated?.Invoke(puck.MatchData.PlayerID, puck.Motor.CurrentDamping);
    }
}
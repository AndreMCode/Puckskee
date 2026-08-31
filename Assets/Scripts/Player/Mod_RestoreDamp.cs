using UnityEngine;

public class Mod_RestoreDamp : MonoBehaviour, IPuckModifier
{
    [SerializeField] private float _defaultDamping = 0.5f;
    [SerializeField] private string _labelName = "Normalize Friction";

    public string ModName => _labelName;

    public void ApplyModifier(PuckMovementController puck)
    {
        puck.Motor.SetDamping(_defaultDamping);
        GameEvents.OnFrictionUpdated?.Invoke(puck.MatchData.PlayerID, puck.Motor.CurrentDamping);
    }
}
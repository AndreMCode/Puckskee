using UnityEngine;

public class Mod_RestoreMass : MonoBehaviour, IPuckModifier
{
    [SerializeField] private float _defaultMass = 1.0f;
    [SerializeField] private string _labelName = "Normalize Mass";

    public string ModName => _labelName;

    public void ApplyModifier(PuckMovementController puck)
    {
        puck.Motor.SetMass(_defaultMass);
        GameEvents.OnMassUpdated?.Invoke(puck.MatchData.PlayerID, puck.Motor.CurrentMass);
    }
}
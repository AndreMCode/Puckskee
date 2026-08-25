using UnityEngine;

public class Mod_ChangeMass : MonoBehaviour, IPuckModifier
{
    [Tooltip("Positive value to increase mass, negative to decrease.")]
    [SerializeField] private float _massChange = 0.5f;
    [SerializeField] private float _minimumMass = 0.1f;
    [SerializeField] private float _maximumMass = 5.0f;
    [SerializeField] private string _labelName = "Modify Mass";

    public string ModName => _labelName;

    public void ApplyModifier(PuckMovementController puck)
    {
        puck.AdjustMass(_massChange, _minimumMass, _maximumMass);
    }
}
using UnityEngine;

public class Mod_ChangeVelocity : MonoBehaviour, IPuckModifier
{
    [Tooltip("Values > 1 will speed it up. Values < 1 will slow it down.")]
    [SerializeField] private float _velocityMultiplier = 1.2f;
    [SerializeField] private string _labelName = "Velocity Shift";

    public string ModName => _labelName;

    public void ApplyModifier(PuckMovementController puck)
    {
        puck.MultiplyVelocity(_velocityMultiplier);
    }
}
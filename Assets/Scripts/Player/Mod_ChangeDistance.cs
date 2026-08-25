using UnityEngine;

public class Mod_ChangeDistance : MonoBehaviour, IPuckModifier
{
    [Tooltip("Amount of distance to add or subtract.")]
    [SerializeField] private float _distanceChange = -50f;
    [SerializeField] private string _labelName = "Distance Warp";

    public string ModName => _labelName;

    public void ApplyModifier(PuckMovementController puck)
    {
        puck.AdjustDistance(_distanceChange);
    }
}
using UnityEngine;

public class Mod_ChangeDistance : MonoBehaviour, IPuckModifier
{
    [Tooltip("Amount of distance to add or subtract.")]
    [SerializeField] private float _distanceChange = -50f;
    [SerializeField] private string _labelName = "Distance Warp";

    public string ModName => _labelName;

    public void ApplyModifier(PuckMovementController puck)
    {
        puck.MatchData.AdjustDistance(_distanceChange);
        GameEvents.OnDistanceUpdated?.Invoke(puck.MatchData.PlayerID, puck.MatchData.TotalDistance);
    }
}
using UnityEngine;

[RequireComponent(typeof(PuckMotor), typeof(PuckPathManager))]
[RequireComponent(typeof(PuckVisuals), typeof(PuckMatchData))]
public class PuckMovementController : MonoBehaviour
{
    public PuckMotor Motor { get; private set; }
    public PuckPathManager PathManager { get; private set; }
    public PuckVisuals Visuals { get; private set; }
    public PuckMatchData MatchData { get; private set; }

    private void Awake()
    {
        Motor = GetComponent<PuckMotor>();
        PathManager = GetComponent<PuckPathManager>();
        Visuals = GetComponent<PuckVisuals>();
        MatchData = GetComponent<PuckMatchData>();
    }

    // ==========================================
    // FACADE ROUTING METHODS
    // ==========================================

    public void SetGhost(bool isGhost)
    {
        Motor.SetTriggerState(isGhost);
        Visuals.SetGhostVisuals(isGhost);
    }

    public void SpawnAt(Transform spawnAnchor)
    {
        Motor.SpawnAt(spawnAnchor);
        MatchData.BroadcastMatchStats(); // Triggers the HUD updates for Mass/Friction
    }
}
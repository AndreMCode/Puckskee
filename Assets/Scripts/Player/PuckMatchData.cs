using UnityEngine;

public class PuckMatchData : MonoBehaviour
{
    [Header("Distance Tracking Options")]
    public bool TrackInactiveDistance = false;

    public float TotalDistance { get; private set; }
    public int PlayerID { get; set; }
    public bool IsActivePuck { get; set; }

    private PuckMotor _motor;
    private PuckPathManager _pathManager;
    private Vector3 _lastPosition;

    private void Awake()
    {
        _motor = GetComponent<PuckMotor>();
        _pathManager = GetComponent<PuckPathManager>();
    }

    private void Start()
    {
        _lastPosition = transform.position;
    }

    private void Update()
    {
        // Distance tracking relies on the motor's physical state
        if (_motor != null && _motor.IsMoving)
        {
            TrackDistance();
        }
        else
        {
            // Keep the anchor fresh when stopped so it doesn't jump when bumped
            _lastPosition = transform.position;
        }
    }

    private void TrackDistance()
    {
        // Prevent tracking if both the puck is inactive and the toggle is disabled
        if (!IsActivePuck && !TrackInactiveDistance) return;

        Vector3 displacement = transform.position - _lastPosition;

        // Use sqrMagnitude to avoid square roots for the threshold check
        if (displacement.sqrMagnitude > 0.000001f)
        {
            TotalDistance += displacement.magnitude; // Only calculate the true distance if moving
            _lastPosition = transform.position;

            // Broadcast to the HUD
            GameEvents.OnDistanceUpdated?.Invoke(PlayerID, TotalDistance);
        }

        // Trigger intersection checks while moving
        if (_pathManager != null)
        {
            _pathManager.CheckIntersectionBoost();
        }
    }

    public void ResetDistance()
    {
        TotalDistance = 0f;
        GameEvents.OnDistanceUpdated?.Invoke(PlayerID, TotalDistance);
    }

    public void AdjustDistance(float amount)
    {
        TotalDistance = Mathf.Max(0f, TotalDistance + amount);
        GameEvents.OnDistanceUpdated?.Invoke(PlayerID, TotalDistance);
    }

    public void BroadcastMatchStats()
    {
        if (_motor != null)
        {
            GameEvents.OnMassUpdated?.Invoke(PlayerID, _motor.CurrentMass);
            GameEvents.OnFrictionUpdated?.Invoke(PlayerID, _motor.CurrentDamping);
        }
    }
}
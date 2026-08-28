using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(PathRecorder))]
public class PuckMovementController : MonoBehaviour
{
    [Header("Movement & Stop Physics")]
    [SerializeField] private float _stopThreshold = 0.05f;
    [SerializeField] private float _stopTimeRequired = 0.5f;
    [SerializeField] private GameObject _impactVFX;

    [Header("Intersection Boost Tracking")]
    [SerializeField] private int _maxRecordedSegments = 1;
    [SerializeField] private float _boostMultiplier = 1.5f;

    [Header("Distance Tracking Options")]
    public bool TrackInactiveDistance = false;

    [Header("Trajectory Math")]
    [Tooltip("Must match the dynamic friction of the floor's Physics Material.")]
    [SerializeField] private float _floorFriction = 0.6f;

    private PathRecorder _pathRecorder;
    private int _currentSegmentsRecorded = 0;
    private List<Vector3> _opponentPathToCheck;
    private PathRecorder _opponentPathRecorder;

    public float TotalDistance { get; private set; }
    public float Radius { get; private set; }
    public int PlayerID { get; set; }
    public bool IsActivePuck { get; set; }

    private Rigidbody _rb;
    private Collider _collider;

    // State Tracking
    private float _stopTimer;
    private bool _isMoving;
    private bool _isTrackingDistance;
    private Vector3 _lastPosition;

    // Physics Tracking
    private Vector3 _lastVelocity;
    private float _pendingSpinOffset;
    private Vector3 _lastValidDirection;

    public Vector3 GetLastTravelDirection() => _lastValidDirection;

    // Exposed for State Machine
    public bool IsMoving => _isMoving;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _pathRecorder = GetComponent<PathRecorder>();

        // Automatically calculate the radius based on the sphere collider and the object's scale
        if (_collider is SphereCollider sphere)
        {
            Radius = sphere.radius * transform.localScale.x;
        }
        else
        {
            Radius = 0.5f; // Fallback just in case
        }
    }

    private void FixedUpdate()
    {
        // We MUST record the velocity right before a collision happens
        // because Unity alters _rb.linearVelocity the moment it hits a wall
        _lastVelocity = _rb.linearVelocity;
    }

    private void Update()
    {
        // Auto-Wake: If this puck is resting but suddenly gets smacked
        if (!_isMoving && _rb.linearVelocity.magnitude > _stopThreshold)
        {
            _isMoving = true;
            _isTrackingDistance = true;
            _lastPosition = transform.position;
        }

        if (_isMoving) MonitorVelocity();
        if (_isTrackingDistance) TrackDistance();
    }

    // ==========================================
    // CORE MOVEMENT & PHYSICS
    // ==========================================

    public void Launch(Vector3 force, float spinOffset)
    {
        _rb.isKinematic = false;
        _rb.AddForce(force, ForceMode.Impulse);

        _pendingSpinOffset = spinOffset;
        _isMoving = true;
        _isTrackingDistance = true;
        _lastPosition = transform.position;
        _stopTimer = 0f;

        GameEvents.OnPuckLaunched?.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If we hit another puck, let Unity's physics handle the bounce
        if (collision.gameObject.TryGetComponent<PuckMovementController>(out var otherPuck))
        {
            // Stop path recording on Puck collision
            if (_pathRecorder != null)
            {
                _pathRecorder.StopRecording();
                MarkSegmentEnd();
            }

            _pendingSpinOffset = 0f; // Clear spin so it doesn't apply to the next wall

            if (_impactVFX != null && collision.contactCount > 0)
            {
                Instantiate(_impactVFX, collision.contacts[0].point, Quaternion.identity);
            }

            GameEvents.OnBumperHit?.Invoke();
            return;
        }

        // Unity physics override: Average the normals of all contact points
        Vector3 averageNormal = Vector3.zero;
        foreach (ContactPoint contact in collision.contacts)
        {
            averageNormal += contact.normal;
        }
        averageNormal = averageNormal.normalized;

        Vector3 obstacleVelocity = Vector3.zero;

        if (collision.rigidbody != null)
        {
            obstacleVelocity = collision.rigidbody.linearVelocity;
        }

        // Have the utility calculate
        _rb.linearVelocity = PuckPhysicsUtility.CalculateReflectionVelocity(
            _lastVelocity,
            obstacleVelocity,
            averageNormal,
            _pendingSpinOffset
        );

        // Clear the spin so it only applies to the first bounce
        _pendingSpinOffset = 0f;

        // Trigger visuals and audio
        if (_impactVFX != null && collision.contactCount > 0)
        {
            Instantiate(_impactVFX, collision.contacts[0].point, Quaternion.identity);
        }

        // Play standard hit sound for all generic wall collisions
        GameEvents.OnBumperHit?.Invoke();

        MarkSegmentEnd();
    }

    private void MonitorVelocity()
    {
        float currentSpeed = _rb.linearVelocity.magnitude;

        // Constantly record the direction while moving fast enough
        if (currentSpeed > _stopThreshold)
        {
            _lastValidDirection = _rb.linearVelocity.normalized;
        }

        if (currentSpeed < _stopThreshold)
        {
            _stopTimer += Time.deltaTime;

            if (_stopTimer >= _stopTimeRequired)
            {
                // The puck has officially stopped
                _isMoving = false;
                _stopTimer = 0f;
                _isTrackingDistance = false;

                GameEvents.OnPuckStopped?.Invoke();
            }
        }
        else
        {
            // Reset timer if it gets bumped and speeds up again
            _stopTimer = 0f;
        }
    }

    // ==========================================
    // DISTANCE TRACKING & MODIFICATION
    // ==========================================

    private void TrackDistance()
    {
        // Prevent tracking if both the puck is inactive and the toggle is disabled
        if (!IsActivePuck && !TrackInactiveDistance) return;

        float distCovered = Vector3.Distance(transform.position, _lastPosition);
        if (distCovered > 0.001f)
        {
            TotalDistance += distCovered;
            _lastPosition = transform.position;

            // Broadcast with the unique Player ID
            GameEvents.OnDistanceUpdated?.Invoke(PlayerID, TotalDistance);
        }

        CheckIntersectionBoost();
    }

    public void ModifyDistance(float amount)
    {
        TotalDistance += amount;
        TotalDistance = Mathf.Max(0, TotalDistance); // Prevent negative distance
        GameEvents.OnDistanceUpdated?.Invoke(PlayerID, TotalDistance);
    }

    public void ResetDistance()
    {
        TotalDistance = 0f;
        GameEvents.OnDistanceUpdated?.Invoke(PlayerID, TotalDistance);
    }

    public void StopTrackingDistance()
    {
        _isTrackingDistance = false;
    }

    // ==========================================
    // PATH RECORDING & SEGMENT CONTROL
    // ==========================================

    public void StartPathRecording()
    {
        _currentSegmentsRecorded = 0;
        if (_pathRecorder != null) _pathRecorder.StartRecording();
    }

    public void StopPathRecording()
    {
        if (_pathRecorder != null) _pathRecorder.StopRecording();
    }

    public void MarkSegmentEnd()
    {
        _currentSegmentsRecorded++;
        if (_currentSegmentsRecorded >= _maxRecordedSegments)
        {
            StopPathRecording();
        }
    }

    public void SetupBoostCheck(PathRecorder opponentRecorder)
    {
        if (opponentRecorder != null && opponentRecorder.PreviousPath != null && opponentRecorder.PreviousPath.Count > 1)
        {
            _opponentPathToCheck = opponentRecorder.PreviousPath;
            _opponentPathRecorder = opponentRecorder;
        }
        else
        {
            _opponentPathToCheck = null;
            _opponentPathRecorder = null;
        }
    }

    public void ShowPath()
    {
        if (_pathRecorder != null) _pathRecorder.ShowBoostPath();
    }

    public void HidePath()
    {
        if (_pathRecorder != null) _pathRecorder.HideBoostPath();
    }

    // ==========================================
    // BOOST MATH
    // ==========================================

    private void CheckIntersectionBoost()
    {
        if (_opponentPathToCheck == null) return;

        Vector3 pos = transform.position;

        for (int i = 0; i < _opponentPathToCheck.Count - 1; i++)
        {
            // Using the dynamic Radius
            if (PointToSegmentDistance(pos, _opponentPathToCheck[i], _opponentPathToCheck[i + 1]) < Radius)
            {
                ApplyBoost();
                break;
            }
        }
    }

    private void ApplyBoost()
    {
        _rb.linearVelocity *= _boostMultiplier;
        _opponentPathToCheck = null; // Consume the boost so it only happens once per shot

        // Hide the opponent's boost line
        if (_opponentPathRecorder != null)
        {
            _opponentPathRecorder.HideBoostPath();
            _opponentPathRecorder = null;
        }

        Debug.Log("[Puck] INTERSECTION BOOST APPLIED!");
    }

    private float PointToSegmentDistance(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float t = Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        Vector3 closest = a + t * ab;
        return Vector3.Distance(p, closest);
    }

    // ==========================================
    // TRAJECTORY MATH
    // ==========================================

    public float GetMaxTravelDistance(float maxLaunchForce)
    {
        float mass = _rb.mass;
        float damping = Mathf.Max(_rb.linearDamping, 0.0001f);

        // V0: Initial Velocity
        float v0 = maxLaunchForce / mass;

        // Af: Constant deceleration from floor friction (mu * g)
        float gravity = Mathf.Abs(Physics.gravity.y);
        float af = _floorFriction * gravity;

        // Apply the integral formula:
        // D = (V0 / c) - (Af / c^2) * ln(1 + (V0 * c) / Af)

        float term1 = v0 / damping;
        float term2 = af / (damping * damping);
        float logTerm = Mathf.Log(1f + ((v0 * damping) / af));

        return term1 - (term2 * logTerm);
    }

    // ==========================================
    // MODIFIER API
    // ==========================================

    public void AdjustMass(float amount, float minMass, float maxMass)
    {
        float newMass = Mathf.Clamp(_rb.mass + amount, minMass, maxMass);
        _rb.mass = newMass;
        GameEvents.OnMassUpdated?.Invoke(PlayerID, _rb.mass);
        Debug.Log($"[Puck] Mass adjusted by {amount}. New Mass: {_rb.mass:F2}");
    }

    public void SetMass(float exactMass)
    {
        _rb.mass = exactMass;
        GameEvents.OnMassUpdated?.Invoke(PlayerID, _rb.mass);
        Debug.Log($"[Puck] Mass restored to: {_rb.mass:F2}");
    }

    public void MultiplyVelocity(float multiplier)
    {
        _rb.linearVelocity *= multiplier;
        Debug.Log($"[Puck] Velocity multiplied by {multiplier}. New Velocity: {_rb.linearVelocity.magnitude:F2}");
    }

    public void AdjustDamping(float amount, float minDamp, float maxDamp)
    {
        float newDamp = Mathf.Clamp(_rb.linearDamping + amount, minDamp, maxDamp);
        _rb.linearDamping = newDamp;
        GameEvents.OnFrictionUpdated?.Invoke(PlayerID, _rb.linearDamping);
        Debug.Log($"[Puck] Damping adjusted by {amount}. New Damping: {_rb.linearDamping:F2}");
    }

    public void SetDamping(float exactDamp)
    {
        _rb.linearDamping = exactDamp;
        GameEvents.OnFrictionUpdated?.Invoke(PlayerID, _rb.linearDamping);
        Debug.Log($"[Puck] Damping restored to: {_rb.linearDamping:F2}");
    }

    public void AdjustDistance(float amount)
    {
        // Add the amount (negative or positive) and clamp the floor to 0
        TotalDistance = Mathf.Max(0f, TotalDistance + amount);

        Debug.Log($"[Puck] Distance adjusted by {amount}. New Distance: {TotalDistance:F2}m");

        // Update the HUD
        GameEvents.OnDistanceUpdated?.Invoke(PlayerID, TotalDistance);
    }

    // ==========================================
    // UTILITY & RESET
    // ==========================================

    public void SpawnAt(Transform spawnAnchor)
    {
        // Zero out all physical momentum
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Synchronize PhysX buffer and transform positions directly
        _rb.position = spawnAnchor.position;
        _rb.rotation = spawnAnchor.rotation;
        transform.SetPositionAndRotation(spawnAnchor.position, spawnAnchor.rotation);
        Physics.SyncTransforms();

        _isMoving = false;
        _isTrackingDistance = false;
        _stopTimer = 0f;

        // Also initialize the travel direction when spawned
        _lastValidDirection = spawnAnchor.forward;

        // Update HUD
        GameEvents.OnMassUpdated?.Invoke(PlayerID, _rb.mass);
        GameEvents.OnFrictionUpdated?.Invoke(PlayerID, _rb.linearDamping);
    }

    public void SetGhost(bool isGhost)
    {
        // Converts the collider to a trigger so physical pucks pass through it
        if (_collider != null)
        {
            _collider.isTrigger = isGhost;
        }

        // Grab all renderers attached to this puck or its children
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer ren in childRenderers)
        {
            if (ren != null)
            {
                Color color = ren.material.color;
                // Lower opacity to 50% if ghosted, otherwise return to full opacity
                color.a = isGhost ? 0.5f : 1.0f;
                ren.material.color = color;
            }
        }
    }
}
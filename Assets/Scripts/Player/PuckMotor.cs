using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PuckMotor : MonoBehaviour
{
    [Header("Movement & Stop Physics")]
    [SerializeField] private float _stopThreshold = 0.05f;
    [SerializeField] private float _stopTimeRequired = 0.5f;

    [Header("Trajectory Math")]
    [Tooltip("Must match the dynamic friction of the floor's Physics Material.")]
    [SerializeField] private float _floorFriction = 0.1f;

    [Header("Bullet Time Prediction")]
    [SerializeField] private float _bulletTimeTriggerDistance = 2f;
    [SerializeField] private LayerMask _predictionMask;

    public float Radius { get; private set; }
    public bool IsMoving { get; private set; }
    public Vector3 LastVelocity { get; private set; }

    private Rigidbody _rb;
    private Collider _collider;

    // Sibling component references
    private PuckVisuals _visuals;
    private PuckPathManager _pathManager;

    // State Tracking
    private float _stopTimer;
    private Vector3 _lastValidDirection;
    private float _pendingSpinOffset;

    // Slow-mo tracker variables
    private bool _isApproachingPuck;
    private float _expectedImpactDistance;
    private Vector3 _segmentStartPosition;
    private bool _hasTriggeredBulletTime;

    public Vector3 GetLastTravelDirection() => _lastValidDirection;
    public float CurrentMass => _rb.mass;
    public float CurrentDamping => _rb.linearDamping;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _visuals = GetComponent<PuckVisuals>();
        _pathManager = GetComponent<PuckPathManager>();

        if (_collider is SphereCollider sphere)
        {
            Radius = sphere.radius * transform.localScale.x;
        }
        else
        {
            Radius = 0.5f;
        }
    }

    private void FixedUpdate()
    {
        LastVelocity = _rb.linearVelocity;
    }

    private void Update()
    {
        // Auto-Wake
        if (!IsMoving && _rb.linearVelocity.magnitude > _stopThreshold)
        {
            IsMoving = true;
        }

        if (_isApproachingPuck && IsMoving)
        {
            float distanceCovered = Vector3.Distance(_segmentStartPosition, transform.position);
            float distanceRemaining = _expectedImpactDistance - distanceCovered;

            if (distanceRemaining <= _bulletTimeTriggerDistance && distanceRemaining > 0f)
            {
                if (!_hasTriggeredBulletTime)
                {
                    _hasTriggeredBulletTime = true;
                    MatchTimeManager.Instance.TriggerBulletTime();
                }
            }
            else if (distanceRemaining < -Radius)
            {
                _isApproachingPuck = false;
                _hasTriggeredBulletTime = false;
                MatchTimeManager.Instance.RestoreNormalTime();
            }
        }

        if (IsMoving) MonitorVelocity();
    }

    // ==========================================
    // CORE MOVEMENT & PHYSICS
    // ==========================================

    public void Launch(Vector3 force, float spinOffset)
    {
        _rb.isKinematic = false;
        _rb.AddForce(force, ForceMode.Impulse);

        _pendingSpinOffset = spinOffset;
        IsMoving = true;
        _stopTimer = 0f;

        EvaluateNextSegment(transform.position, force.normalized);

        GameEvents.OnPuckLaunched?.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If we hit another puck, override the physics manually
        if (collision.gameObject.TryGetComponent<PuckMotor>(out var otherPuck))
        {
            // Stop path recording when colliding with another puck
            if (_pathManager != null)
            {
                _pathManager.StopPathRecording();
                _pathManager.MarkSegmentEnd();
            }

            _pendingSpinOffset = 0f; // Clear spin so it doesn't apply to the next wall

            if (_visuals != null && collision.contactCount > 0)
            {
                _visuals.PlayImpact(collision.contacts[0].point, collision.contacts[0].normal);
            }

            GameEvents.OnBumperHit?.Invoke();
            _isApproachingPuck = false;

            // Bullet time delay post-collision
            MatchTimeManager.Instance.RestoreNormalTimeDelayed(1f);
            return;
        }

        // Override physics contacts/normals with verified ones
        Vector3 incomingDir = LastVelocity.normalized;

        // Pull the origin back along the incoming vector
        Vector3 resolvedNormal = Vector3.zero;
        float castDistance = Radius * 2f;
        Vector3 safeOrigin = transform.position - (incomingDir * castDistance);

        // Cast forward to find the true geometric normal
        if (Physics.SphereCast(safeOrigin, Radius, incomingDir, out RaycastHit hit, castDistance * 1.5f))
        {
            // Verify we actually hit the obstacle we collided with
            if (hit.collider == collision.collider) resolvedNormal = hit.normal;
        }

        // Use Unity's contact points if the cast missed somehow
        if (resolvedNormal == Vector3.zero)
        {
            foreach (ContactPoint contact in collision.contacts) resolvedNormal += contact.normal;
            resolvedNormal = resolvedNormal.normalized;
        }

        Vector3 obstacleVelocity = collision.rigidbody != null ? collision.rigidbody.linearVelocity : Vector3.zero;

        // Have the utility calculate
        _rb.linearVelocity = PuckPhysicsUtility.CalculateReflectionVelocity(
            LastVelocity, obstacleVelocity, resolvedNormal, _pendingSpinOffset
        );

        // Clear the spin so it only applies to the first bounce
        _pendingSpinOffset = 0f;

        // Cancel any pending bullet time and re-evaluate here since did not hit another puck
        _isApproachingPuck = false;
        MatchTimeManager.Instance.RestoreNormalTime();
        EvaluateNextSegment(transform.position, _rb.linearVelocity.normalized);

        // Trigger visuals and audio
        if (_visuals != null && collision.contactCount > 0)
        {
            _visuals.PlayImpact(collision.contacts[0].point, collision.contacts[0].normal);
        }

        // Play standard hit sound for all generic wall collisions
        GameEvents.OnBumperHit?.Invoke();

        if (_pathManager != null) _pathManager.MarkSegmentEnd();
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
                IsMoving = false;
                _stopTimer = 0f;
                _hasTriggeredBulletTime = false;
                MatchTimeManager.Instance.RestoreNormalTime();

                GameEvents.OnPuckStopped?.Invoke();
            }
        }
        else
        {
            // Reset timer if it gets bumped and speeds up again
            _stopTimer = 0f;
        }
    }

    private void EvaluateNextSegment(Vector3 startPos, Vector3 direction)
    {
        _isApproachingPuck = false;
        _segmentStartPosition = startPos;
        float maxDist = GetMaxTravelDistance(100f);

        if (Physics.SphereCast(startPos, Radius, direction, out RaycastHit hit, maxDist, _predictionMask))
        {
            if (hit.collider.TryGetComponent<PuckMotor>(out _))
            {
                _isApproachingPuck = true;
                _expectedImpactDistance = hit.distance;
            }
        }
    }

    // ==========================================
    // UTILITY & MODIFIERS
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

        IsMoving = false;
        _stopTimer = 0f;

        // Also initialize the travel direction when spawned
        _lastValidDirection = spawnAnchor.forward;
    }

    public void SetTriggerState(bool isTrigger)
    {
        if (_collider != null) _collider.isTrigger = isTrigger;
    }

    public void AdjustMass(float amount, float minMass, float maxMass) => _rb.mass = Mathf.Clamp(_rb.mass + amount, minMass, maxMass);
    public void SetMass(float exactMass) => _rb.mass = exactMass;
    public void AdjustDamping(float amount, float minDamp, float maxDamp) => _rb.linearDamping = Mathf.Clamp(_rb.linearDamping + amount, minDamp, maxDamp);
    public void SetDamping(float exactDamp) => _rb.linearDamping = exactDamp;
    public void MultiplyVelocity(float multiplier) => _rb.linearVelocity *= multiplier;
}
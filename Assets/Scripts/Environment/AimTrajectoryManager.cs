using UnityEngine;

public class AimTrajectoryManager : MonoBehaviour
{
    [Header("Trajectory Visuals (Line Renderers)")]
    [Tooltip("The primary line from the puck to the first collision.")]
    [SerializeField] private LineRenderer _preBounceLine;

    [Tooltip("The line showing where the puck goes after the first collision.")]
    [SerializeField] private LineRenderer _postBounceTail;

    [Tooltip("The line showing where the puck goes after the second collision.")]
    [SerializeField] private LineRenderer _secondaryTail;

    [Header("Parameters")]
    [Tooltip("How far the laser sight should travel if it hits nothing.")]
    [SerializeField] private float _maxPredictionDistance = 100f;

    [Tooltip("The layers the trajectory laser should interact (collide) with.")]
    [SerializeField] private LayerMask _bounceMask;

    [Tooltip("Offsets the rendered line visually without affecting physics calculations.")]
    [SerializeField] private Vector3 _visualOffset = new(0f, -0.1f, 0f);

    private void Awake()
    {
        HideTrajectory();
    }

    public void ShowTrajectory(Vector3 startPos, Vector3 direction, float currentSpinOffset, float puckRadius)
    {
        // Initial SphereCast
        if (Physics.SphereCast(startPos, puckRadius, direction, out RaycastHit hit1, _maxPredictionDistance, _bounceMask))
        {
            // Calculate the true center of the puck at the first impact
            Vector3 puckCenterAtImpact1 = startPos + (direction * hit1.distance);

            // Draw line to the impact center of the puck, not the wall surface
            DrawLine(_preBounceLine, startPos, puckCenterAtImpact1);

            // --- CALCULATE FIRST BOUNCE ---
            Vector3 bounce1Direction = CalculateBounce(direction, hit1, currentSpinOffset);

            // Nudge the start position slightly along the bounce vector to prevent self-intersection with the wall
            // Vector3 secondCastStartPos = puckCenterAtImpact1 + (bounce1Direction * 0.01f); <-- FLAGGED for removal

            // Subsequent SphereCast
            if (Physics.SphereCast(puckCenterAtImpact1, puckRadius, bounce1Direction, out RaycastHit hit2, _maxPredictionDistance * 0.25f, _bounceMask))
            {
                // Calculate the true center of the puck at the second impact
                Vector3 puckCenterAtImpact2 = puckCenterAtImpact1 + (bounce1Direction * hit2.distance);

                // Draw line from the first impact center to the second impact center
                DrawLine(_postBounceTail, puckCenterAtImpact1, puckCenterAtImpact2);

                // --- CALCULATE SECOND BOUNCE ---
                Vector3 bounce2Direction = CalculateBounce(bounce1Direction, hit2, 0f);

                // Draw final tail
                Vector3 tailEnd = puckCenterAtImpact2 + (bounce2Direction.normalized * 3f);
                DrawLine(_secondaryTail, puckCenterAtImpact2, tailEnd);
            }
            else
            {
                // No second collision, draw partial tail
                Vector3 tailEnd = puckCenterAtImpact1 + (bounce1Direction.normalized * 5f);
                DrawLine(_postBounceTail, puckCenterAtImpact1, tailEnd);

                // Omit third line
                SetLineActive(_secondaryTail, false);
            }
        }
        else
        {
            // First cast hit absolutely nothing (open space)
            Vector3 endPos = startPos + (direction * _maxPredictionDistance);
            DrawLine(_preBounceLine, startPos, endPos);

            // Omit second and third lines
            SetLineActive(_postBounceTail, false);
            SetLineActive(_secondaryTail, false);
        }
    }

    private Vector3 CalculateBounce(Vector3 incomingDirection, RaycastHit hit, float spinOffset)
    {
        float multiplier = 1.0f;

        // Use the exact same math utility the Puck uses
        // Pass the normalized direction as velocity. The utility returns the new direction.
        Vector3 exitVelocity = PuckPhysicsUtility.CalculateReflectionVelocity(
            incomingVelocity: incomingDirection.normalized,
            obstacleVelocity: Vector3.zero, // Assume static for aim preview
            surfaceNormal: hit.normal,
            bouncinessMultiplier: multiplier,
            spinOffsetAngle: spinOffset
        );

        return exitVelocity.normalized;
    }

    public Vector3 GetFirstHitPoint(Vector3 startPos, Vector3 direction, float puckRadius)
    {
        if (Physics.SphereCast(startPos, puckRadius, direction, out RaycastHit hit, _maxPredictionDistance, _bounceMask))
        {
            return hit.point;
        }

        // Fallback if aiming into the void
        return startPos + (direction * _maxPredictionDistance);
    }

    public Vector3 GetFirstHitNormal(Vector3 startPos, Vector3 direction, float puckRadius)
    {
        if (Physics.SphereCast(startPos, puckRadius, direction, out RaycastHit hit, _maxPredictionDistance, _bounceMask))
        {
            return hit.normal;
        }

        // Fallback if aiming into the void
        return -direction.normalized;
    }

    public void HideTrajectory()
    {
        SetLineActive(_preBounceLine, false);
        SetLineActive(_postBounceTail, false);
        SetLineActive(_secondaryTail, false);
    }

    // ==========================================
    // UTILITY DRAWING
    // ==========================================

    private void DrawLine(LineRenderer line, Vector3 start, Vector3 end)
    {
        if (line == null) return;

        SetLineActive(line, true);

        // Apply the offset (lowering the line to the game floor)
        line.SetPosition(0, start + _visualOffset);
        line.SetPosition(1, end + _visualOffset);
    }

    private void SetLineActive(LineRenderer line, bool isActive)
    {
        if (line != null && line.gameObject.activeSelf != isActive)
        {
            line.gameObject.SetActive(isActive);
        }
    }
}
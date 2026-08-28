using System.Collections.Generic;
using UnityEngine;

public class AimTrajectoryManager : MonoBehaviour
{
    [Header("Trajectory Visuals")]
    [Tooltip("The single continuous LineRenderer used for the entire trajectory.")]
    [SerializeField] private LineRenderer _trajectoryLine;

    [Header("Parameters")]
    [Tooltip("The maximum number of times the prediction will bounce off walls before stopping.")]
    [SerializeField] private int _maxBounces = 3;

    [Tooltip("The layers the trajectory laser should interact (collide) with.")]
    [SerializeField] private LayerMask _bounceMask;

    [Tooltip("Offsets the rendered line visually without affecting physics calculations.")]
    [SerializeField] private Vector3 _visualOffset = new(0f, -0.1f, 0f);

    private readonly List<Vector3> _trajectoryPoints = new List<Vector3>(10);

    private void Awake()
    {
        HideTrajectory();
    }

    public void ShowTrajectory(Vector3 startPos, Vector3 direction, float currentSpinOffset, float puckRadius, float maxDistance)
    {
        float remainingDist = maxDistance;
        Vector3 currentPos = startPos;
        Vector3 currentDir = direction;

        _trajectoryPoints.Clear();
        _trajectoryPoints.Add(currentPos); // Adds the initial point

        for (int i = 0; i <= _maxBounces; i++)
        {
            if (Physics.SphereCast(currentPos, puckRadius, currentDir, out RaycastHit hit, remainingDist, _bounceMask))
            {
                Vector3 impactCenter = currentPos + (currentDir * hit.distance);
                _trajectoryPoints.Add(impactCenter);

                remainingDist -= hit.distance;

                // Stop calculating if we've run out of distance OR hit our bounce limit
                if (remainingDist <= 0.001f || i == _maxBounces) break;

                // Only the first collision calculates with the player's spin offset
                float spinToApply = (i == 0) ? currentSpinOffset : 0f;

                // Calculate bounce direction for the next loop iteration
                currentDir = CalculateBounce(currentDir, hit, spinToApply);
                currentPos = impactCenter;
            }
            else
            {
                // First cast hit nothing (open space cutoff)
                Vector3 endPos = currentPos + (currentDir * remainingDist);
                _trajectoryPoints.Add(endPos);
                break; // Finished predicting
            }
        }

        DrawTrajectory();
    }

    private Vector3 CalculateBounce(Vector3 incomingDirection, RaycastHit hit, float spinOffset)
    {
        // Use the exact same math utility the Puck uses
        Vector3 exitVelocity = PuckPhysicsUtility.CalculateReflectionVelocity(
            incomingVelocity: incomingDirection.normalized,
            obstacleVelocity: Vector3.zero, // Assume static for aim preview
            surfaceNormal: hit.normal,
            spinOffsetAngle: spinOffset
        );

        return exitVelocity.normalized;
    }

    public Vector3 GetFirstHitPoint(Vector3 startPos, Vector3 direction, float puckRadius, float maxDistance)
    {
        if (Physics.SphereCast(startPos, puckRadius, direction, out RaycastHit hit, maxDistance, _bounceMask))
        {
            return hit.point;
        }

        // Fallback if aiming into the void
        return startPos + (direction * maxDistance);
    }

    public Vector3 GetFirstHitNormal(Vector3 startPos, Vector3 direction, float puckRadius, float maxDistance)
    {
        if (Physics.SphereCast(startPos, puckRadius, direction, out RaycastHit hit, maxDistance, _bounceMask))
        {
            return hit.normal;
        }

        // Fallback if aiming into the void
        return -direction.normalized;
    }

    public void HideTrajectory()
    {
        if (_trajectoryLine != null)
        {
            _trajectoryLine.gameObject.SetActive(false);
        }
    }

    // ==========================================
    // UTILITY DRAWING
    // ==========================================

    private void DrawTrajectory()
    {
        if (_trajectoryLine == null) return;

        if (!_trajectoryLine.gameObject.activeSelf)
        {
            _trajectoryLine.gameObject.SetActive(true);
        }

        _trajectoryLine.positionCount = _trajectoryPoints.Count;

        for (int i = 0; i < _trajectoryPoints.Count; i++)
        {
            // Apply the offset (lowering the line to the game floor)
            _trajectoryLine.SetPosition(i, _trajectoryPoints[i] + _visualOffset);
        }
    }
}
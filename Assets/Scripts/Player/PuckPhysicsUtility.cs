using UnityEngine;

public static class PuckPhysicsUtility
{
    public static Vector3 CalculateReflectionVelocity(
        Vector3 incomingVelocity,
        Vector3 obstacleVelocity,
        Vector3 surfaceNormal,
        float spinOffsetAngle)
    {
        // Calculate relative velocity (for moving obstacles)
        Vector3 relativeIncomingVel = incomingVelocity - obstacleVelocity;

        // Prevent dividing by zero
        if (relativeIncomingVel.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 incomingDir = relativeIncomingVel.normalized;

        // Base reflection off the surface
        Vector3 reflectedDir = Vector3.Reflect(incomingDir, surfaceNormal);

        // Apply the Spin Offset
        if (spinOffsetAngle != 0f)
        {
            reflectedDir = Quaternion.Euler(0, spinOffsetAngle, 0) * reflectedDir;
        }

        // Calculate the exit speed
        Vector3 reflectedRelativeVel = reflectedDir * relativeIncomingVel.magnitude;

        // Convert back to world space
        return reflectedRelativeVel + obstacleVelocity;
    }
}
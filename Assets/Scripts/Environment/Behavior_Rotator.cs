using UnityEngine;

public class Behavior_Rotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("The axis to rotate around. Defaults to Y-axis.")]
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;

    [Tooltip("Degrees per second.")]
    [SerializeField] private float _rotationSpeed = 90f;

    [Tooltip("If true, clockwise (positive direction).")]
    [SerializeField] private bool _isClockwise = true;

    private void Update()
    {
        // Determine direction
        float direction = _isClockwise ? 1f : -1f;

        // Apply continuous rotation relative to the object's own local space
        transform.Rotate(_rotationAxis.normalized, _rotationSpeed * direction * Time.deltaTime, Space.Self);
    }
}
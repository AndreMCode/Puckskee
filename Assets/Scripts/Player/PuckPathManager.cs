using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathRecorder))]
public class PuckPathManager : MonoBehaviour
{
    [Header("Intersection Boost Tracking")]
    [SerializeField] private int _maxRecordedSegments = 1;

    // [SerializeField] private float _boostMultiplier = 1.5f;
    // Currently replaced by:
    private float _maxLaunchForce; // for testing

    private PathRecorder _pathRecorder;
    private Rigidbody _rb;
    private SphereCollider _sphereCollider;

    private int _currentSegmentsRecorded = 0;
    private List<Vector3> _opponentPathToCheck;
    private PathRecorder _opponentPathRecorder;

    private float _puckRadius = 0.5f;

    private void Awake()
    {
        _pathRecorder = GetComponent<PathRecorder>();
        _rb = GetComponent<Rigidbody>();
        _sphereCollider = GetComponent<SphereCollider>();

        // Cache the radius for intersection proximity checks
        if (_sphereCollider != null)
        {
            _puckRadius = _sphereCollider.radius * transform.localScale.x;
        }
    }

    public void SetMaxLaunchForce(float maxLaunchForce)
    {
        _maxLaunchForce = maxLaunchForce;
    }

    // ==========================================
    // PATH RECORDING CONTROL
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

    public void ShowPath()
    {
        if (_pathRecorder != null) _pathRecorder.ShowBoostPath();
    }

    public void HidePath()
    {
        if (_pathRecorder != null) _pathRecorder.HideBoostPath();
    }

    // ==========================================
    // BOOST SETUP & INTERSECTION MATH
    // ==========================================

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

    public void CheckIntersectionBoost()
    {
        if (_opponentPathToCheck == null) return;

        Vector3 currentPos = transform.position;

        for (int i = 0; i < _opponentPathToCheck.Count - 1; i++)
        {
            // Using the dynamic Radius
            if (PointToSegmentDistance(currentPos, _opponentPathToCheck[i], _opponentPathToCheck[i + 1]) < _puckRadius)
            {
                ApplyBoost();
                break;
            }
        }
    }

    private void ApplyBoost()
    {
        //if (_rb != null)
        //{
        //    _rb.linearVelocity *= _boostMultiplier;
        //}
        // Currently replaced by:
        if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.01f) // Prevent a boost with no direction
        {
            Vector3 travelDir = _rb.linearVelocity.normalized;
            _rb.linearVelocity = travelDir * (_maxLaunchForce / _rb.mass);
        }
        // for testing

        _opponentPathToCheck = null; // Consume boost once hit

        // Hide the opponent's boost line
        if (_opponentPathRecorder != null)
        {
            _opponentPathRecorder.HideBoostPath();
            _opponentPathRecorder = null;
        }

        Debug.Log("[PuckPathManager] INTERSECTION BOOST APPLIED!");
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
}
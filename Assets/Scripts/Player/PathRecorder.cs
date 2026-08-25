using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class PathRecorder : MonoBehaviour
{
    [SerializeField] private float _minPointDistance = 0.1f;

    private LineRenderer _ghostLine;
    private List<Vector3> _currentPath = new();
    private List<Vector3> _previousPath = new();
    private bool _isRecording = false;

    public List<Vector3> PreviousPath => _previousPath;

    private void Awake()
    {
        _ghostLine = GetComponent<LineRenderer>();
        HideGhostPath();
    }

    public void StartRecording()
    {
        _currentPath.Clear();
        _currentPath.Add(transform.position);
        _isRecording = true;
    }

    public void StopRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;

        // Save this turn's path for the opponent to see next turn
        _previousPath = new List<Vector3>(_currentPath);
    }

    private void Update()
    {
        if (_isRecording)
        {
            Vector3 currentPos = transform.position;
            if (Vector3.Distance(currentPos, _currentPath[_currentPath.Count - 1]) > _minPointDistance)
            {
                _currentPath.Add(currentPos);
            }
        }
    }

    public void ShowGhostPath()
    {
        if (_ghostLine == null || _previousPath.Count < 2) return;
        _ghostLine.enabled = true;
        _ghostLine.positionCount = _previousPath.Count;
        _ghostLine.SetPositions(_previousPath.ToArray());
    }

    public void HideGhostPath()
    {
        if (_ghostLine != null) _ghostLine.enabled = false;
    }
}
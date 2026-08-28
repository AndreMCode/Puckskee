using UnityEngine;
using System.Collections;

public class Behavior_Patrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("The starting position relative to where you placed the object.")]
    [SerializeField] private Vector3 _localPointA = Vector3.zero;

    [Tooltip("The destination position relative to where you placed the object.")]
    [SerializeField] private Vector3 _localPointB = new(5f, 0f, 0f);

    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _waitTimeAtPoint = 0f;

    private Vector3 _worldPointA;
    private Vector3 _worldPointB;

    private void Start()
    {
        // Convert the local Inspector offsets into absolute world coordinates 
        _worldPointA = transform.position + _localPointA;
        _worldPointB = transform.position + _localPointB;

        // Object starts at Point A
        transform.position = _worldPointA;

        StartCoroutine(PatrolRoutine());
    }

    private IEnumerator PatrolRoutine()
    {
        Vector3 target = _worldPointB;
        Vector3 start = _worldPointA;
        WaitForSeconds waitTime = new WaitForSeconds(_waitTimeAtPoint);

        while (true)
        {
            // Move towards the current target
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, _patrolSpeed * Time.deltaTime);
                yield return null; // Wait for the next frame
            }

            // Snap to exact position
            transform.position = target;

            // Wait if set
            if (_waitTimeAtPoint > 0f)
            {
                yield return waitTime;
            }

            // Swap targets for the ping-pong effect
            (start, target) = (target, start);
            // New format, formerly:
            // Vector3 temp = target;
            // target = start;
            // start = temp;
        }
    }

    // ==========================================
    // EDITOR VISUALIZATION
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        // Draw the path in the Scene view
        Vector3 wa = Application.isPlaying ? _worldPointA : transform.position + _localPointA;
        Vector3 wb = Application.isPlaying ? _worldPointB : transform.position + _localPointB;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(wa, 0.25f);
        Gizmos.DrawWireSphere(wb, 0.25f);
        Gizmos.DrawLine(wa, wb);
    }
}
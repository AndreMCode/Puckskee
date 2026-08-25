using UnityEngine;
using System.Collections.Generic;

public class GoalZone : MonoBehaviour
{
    private readonly HashSet<GameObject> _pucksInZone = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PuckMovementController>() != null)
        {
            _pucksInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PuckMovementController>() != null)
        {
            _pucksInZone.Remove(other.gameObject);
        }
    }

    public bool IsPuckInside(GameObject puck) => _pucksInZone.Contains(puck);
}
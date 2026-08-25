using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Behavior Settings")]
    [Tooltip("If true, the object destroys/hides itself after one use.")]
    [SerializeField] private bool _isConsumable = true;

    [Tooltip("True for physical bumpers, False for passthrough triggers.")]
    [SerializeField] private bool _triggerOnCollision = true;

    private IPuckModifier[] _modifiers;

    private void Awake()
    {
        // Grab all attached modifiers
        _modifiers = GetComponents<IPuckModifier>();
    }

    // Used if this object is a solid physical obstacle
    private void OnCollisionEnter(Collision collision)
    {
        if (_triggerOnCollision && collision.gameObject.TryGetComponent<PuckMovementController>(out var puck))
        {
            ApplyAllMods(puck);
        }
    }

    // Used if this object is a pass-through trigger zone
    private void OnTriggerEnter(Collider other)
    {
        if (!_triggerOnCollision && other.TryGetComponent<PuckMovementController>(out var puck))
        {
            ApplyAllMods(puck);
        }
    }

    private void ApplyAllMods(PuckMovementController puck)
    {
        // Apply all modifiers
        foreach (var mod in _modifiers)
        {
            mod.ApplyModifier(puck);
            Debug.Log($"[Interactable] Applied Mod: {mod.ModName}");
        }

        if (_isConsumable)
        {
            // SetActive(false) instead of Destroy() until prototype grows in scope
            gameObject.SetActive(false);
        }
    }
}
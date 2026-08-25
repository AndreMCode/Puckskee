public interface IPuckModifier
{
    // Name for potential floating UI labels
    string ModName { get; }

    void ApplyModifier(PuckMovementController puck);
}
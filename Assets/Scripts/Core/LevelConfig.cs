using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "Puckskee/Level Configuration")]
public class LevelConfig : ScriptableObject
{
    [Header("Level Identification")]
    public int StageNumber = 1;
    public string LevelName = "Map 1";
    [TextArea] public string LevelDescription = "A simple introductory map with basic bumpers.";

    [Header("Easy Mode (Max Turns)")]
    [Tooltip("Maximum turns allowed to earn Gold")]
    public int GoldTurns = 7;
    public int SilverTurns = 10;
    public int BronzeTurns = 15;
    [Tooltip("If the player exceeds this turn count, they automatically lose.")]
    public int MaxTurns = 30;

    [Header("Hard Mode (Max Distance)")]
    [Tooltip("Maximum distance units allowed to earn Gold")]
    public float GoldDistance = 500f;
    public float SilverDistance = 750f;
    public float BronzeDistance = 1000f;
    [Tooltip("If the player exceeds this distance, they automatically lose.")]
    public float MaxDistance = 2000f;
}
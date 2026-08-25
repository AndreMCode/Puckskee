using System.Collections.Generic;

[System.Serializable]
public class StageRecord
{
    public int StageNumber;
    public int BestTurns;
    public float BestDistance;

    public StageRecord(int stage, int turns, float distance)
    {
        StageNumber = stage;
        BestTurns = turns;
        BestDistance = distance;
    }
}

[System.Serializable]
public class SaveData
{
    public List<StageRecord> Records = new List<StageRecord>();

    public float CameraSensX = 1.0f;
    public float CameraSensY = 1.0f;
    public float ZoomSens = 1.0f;
    public float SpinSens = 1.0f;
}
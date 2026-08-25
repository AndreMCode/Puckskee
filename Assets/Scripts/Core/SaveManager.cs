using UnityEngine;
using System.IO;
using System.Linq;

public static class SaveManager
{
    private static string SavePath => Application.persistentDataPath + "/puckskee_save.json";
    private static SaveData _currentData;

    public static float CameraSensX => _currentData?.CameraSensX ?? 1.0f;
    public static float CameraSensY => _currentData?.CameraSensY ?? 1.0f;
    public static float ZoomSens => _currentData?.ZoomSens ?? 1.0f;
    public static float SpinSens => _currentData?.SpinSens ?? 1.0f;

    // Load data from the hard drive into memory
    public static void Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            _currentData = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            _currentData = new SaveData();
        }
    }

    // Save memory data back to the hard drive
    private static void Save()
    {
        string json = JsonUtility.ToJson(_currentData, true);
        File.WriteAllText(SavePath, json);
    }

    // Retrieve a record for the UI
    public static StageRecord GetRecord(int stageNumber)
    {
        if (_currentData == null) Load();
        return _currentData.Records.FirstOrDefault(r => r.StageNumber == stageNumber);
    }

    // Update a record if the player beats their best score
    public static void UpdateRecord(int stageNumber, int turns, float distance)
    {
        if (_currentData == null) Load();

        StageRecord existingRecord = _currentData.Records.FirstOrDefault(r => r.StageNumber == stageNumber);

        if (existingRecord != null)
        {
            // Only overwrite if the new score is better
            if (turns < existingRecord.BestTurns) existingRecord.BestTurns = turns;
            if (distance < existingRecord.BestDistance) existingRecord.BestDistance = distance;
        }
        else
        {
            // First time beating this stage
            _currentData.Records.Add(new StageRecord(stageNumber, turns, distance));
        }

        Save();
    }

    public static void SetCameraSensX(float value)
    {
        if (_currentData == null) Load();
        _currentData.CameraSensX = value;
        Save();
    }

    public static void SetCameraSensY(float value)
    {
        if (_currentData == null) Load();
        _currentData.CameraSensY = value;
        Save();
    }

    public static void SetZoomSens(float value)
    {
        if (_currentData == null) Load();
        _currentData.ZoomSens = value;
        Save();
    }

    public static void SetSpinSens(float value)
    {
        if (_currentData == null) Load();
        _currentData.SpinSens = value;
        Save();
    }

    public static void ClearAllData()
    {
        _currentData = new SaveData();
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
        Debug.Log("[SaveManager] Local save file permanently deleted.");
    }
}
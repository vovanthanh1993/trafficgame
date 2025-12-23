using System.IO;
using UnityEngine;

public static class PlayerDataStorage
{
    private const string FileName = "playerdata.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static PlayerData LoadOrCreateDefault(int levelCount = 50)
    {
        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                if (!string.IsNullOrEmpty(json))
                {
                    PlayerData data = JsonUtility.FromJson<PlayerData>(json);
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.LogWarning($"PlayerDataStorage: Failed to read data file. {ex.Message}");
            }
        }

        PlayerData defaultData = PlayerData.CreateDefault(levelCount);
        Save(defaultData);
        return defaultData;
    }

    public static void Save(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogWarning("PlayerDataStorage.Save called with null data.");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
        }
        catch (IOException ex)
        {
            Debug.LogError($"PlayerDataStorage: Failed to save data. {ex.Message}");
        }
    }
}


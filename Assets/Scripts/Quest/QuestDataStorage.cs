using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Class để lưu trữ và load QuestData từ JSON
/// </summary>
public static class QuestDataStorage
{
    private const string QuestFileName = "quests.json";
    private static string QuestFilePath => Path.Combine(Application.persistentDataPath, QuestFileName);
    
    /// <summary>
    /// Load tất cả quest từ JSON file
    /// </summary>
    public static Dictionary<int, QuestData> LoadAllQuests()
    {
        Dictionary<int, QuestData> quests = new Dictionary<int, QuestData>();
        
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file {QuestFilePath}!");
            return quests;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            if (!string.IsNullOrEmpty(json))
            {
                QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
                if (questList != null && questList.quests != null)
                {
                    foreach (var questJson in questList.quests)
                    {
                        QuestData questData = questJson.ToQuestData();
                        if (questData != null)
                        {
                            quests[questData.questId] = questData;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load quest từ JSON: {ex.Message}");
        }
        
        return quests;
    }
    
    /// <summary>
    /// Load một quest cụ thể theo ID
    /// </summary>
    public static QuestData LoadQuest(int questId)
    {
        Dictionary<int, QuestData> allQuests = LoadAllQuests();
        if (allQuests.ContainsKey(questId))
        {
            return allQuests[questId];
        }
        
        Debug.LogWarning($"QuestDataStorage: Không tìm thấy quest với ID: {questId}");
        return null;
    }
    
    /// <summary>
    /// Lưu tất cả quest vào JSON file
    /// </summary>
    public static void SaveAllQuests(Dictionary<int, QuestData> quests)
    {
        if (quests == null || quests.Count == 0)
        {
            Debug.LogWarning("QuestDataStorage: Không có quest nào để lưu!");
            return;
        }
        
        try
        {
            QuestDataList questList = new QuestDataList();
            questList.quests = new List<QuestDataJSON>();
            
            foreach (var quest in quests.Values)
            {
                questList.quests.Add(new QuestDataJSON(quest));
            }
            
            string json = JsonUtility.ToJson(questList, true);
            File.WriteAllText(QuestFilePath, json);
            Debug.Log($"QuestDataStorage: Đã lưu {quests.Count} quest vào {QuestFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi lưu quest vào JSON: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Lưu một quest cụ thể
    /// </summary>
    public static void SaveQuest(QuestData questData)
    {
        if (questData == null)
        {
            Debug.LogWarning("QuestDataStorage: QuestData là null!");
            return;
        }
        
        Dictionary<int, QuestData> allQuests = LoadAllQuests();
        allQuests[questData.questId] = questData;
        SaveAllQuests(allQuests);
    }
    
    /// <summary>
    /// Lưu kết quả sao cho một quest và unlock quest tiếp theo
    /// </summary>
    public static void SaveQuestStars(int questId, int stars)
    {
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để lưu stars cho quest {questId}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        // Chỉ cập nhật nếu số sao mới cao hơn
                        if (stars > questJson.stars)
                        {
                            questJson.stars = stars;
                            updated = true;
                            Debug.Log($"QuestDataStorage: Đã lưu {stars} sao cho quest {questId}");
                        }
                    }
                    
                    // Unlock quest tiếp theo nếu quest hiện tại đã hoàn thành
                    if (questJson.questId == questId + 1 && questJson.isLocked)
                    {
                        questJson.isLocked = false;
                        updated = true;
                        Debug.Log($"QuestDataStorage: Đã unlock quest {questId + 1}");
                    }
                }
                
                if (updated)
                {
                    // Lưu lại file
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    File.WriteAllText(QuestFilePath, updatedJson);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi lưu stars: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Lấy kết quả sao của một quest
    /// </summary>
    public static int GetQuestStars(int questId)
    {
        if (!File.Exists(QuestFilePath))
        {
            return 0;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        return questJson.stars;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load stars từ JSON: {ex.Message}");
        }
        
        return 0;
    }
    
    /// <summary>
    /// Lấy trạng thái locked của một quest
    /// </summary>
    public static bool IsQuestLocked(int questId)
    {
        if (!File.Exists(QuestFilePath))
        {
            // Quest đầu tiên không locked, các quest khác locked mặc định
            return questId != 1;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        return questJson.isLocked;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load isLocked từ JSON: {ex.Message}");
        }
        
        // Fallback: Quest đầu tiên không locked, các quest khác locked
        return questId != 1;
    }
    
    /// <summary>
    /// Unlock một quest
    /// </summary>
    public static void UnlockQuest(int questId)
    {
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để unlock quest {questId}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        if (questJson.isLocked)
                        {
                            questJson.isLocked = false;
                            Debug.Log($"QuestDataStorage: Đã unlock quest {questId}");
                            
                            // Lưu lại file
                            string updatedJson = JsonUtility.ToJson(questList, true);
                            File.WriteAllText(QuestFilePath, updatedJson);
                        }
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi unlock quest: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Unlock tất cả các quest (dùng cho cheat code F1)
    /// </summary>
    public static void UnlockAllQuests()
    {
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để unlock tất cả quest!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                int unlockedCount = 0;
                
                foreach (var questJson in questList.quests)
                {
                    if (questJson.isLocked)
                    {
                        questJson.isLocked = false;
                        updated = true;
                        unlockedCount++;
                    }
                }
                
                if (updated)
                {
                    // Lưu lại file
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    File.WriteAllText(QuestFilePath, updatedJson);
                    Debug.Log($"QuestDataStorage: Đã unlock {unlockedCount} quest!");
                }
                else
                {
                    Debug.Log("QuestDataStorage: Tất cả quest đã được unlock rồi!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi unlock tất cả quest: {ex.Message}");
        }
    }
}

/// <summary>
/// Class JSON serializable cho QuestData
/// </summary>
[Serializable]
public class QuestDataJSON
{
    public int questId;
    public QuestObjective[] objectives;
    public float timeFor3Stars;
    public float timeFor2Stars;
    public int[] rewardList;
    public int stars = 0; // Kết quả sao đạt được (0 = chưa hoàn thành, 1-3 = số sao)
    public bool isLocked = true; // Trạng thái locked (true = bị khóa, false = đã unlock)
    
    public QuestDataJSON() { }
    
    public QuestDataJSON(QuestData questData)
    {
        if (questData == null) return;
        
        questId = questData.questId;
        objectives = questData.objectives;
        timeFor3Stars = questData.timeFor3Stars;
        timeFor2Stars = questData.timeFor2Stars;
        rewardList = questData.rewardList != null ? questData.rewardList.ToArray() : new int[] { 50, 100, 150 };
        stars = 0; // Mặc định chưa có sao
        isLocked = questId != 1; // Quest đầu tiên không locked, các quest khác locked mặc định
    }
    
    public QuestData ToQuestData()
    {
        QuestData questData = ScriptableObject.CreateInstance<QuestData>();
        questData.questId = questId;
        questData.objectives = objectives;
        questData.timeFor3Stars = timeFor3Stars;
        questData.timeFor2Stars = timeFor2Stars;
        questData.rewardList = rewardList != null ? new List<int>(rewardList) : new List<int> { 50, 100, 150 };
        return questData;
    }
}

/// <summary>
/// Wrapper class để serialize list quest
/// </summary>
[Serializable]
public class QuestDataList
{
    public List<QuestDataJSON> quests;
}


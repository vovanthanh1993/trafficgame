using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class StartPanel : MonoBehaviour
{
    [Header("Panel UI")]
    public TextMeshProUGUI levelTitle;
    public TextMeshProUGUI description;
    public TextMeshProUGUI star2TimeText;
    public TextMeshProUGUI star3TimeText;

    private PlayerLevelData currentLevelData;

    public void ShowForLevel(PlayerLevelData levelData)
    {
        currentLevelData = levelData;

        gameObject.SetActive(true);
        levelTitle.text = "Level " + levelData.level;

        // Load và hiển thị description từ QuestData đầu tiên (Quest1) mặc định
        // Quest index = level number (Level 1 -> Quest1, Level 2 -> Quest2...)
        LoadQuestDescription(levelData.level);

    }
    
    /// <summary>
    /// Load QuestData từ JSON dựa trên quest index và hiển thị description
    /// </summary>
    private void LoadQuestDescription(int questIndex)
    {
        // Load QuestData từ JSON
        QuestData questData = QuestDataStorage.LoadQuest(questIndex);
        
        if (questData == null)
        {
            Debug.LogWarning($"Không tìm thấy QuestData cho level {questIndex} trong JSON. Hãy đảm bảo quest đã được lưu vào file JSON!");
            return;
        }
        
        // Tạo description từ QuestObjective
        if (description != null)
        {
            string generatedDescription = GenerateDescriptionFromObjectives(questData.objectives);
            if (!string.IsNullOrEmpty(generatedDescription))
            {
                description.text = generatedDescription;
            }
        }
        
        // Hiển thị thời gian để đạt 2 sao
        if (star2TimeText != null)
        {
            string timeFormatted = FormatTime(questData.timeFor2Stars);
            star2TimeText.text = $"Complete quest in {timeFormatted}";
        }
        
        // Hiển thị thời gian để đạt 3 sao (nếu có)
        if (star3TimeText != null)
        {
            string timeFormatted = FormatTime(questData.timeFor3Stars);
            star3TimeText.text = $"Complete quest in {timeFormatted}";
        }
    }
    
    /// <summary>
    /// Tạo description từ QuestObjective array
    /// </summary>
    private string GenerateDescriptionFromObjectives(QuestObjective[] objectives)
    {
        if (objectives == null || objectives.Length == 0)
            return "";

        // Format gem collection objectives
        return FormatGemObjectives(new List<QuestObjective>(objectives));
    }
    
    /// <summary>
    /// Format các animal objectives thành "Collect 1 deer, 4 fox"
    /// </summary>
    private string FormatGemObjectives(List<QuestObjective> objectives)
    {
        if (objectives == null || objectives.Count == 0)
            return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Collect ");
        
        for (int i = 0; i < objectives.Count; i++)
        {
            QuestObjective obj = objectives[i];
            string itemName = obj.itemType.ToString().ToLower();
            
            if (i > 0)
            {
                sb.Append(", ");
            }
            
            sb.Append($"{obj.requiredAmount} {itemName}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Format các CollectItem objectives
    /// </summary>
    private string FormatCollectItemObjectives(List<QuestObjective> objectives)
    {
        if (objectives == null || objectives.Count == 0)
            return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Collect ");
        
        for (int i = 0; i < objectives.Count; i++)
        {
            QuestObjective obj = objectives[i];
            
            if (i > 0)
            {
                sb.Append(" , ");
            }
            
            if (obj.requiredAmount == 1)
            {
                sb.Append("1 item");
            }
            else
            {
                sb.Append($"{obj.requiredAmount} items");
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Format thời gian từ giây sang định dạng "min:ss"
    /// </summary>
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0}:{1:00}", minutes, secs);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnStartButtonClicked()
    {
        // Lưu level hiện tại vào PlayerPrefs để các script khác có thể lấy
        PlayerPrefs.SetInt("CurrentLevel", currentLevelData.level);
        PlayerPrefs.Save();
        
        // Xác định scene dựa trên level:
        // Level 1-5: GamePlay1
        // Level 6-10: GamePlay2
        // Level 11-15: GamePlay3
        // Level 16-20: GamePlay1 (quay lại)
        // Pattern lặp lại mỗi 15 level
        string sceneName = GetSceneNameForLevel(currentLevelData.level);
        GameCommonUtils.LoadScene(sceneName);
        UIManager.Instance.ShowGamePlayPanel(true);
        gameObject.SetActive(false);
        UIManager.Instance.ShowHomePanel(false);
        UIManager.Instance.ShowSelectLevelPanel(false);
    }
    
    /// <summary>
    /// Xác định tên scene dựa trên level
    /// </summary>
    private string GetSceneNameForLevel(int level)
    {
        // Tính vị trí trong chu kỳ 15 level (0-14)
        int positionInCycle = (level - 1) % 15;
        
        // Level 1-5 (position 0-4): GamePlay1
        // Level 6-10 (position 5-9): GamePlay2
        // Level 11-15 (position 10-14): GamePlay3
        if (positionInCycle < 5)
        {
            return "GamePlay1";
        }
        else if (positionInCycle < 10)
        {
            return "GamePlay2";
        }
        else
        {
            return "GamePlay3";
        }
    }
}

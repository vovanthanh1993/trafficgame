using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Component để hiển thị một objective item
/// </summary>
public class ObjectiveItem : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text hiển thị format: Tên item: 0/1")]
    public TextMeshProUGUI objectiveText;
    
    private QuestObjective objective;
    private int currentProgress = 0;
    private bool isCompleted = false;
    
    /// <summary>
    /// Khởi tạo objective item với objective data
    /// </summary>
    public void Initialize(QuestObjective obj)
    {
        objective = obj;
        currentProgress = 0;
        isCompleted = false;
        
        UpdateUI();
    }
    
    /// <summary>
    /// Cập nhật progress từ QuestManager
    /// </summary>
    public void UpdateProgress()
    {
        if (objective == null)
            return;
        
        // Nếu đã hoàn thành thì không update nữa
        if (isCompleted)
            return;
        
        // Update item progress từ QuestManager
        if (QuestManager.Instance != null && QuestManager.Instance.progress != null)
        {
            if (QuestManager.Instance.progress.ContainsKey(objective.itemType))
            {
                currentProgress = QuestManager.Instance.progress[objective.itemType];
            }
            else
            {
                currentProgress = 0;
            }
        }
        else
        {
            currentProgress = 0;
        }
        
        // Kiểm tra và đánh dấu hoàn thành
        if (currentProgress >= objective.requiredAmount)
        {
            isCompleted = true;
            currentProgress = objective.requiredAmount; // Đảm bảo không vượt quá
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Cập nhật UI hiển thị
    /// </summary>
    private void UpdateUI()
    {
        if (objective == null || objectiveText == null)
            return;
        
        string displayName = objective.itemType.ToString();
        string text;
        
        if (isCompleted)
        {
            // Khi hoàn thành: chỉ hiển thị requiredAmount/requiredAmount, không hiển thị số lớn hơn
            text = $"{displayName}: {objective.requiredAmount}/{objective.requiredAmount}";
            // Gạch ngang ở giữa và màu #796847
            objectiveText.text = $"<s><color=#796847>{text}</color></s>";
        }
        else
        {
            // Chưa hoàn thành: hiển thị progress bình thường
            text = $"{displayName}: {currentProgress}/{objective.requiredAmount}";
            objectiveText.text = text;
        }
    }
}


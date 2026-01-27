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
    /// Hiển thị: progress (đã thả tại checkpoint) + số lượng đang mang
    /// Chỉ gạch ngang khi progress (đã thả) >= requiredAmount
    /// </summary>
    public void UpdateProgress()
    {
        if (objective == null)
        {
            Debug.LogWarning("ObjectiveItem: objective là null!");
            return;
        }
        
        // Lấy progress từ QuestManager
        int progressValue = 0;
        if (QuestManager.Instance != null && QuestManager.Instance.progress != null)
        {
            if (QuestManager.Instance.progress.ContainsKey(objective.itemType))
            {
                progressValue = QuestManager.Instance.progress[objective.itemType];
            }
        }
        
        // Đếm số lượng item đang mang từ PlayerController
        int carriedCount = 0;
        if (PlayerController.Instance != null)
        {
            var carriedItems = PlayerController.Instance.GetCarriedItems();
            if (carriedItems != null)
            {
                foreach (var item in carriedItems)
                {
                    if (item != null && item.ItemType == objective.itemType)
                    {
                        carriedCount++;
                    }
                }
            }
        }
        
        // Hiển thị: progress (đã thả tại checkpoint) + số lượng đang mang
        int oldProgress = currentProgress;
        currentProgress = progressValue + carriedCount;
        Debug.Log($"ObjectiveItem: {objective.itemType} progress = {progressValue} (đã thả), đang mang = {carriedCount}, tổng hiển thị = {currentProgress}");
        
        // Chỉ gạch ngang khi đã thả tại checkpoint (progressValue > 0) và progressValue >= requiredAmount
        // KHÔNG tính số lượng đang mang vào việc check completed
        if (progressValue > 0 && progressValue >= objective.requiredAmount)
        {
            isCompleted = true;
            // KHÔNG giới hạn currentProgress, cho phép vượt quá requiredAmount
        }
        else
        {
            // Nếu progress giảm xuống dưới requiredAmount, reset trạng thái completed
            isCompleted = false;
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Cập nhật UI hiển thị
    /// </summary>
    private void UpdateUI()
    {
        if (objective == null || objectiveText == null)
        {
            Debug.LogWarning("ObjectiveItem: objective hoặc objectiveText là null!");
            return;
        }
        
        string displayName = objective.itemType.ToString();
        string text;
        
        if (isCompleted)
        {
            // Khi hoàn thành: hiển thị progress thực tế (có thể vượt quá requiredAmount)
            text = $"{displayName}: {currentProgress}/{objective.requiredAmount}";
            // Gạch ngang ở giữa và màu #796847
            objectiveText.text = $"<s><color=#796847>{text}</color></s>";
            Debug.Log($"ObjectiveItem: UI cập nhật - {text} (completed, có thể vượt quá)");
        }
        else
        {
            // Chưa hoàn thành: hiển thị progress bình thường
            text = $"{displayName}: {currentProgress}/{objective.requiredAmount}";
            objectiveText.text = text;
            Debug.Log($"ObjectiveItem: UI cập nhật - {text} (not completed)");
        }
    }
}


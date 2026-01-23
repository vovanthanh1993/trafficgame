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
        {
            Debug.LogWarning("ObjectiveItem: objective là null!");
            return;
        }
        
        // Update item progress từ QuestManager (luôn cập nhật, không check isCompleted)
        if (QuestManager.Instance != null && QuestManager.Instance.progress != null)
        {
            if (QuestManager.Instance.progress.ContainsKey(objective.itemType))
            {
                int oldProgress = currentProgress;
                currentProgress = QuestManager.Instance.progress[objective.itemType];
                Debug.Log($"ObjectiveItem: {objective.itemType} progress cập nhật từ {oldProgress} -> {currentProgress}");
            }
            else
            {
                Debug.LogWarning($"ObjectiveItem: Không tìm thấy progress key cho {objective.itemType}");
                currentProgress = 0;
            }
        }
        else
        {
            Debug.LogWarning("ObjectiveItem: QuestManager.Instance hoặc progress là null!");
            currentProgress = 0;
        }
        
        // Kiểm tra và đánh dấu hoàn thành (hoặc reset nếu progress giảm)
        if (currentProgress >= objective.requiredAmount)
        {
            isCompleted = true;
            currentProgress = objective.requiredAmount; // Đảm bảo không vượt quá
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
            // Khi hoàn thành: chỉ hiển thị requiredAmount/requiredAmount, không hiển thị số lớn hơn
            text = $"{displayName}: {objective.requiredAmount}/{objective.requiredAmount}";
            objectiveText.text = text;
            Debug.Log($"ObjectiveItem: UI cập nhật - {text} (completed)");
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


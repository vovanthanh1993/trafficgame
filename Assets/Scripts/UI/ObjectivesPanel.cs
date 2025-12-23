using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel để hiển thị các objectives của quest
/// </summary>
public class ObjectivesPanel : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Prefab để hiển thị mỗi objective")]
    [SerializeField] private GameObject objectivePrefab;
    
    [Tooltip("Parent để chứa các objective (thường là Vertical Layout Group hoặc Grid Layout Group)")]
    [SerializeField] private Transform objectivesContainer;
    
    private List<ObjectiveItem> objectiveItems = new List<ObjectiveItem>();
    
    void Start()
    {
        if (objectivesContainer == null)
        {
            objectivesContainer = transform;
        }
        
        // Đăng ký để cập nhật khi quest thay đổi
        if (QuestManager.Instance != null)
        {
            InitializeObjectives();
        }
    }
    
    void OnEnable()
    {
        // Refresh objectives khi panel được bật
        if (QuestManager.Instance != null && QuestManager.Instance.currentQuest != null)
        {
            RefreshObjectives();
        }
    }
    
    /// <summary>
    /// Khởi tạo objectives từ quest hiện tại
    /// </summary>
    public void InitializeObjectives()
    {
        ClearObjectives();
        
        if (QuestManager.Instance == null || QuestManager.Instance.currentQuest == null)
        {
            Debug.LogWarning("ObjectivesPanel: QuestManager hoặc currentQuest là null!");
            return;
        }
        
        if (objectivePrefab == null)
        {
            Debug.LogError("ObjectivesPanel: objectivePrefab không được set!");
            return;
        }
        
        QuestData quest = QuestManager.Instance.currentQuest;
        
        if (quest.objectives == null || quest.objectives.Length == 0)
        {
            Debug.LogWarning("ObjectivesPanel: Quest không có objectives!");
            return;
        }
        
        // Tạo objective item cho mỗi objective
        foreach (var objective in quest.objectives)
        {
            CreateObjectiveItem(objective);
        }
        
        // Cập nhật progress ban đầu
        UpdateProgress();
    }
    
    /// <summary>
    /// Tạo một objective item
    /// </summary>
    private void CreateObjectiveItem(QuestObjective objective)
    {
        GameObject objInstance = Instantiate(objectivePrefab, objectivesContainer);
        ObjectiveItem item = objInstance.GetComponent<ObjectiveItem>();
        
        if (item == null)
        {
            item = objInstance.AddComponent<ObjectiveItem>();
        }
        
        item.Initialize(objective);
        objectiveItems.Add(item);
    }
    
    /// <summary>
    /// Refresh objectives (xóa cũ và tạo lại)
    /// </summary>
    public void RefreshObjectives()
    {
        InitializeObjectives();
    }
    
    /// <summary>
    /// Cập nhật progress của tất cả objectives
    /// </summary>
    public void UpdateProgress()
    {
        if (QuestManager.Instance == null)
            return;
        
        foreach (var item in objectiveItems)
        {
            if (item != null)
            {
                // Update animal progress
                item.UpdateProgress();
            }
        }
    }
    
    /// <summary>
    /// Xóa tất cả objectives
    /// </summary>
    private void ClearObjectives()
    {
        foreach (var item in objectiveItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        objectiveItems.Clear();
    }
    
    void OnDestroy()
    {
        ClearObjectives();
    }
}


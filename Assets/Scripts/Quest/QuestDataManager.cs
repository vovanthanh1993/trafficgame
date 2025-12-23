using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager để quản lý QuestData, tự động tạo 50 quest mặc định nếu chưa có
/// </summary>
public class QuestDataManager : MonoBehaviour
{
    public static QuestDataManager Instance { get; private set; }

    private Dictionary<int, QuestData> questsCache = new Dictionary<int, QuestData>();
    private const int DefaultQuestCount = 50;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadOrCreateQuests();
    }

    /// <summary>
    /// Load quest từ JSON hoặc tạo 50 quest mặc định nếu chưa có
    /// </summary>
    public void LoadOrCreateQuests()
    {
        questsCache = QuestDataStorage.LoadAllQuests();
        
        // Nếu chưa có quest nào, tạo 50 quest mặc định
        if (questsCache == null || questsCache.Count == 0)
        {
            Debug.Log($"QuestDataManager: Không tìm thấy quest nào, đang tạo {DefaultQuestCount} quest mặc định...");
            CreateDefaultQuests();
            questsCache = QuestDataStorage.LoadAllQuests();
        }
        
        Debug.Log($"QuestDataManager: Đã load {questsCache.Count} quest");
    }

    /// <summary>
    /// Tạo 50 quest mặc định
    /// </summary>
    private void CreateDefaultQuests()
    {
        Dictionary<int, QuestData> defaultQuests = new Dictionary<int, QuestData>();
        
        for (int i = 1; i <= DefaultQuestCount; i++)
        {
            QuestData questData = CreateDefaultQuest(i);
            defaultQuests[i] = questData;
        }
        
        // Lưu vào JSON
        QuestDataStorage.SaveAllQuests(defaultQuests);
        Debug.Log($"QuestDataManager: Đã tạo {DefaultQuestCount} quest mặc định");
    }

    /// <summary>
    /// Tạo một quest mặc định với ID cụ thể
    /// </summary>
    private QuestData CreateDefaultQuest(int questId)
    {
        QuestData questData = ScriptableObject.CreateInstance<QuestData>();
        questData.questId = questId;
        
        // Tạo objectives cho animal collection với độ khó tăng dần theo level
        List<QuestObjective> objectives = new List<QuestObjective>();
        
        // Số lượng objectives (loại animal khác nhau) - đảm bảo ít nhất 2 loại
        int objectivesCount = 2; // Mặc định ít nhất 2 loại
        if (questId == 1)
        {
            objectivesCount = 2; // Level 1: cố định 2 loại
        }
        else if (questId < 10)
        {
            objectivesCount = Random.Range(2, 4); // Level 2-9: 2-3 loại (ngẫu nhiên)
        }
        else if (questId < 20)
        {
            objectivesCount = Random.Range(3, 5); // Level 10-19: 3-4 loại (ngẫu nhiên)
        }
        else
        {
            objectivesCount = 4; // Level 20+: 4 loại (cố định)
        }
        
        // Xác định phạm vi ItemType dựa trên level (độ khó tăng dần)
        ItemType[] allItemTypes = (ItemType[])System.Enum.GetValues(typeof(ItemType));
        List<ItemType> availableItemTypes = new List<ItemType>();
        
        if (questId == 1)
        {
            // Level 1: Các item đơn giản (2-3 loại đầu tiên)
            availableItemTypes.Add(ItemType.Apple);
            availableItemTypes.Add(ItemType.Apricot);
            availableItemTypes.Add(ItemType.Avocado);
        }
        else if (questId <= 10)
        {
            // Level 2-10: Thêm các item phổ biến (4-5 loại đầu tiên)
            int maxIndex = Mathf.Min(4, allItemTypes.Length - 1);
            for (int i = 0; i <= maxIndex; i++)
            {
                availableItemTypes.Add(allItemTypes[i]);
            }
        }
        else if (questId <= 20)
        {
            // Level 11-20: Hầu hết các loại (5-6 loại)
            int maxIndex = Mathf.Min(5, allItemTypes.Length - 1);
            for (int i = 0; i <= maxIndex; i++)
            {
                availableItemTypes.Add(allItemTypes[i]);
            }
        }
        else
        {
            // Level 21+: Tất cả các loại item
            availableItemTypes.AddRange(allItemTypes);
        }
        
        // Xác định requiredAmount dựa trên level
        int minRequiredAmount = 1;
        int maxRequiredAmount = 3;
        
        if (questId <= 10)
        {
            minRequiredAmount = 1;
            maxRequiredAmount = 2;
        }
        else if (questId <= 20)
        {
            minRequiredAmount = 1;
            maxRequiredAmount = 3;
        }
        else if (questId <= 30)
        {
            minRequiredAmount = 2;
            maxRequiredAmount = 3;
        }
        else
        {
            minRequiredAmount = 2;
            maxRequiredAmount = 3; // Tối đa 3 con mỗi loại để đảm bảo 4 loại x 3 con = 12 con (không vượt quá 13)
        }
        
        // Đảm bảo có đủ loại item để chọn
        if (availableItemTypes.Count < objectivesCount)
        {
            Debug.LogWarning($"QuestDataManager: Quest {questId} chỉ có {availableItemTypes.Count} loại item khả dụng, nhưng cần {objectivesCount}. Giảm xuống {availableItemTypes.Count} loại.");
            objectivesCount = availableItemTypes.Count;
        }
        
        // Đảm bảo ít nhất 2 loại item
        if (objectivesCount < 2 && availableItemTypes.Count >= 2)
        {
            objectivesCount = 2;
        }
        
        // Tạo objectives ngẫu nhiên
        for (int i = 0; i < objectivesCount && availableItemTypes.Count > 0; i++)
        {
            // Chọn ngẫu nhiên một ItemType chưa dùng
            int randomIndex = Random.Range(0, availableItemTypes.Count);
            ItemType randomItemType = availableItemTypes[randomIndex];
            availableItemTypes.RemoveAt(randomIndex); // Xóa để không trùng lặp
            
            // requiredAmount ngẫu nhiên trong phạm vi minRequiredAmount đến maxRequiredAmount
            int requiredAmount = Random.Range(minRequiredAmount, maxRequiredAmount + 1);
            
            QuestObjective objective = new QuestObjective
            {
                itemType = randomItemType,
                requiredAmount = requiredAmount
            };
            objectives.Add(objective);
        }
        
        // Validation: Đảm bảo quest có ít nhất 2 loại item
        if (objectives.Count < 2)
        {
            Debug.LogError($"QuestDataManager: Quest {questId} chỉ có {objectives.Count} loại item, không đủ 2 loại! Vui lòng kiểm tra lại availableItemTypes.");
            // Nếu không đủ, thử thêm loại item nếu còn available
            while (objectives.Count < 2 && availableItemTypes.Count > 0)
            {
                int randomIndex = Random.Range(0, availableItemTypes.Count);
                ItemType randomItemType = availableItemTypes[randomIndex];
                availableItemTypes.RemoveAt(randomIndex);
                
                int requiredAmount = Random.Range(minRequiredAmount, maxRequiredAmount + 1);
                
                QuestObjective objective = new QuestObjective
                {
                    itemType = randomItemType,
                    requiredAmount = requiredAmount
                };
                objectives.Add(objective);
            }
        }
        
        questData.objectives = objectives.ToArray();
        
        // Thời gian mặc định tăng dần theo level (đã giảm)
        questData.timeFor3Stars = 60f + (questId - 1) * 2f; // 60s, 65s, 70s...
        questData.timeFor2Stars = questData.timeFor3Stars * 1.5f;
        
        // Reward mặc định tăng dần theo level (đã giảm)
        int baseReward = 10 + (questId - 1) * 2;
        questData.rewardList = new List<int>
        {
            baseReward,              // 1 sao
            Mathf.RoundToInt(baseReward * 1.5f),  // 2 sao
            baseReward * 2           // 3 sao
        };
        
        return questData;
    }

    /// <summary>
    /// Lấy quest theo ID từ cache
    /// </summary>
    public QuestData GetQuest(int questId)
    {
        if (questsCache.ContainsKey(questId))
        {
            return questsCache[questId];
        }
        
        // Nếu không có trong cache, thử load từ storage
        QuestData quest = QuestDataStorage.LoadQuest(questId);
        if (quest != null)
        {
            questsCache[questId] = quest;
        }
        
        return quest;
    }

    /// <summary>
    /// Lấy tất cả quest từ cache
    /// </summary>
    public Dictionary<int, QuestData> GetAllQuests()
    {
        return questsCache;
    }

    /// <summary>
    /// Lấy số lượng quest
    /// </summary>
    public int GetQuestCount()
    {
        return questsCache != null ? questsCache.Count : 0;
    }

    /// <summary>
    /// Lấy kết quả sao của một quest
    /// </summary>
    public int GetQuestStars(int questId)
    {
        return QuestDataStorage.GetQuestStars(questId);
    }

    /// <summary>
    /// Kiểm tra quest có bị locked không
    /// </summary>
    public bool IsQuestLocked(int questId)
    {
        return QuestDataStorage.IsQuestLocked(questId);
    }

    /// <summary>
    /// Refresh cache từ JSON
    /// </summary>
    public void Refresh()
    {
        questsCache = QuestDataStorage.LoadAllQuests();
        Debug.Log($"QuestDataManager: Đã refresh, có {questsCache.Count} quest");
    }
}


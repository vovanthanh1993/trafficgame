using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawner để spawn item theo số lượng trong quest objectives
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Tự động load prefabs từ Resources/Prefabs (nếu bật, sẽ bỏ qua itemPrefabs list)")]
    [SerializeField] private bool autoLoadFromResources = true;
    
    [Tooltip("Đường dẫn thư mục trong Resources chứa item prefabs (ví dụ: 'Prefabs' hoặc 'Prefabs/Items')")]
    [SerializeField] private string prefabsFolderPath = "Prefabs/Items";
    
    [Tooltip("Dictionary để map ItemType với prefab tương ứng (chỉ dùng khi autoLoadFromResources = false)")]
    [SerializeField] private List<ItemPrefabData> itemPrefabs = new List<ItemPrefabData>();
    
    [Header("Spawn Points")]
    [Tooltip("GameObject cha chứa tất cả các spawn points (sẽ tự động lấy tất cả các con)")]
    [SerializeField] private Transform spawnPointsParent;
    
    private List<Transform> spawnPoints = new List<Transform>();
    
    
    private Dictionary<ItemType, GameObject> itemPrefabDict = new Dictionary<ItemType, GameObject>();
    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<int> usedSpawnPointIndices = new List<int>(); // Lưu các spawn point đã dùng
    
    void Start()
    {
        InitializeSpawnPoints();
        InitializePrefabDictionary();
        // Delay spawning để đảm bảo QuestManager đã load quest
        StartCoroutine(WaitForQuestManagerAndSpawn());
    }
    
    /// <summary>
    /// Đợi QuestManager khởi tạo và load quest trước khi spawn items
    /// </summary>
    System.Collections.IEnumerator WaitForQuestManagerAndSpawn()
    {
        // Đợi một frame để đảm bảo tất cả Start() methods đã chạy
        yield return null;
        
        // Đợi cho đến khi QuestManager.Instance và currentQuest đã sẵn sàng
        int maxWaitFrames = 60; // Tối đa đợi 60 frames (khoảng 1 giây ở 60fps)
        int waitFrames = 0;
        
        while ((QuestManager.Instance == null || QuestManager.Instance.currentQuest == null) && waitFrames < maxWaitFrames)
        {
            yield return null;
            waitFrames++;
        }
        
        // Kiểm tra lại sau khi đợi
        if (QuestManager.Instance == null || QuestManager.Instance.currentQuest == null)
        {
            Debug.LogError("ItemSpawner: Không thể tìm thấy QuestManager hoặc currentQuest sau khi đợi!");
            yield break;
        }
        
        // Spawn items khi QuestManager đã sẵn sàng
        SpawnItemsFromQuest();
    }
    
    /// <summary>
    /// Khởi tạo danh sách spawn points từ các con của spawnPointsParent
    /// </summary>
    private void InitializeSpawnPoints()
    {
        spawnPoints.Clear();
        
        if (spawnPointsParent == null)
        {
            Debug.LogError("ItemSpawner: spawnPointsParent không được set! Vui lòng gán GameObject cha chứa spawn points.");
            return;
        }
        
        // Lấy tất cả các con của spawnPointsParent
        for (int i = 0; i < spawnPointsParent.childCount; i++)
        {
            Transform child = spawnPointsParent.GetChild(i);
            if (child != null)
            {
                spawnPoints.Add(child);
            }
        }
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"ItemSpawner: Không tìm thấy spawn point nào trong {spawnPointsParent.name}!");
        }
        else
        {
            Debug.Log($"ItemSpawner: Đã tìm thấy {spawnPoints.Count} spawn points");
        }
    }
    
    /// <summary>
    /// Khởi tạo dictionary từ list itemPrefabs hoặc load từ Resources
    /// </summary>
    private void InitializePrefabDictionary()
    {
        itemPrefabDict.Clear();
        
        if (autoLoadFromResources)
        {
            LoadPrefabsFromResources();
        }
        else
        {
            LoadPrefabsFromList();
        }
        
        Debug.Log($"ItemSpawner: Đã load {itemPrefabDict.Count} item prefabs");
    }
    
    /// <summary>
    /// Load prefabs từ Resources folder
    /// </summary>
    private void LoadPrefabsFromResources()
    {
        if (string.IsNullOrEmpty(prefabsFolderPath))
        {
            Debug.LogWarning("ItemSpawner: prefabsFolderPath không được set!");
            return;
        }
        
        // Load tất cả GameObject từ Resources/Prefabs
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(prefabsFolderPath);
        
        if (loadedPrefabs == null || loadedPrefabs.Length == 0)
        {
            Debug.LogWarning($"ItemSpawner: Không tìm thấy prefab nào trong Resources/{prefabsFolderPath}!");
            return;
        }
        
        // Map mỗi prefab với ItemType dựa trên tên hoặc Item component
        foreach (GameObject prefab in loadedPrefabs)
        {
            if (prefab == null) continue;
            
            ItemType itemType = GetItemTypeFromPrefab(prefab);
            
            if (itemPrefabDict.ContainsKey(itemType))
            {
                Debug.LogWarning($"ItemSpawner: Prefab cho {itemType} đã tồn tại, bỏ qua prefab: {prefab.name}");
                continue;
            }
            
            itemPrefabDict[itemType] = prefab;
            Debug.Log($"ItemSpawner: Đã load prefab '{prefab.name}' cho {itemType}");
        }
    }
    
    /// <summary>
    /// Load prefabs từ list itemPrefabs (manual)
    /// </summary>
    private void LoadPrefabsFromList()
    {
        foreach (var data in itemPrefabs)
        {
            if (data.prefab != null)
            {
                itemPrefabDict[data.itemType] = data.prefab;
            }
        }
    }
    
    /// <summary>
    /// Xác định ItemType từ prefab (dựa trên Item component hoặc tên file)
    /// </summary>
    private ItemType GetItemTypeFromPrefab(GameObject prefab)
    {
        // Thử lấy từ Item component trước
        Item item = prefab.GetComponent<Item>();
        if (item != null)
        {
            return item.ItemType;
        }
        
        // Nếu không có component, thử parse từ tên file
        string prefabName = prefab.name;
        
        // Loại bỏ các ký tự đặc biệt và chuyển về PascalCase
        prefabName = prefabName.Replace("_", "").Replace("-", "").Trim();
        
        // Thử parse tên thành ItemType enum
        if (System.Enum.TryParse<ItemType>(prefabName, true, out ItemType parsedType))
        {
            return parsedType;
        }
        
        // Nếu không parse được, thử tìm match không phân biệt hoa thường
        ItemType[] allTypes = (ItemType[])System.Enum.GetValues(typeof(ItemType));
        foreach (ItemType type in allTypes)
        {
            if (prefabName.Equals(type.ToString(), System.StringComparison.OrdinalIgnoreCase))
            {
                return type;
            }
        }
        
        // Fallback: nếu không tìm thấy, dùng Apple làm mặc định và cảnh báo
        Debug.LogWarning($"ItemSpawner: Không thể xác định ItemType cho prefab '{prefab.name}', sử dụng Apple làm mặc định");
        return ItemType.Apple;
    }
    
    /// <summary>
    /// Spawn items dựa trên quest objectives
    /// </summary>
    public void SpawnItemsFromQuest()
    {
        if (QuestManager.Instance == null || QuestManager.Instance.currentQuest == null)
        {
            Debug.LogError("ItemSpawner: QuestManager hoặc currentQuest là null!");
            return;
        }
        
        QuestData quest = QuestManager.Instance.currentQuest;
        
        if (quest.objectives == null || quest.objectives.Length == 0)
        {
            Debug.LogWarning("ItemSpawner: Quest không có objectives!");
            return;
        }
        
        // Reset danh sách spawn points đã dùng
        usedSpawnPointIndices.Clear();
        spawnedItems.Clear();
        
        // Tính tổng requiredAmount của tất cả loại item
        int totalRequiredAmount = 0;
        foreach (var objective in quest.objectives)
        {
            totalRequiredAmount += objective.requiredAmount;
        }
        
        // Bước 1: Ưu tiên spawn đủ requiredAmount cho TẤT CẢ các loại item trước
        foreach (var objective in quest.objectives)
        {
            SpawnRequiredAmountForObjective(objective);
        }
        
        // Bước 2: Nếu tổng requiredAmount < 6, spawn thêm 1 con cho mỗi loại (nếu còn chỗ)
        if (totalRequiredAmount < 6)
        {
            foreach (var objective in quest.objectives)
            {
                SpawnBonusItemForObjective(objective);
            }
        }
        
        Debug.Log($"ItemSpawner: Đã spawn {spawnedItems.Count} items. Tổng requiredAmount: {totalRequiredAmount}");
    }
    
    /// <summary>
    /// Spawn đủ requiredAmount cho một objective (ưu tiên cao nhất)
    /// </summary>
    private void SpawnRequiredAmountForObjective(QuestObjective objective)
    {
        if (!itemPrefabDict.ContainsKey(objective.itemType))
        {
            Debug.LogWarning($"ItemSpawner: Không tìm thấy prefab cho {objective.itemType}!");
            return;
        }
        
        GameObject prefab = itemPrefabDict[objective.itemType];
        int requiredAmount = objective.requiredAmount;
        int actuallySpawned = 0;
        
        // Spawn đủ requiredAmount
        // Nếu không còn spawn point trống thì ngừng spawn
        for (int i = 0; i < requiredAmount; i++)
        {
            int spawnIndex = GetUnusedSpawnPointIndex();
            if (spawnIndex == -1)
            {
                // Không còn spawn point trống, ngừng spawn
                Debug.LogWarning($"ItemSpawner: Không còn spawn point trống để spawn {objective.itemType}! Cần {requiredAmount} con nhưng chỉ spawn được {actuallySpawned} con.");
                break;
            }
            
            Vector3 spawnPosition = spawnPoints[spawnIndex].position;
            // Set rotation y = 180 khi spawn
            Quaternion spawnRotation = Quaternion.Euler(0, 180, 0);
            GameObject item = Instantiate(prefab, spawnPosition, spawnRotation);
            
            // Đảm bảo item có Item component với đúng itemType
            Item itemComponent = item.GetComponent<Item>();
            if (itemComponent == null)
            {
                // Nếu prefab chưa có Item, thêm vào
                itemComponent = item.AddComponent<Item>();
            }
            // Set itemType cho item
            itemComponent.SetItemType(objective.itemType);
            
            spawnedItems.Add(item);
            usedSpawnPointIndices.Add(spawnIndex);
            actuallySpawned++;
        }
        
        // Kiểm tra xem đã spawn đủ requiredAmount chưa
        if (actuallySpawned < requiredAmount)
        {
            Debug.LogError($"ItemSpawner: Không thể spawn đủ {requiredAmount} {objective.itemType}! Chỉ spawn được {actuallySpawned} con. Thiếu {requiredAmount - actuallySpawned} con.");
        }
        else
        {
            Debug.Log($"ItemSpawner: Đã spawn đủ {actuallySpawned} {objective.itemType} (yêu cầu: {requiredAmount})");
        }
    }
    
    /// <summary>
    /// Spawn thêm 1 con bonus cho một objective (chỉ khi tổng requiredAmount < 6)
    /// </summary>
    private void SpawnBonusItemForObjective(QuestObjective objective)
    {
        if (!itemPrefabDict.ContainsKey(objective.itemType))
        {
            return;
        }
        
        GameObject prefab = itemPrefabDict[objective.itemType];
        
        // Spawn thêm 1 con nếu còn chỗ
        int spawnIndex = GetUnusedSpawnPointIndex();
        if (spawnIndex != -1)
        {
            Vector3 spawnPosition = spawnPoints[spawnIndex].position;
            Quaternion spawnRotation = Quaternion.Euler(0, 180, 0);
            GameObject item = Instantiate(prefab, spawnPosition, spawnRotation);
            
            Item itemComponent = item.GetComponent<Item>();
            if (itemComponent == null)
            {
                itemComponent = item.AddComponent<Item>();
            }
            itemComponent.SetItemType(objective.itemType);
            
            spawnedItems.Add(item);
            usedSpawnPointIndices.Add(spawnIndex);
            
            Debug.Log($"ItemSpawner: Đã spawn thêm 1 {objective.itemType} (bonus)");
        }
        else
        {
            Debug.Log($"ItemSpawner: Không còn chỗ để spawn thêm 1 {objective.itemType} (bonus)");
        }
    }
    
    /// <summary>
    /// Lấy index của một spawn point chưa được sử dụng
    /// </summary>
    private int GetUnusedSpawnPointIndex()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return -1;
        }
        
        // Tạo danh sách các index chưa dùng
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (!usedSpawnPointIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }
        
        if (availableIndices.Count == 0)
        {
            return -1; // Không còn spawn point trống
        }
        
        // Chọn ngẫu nhiên một index từ danh sách chưa dùng
        int randomIndex = Random.Range(0, availableIndices.Count);
        return availableIndices[randomIndex];
    }
    
    
    /// <summary>
    /// Xóa tất cả items đã spawn
    /// </summary>
    public void ClearSpawnedItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();
        usedSpawnPointIndices.Clear();
    }
    
    /// <summary>
    /// Respawn items từ quest (xóa cũ và spawn lại)
    /// </summary>
    public void RespawnItems()
    {
        ClearSpawnedItems();
        SpawnItemsFromQuest();
    }
}

/// <summary>
/// Class để map ItemType với prefab
/// </summary>
[System.Serializable]
public class ItemPrefabData
{
    public ItemType itemType;
    public GameObject prefab;
}


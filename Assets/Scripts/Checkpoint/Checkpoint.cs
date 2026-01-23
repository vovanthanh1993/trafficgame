using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script cho checkpoint - nơi player thả item để tính điểm
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Parent chứa danh sách các điểm drop item")]
    [SerializeField] private Transform parentDropPoint;
    
    [Tooltip("Effect khi thả item thành công")]
    [SerializeField] private GameObject dropEffect;
    
    [Header("Tier Settings")]
    [Tooltip("Tên của GameObject chứa tầng 1 (ví dụ: 'Tier1', 'Floor1')")]
    [SerializeField] private string tier1Name = "Tier1";
    
    [Tooltip("Tên của GameObject chứa tầng 2 (ví dụ: 'Tier2', 'Floor2')")]
    [SerializeField] private string tier2Name = "Tier2";
    
    // Danh sách các điểm drop tầng 1
    private List<Transform> tier1Positions = new List<Transform>();
    
    // Danh sách các điểm drop tầng 2
    private List<Transform> tier2Positions = new List<Transform>();
    
    // Danh sách các điểm drop đã được sử dụng ở tầng 1
    private List<Transform> usedTier1Positions = new List<Transform>();
    
    // Danh sách các điểm drop đã được sử dụng ở tầng 2
    private List<Transform> usedTier2Positions = new List<Transform>();
    
    // Tầng hiện tại đang sử dụng (1 hoặc 2)
    private int currentTier = 1;
    
    private void Start()
    {
        // Tự động tìm parentDropPoint nếu chưa được assign
        if (parentDropPoint == null)
        {
            parentDropPoint = transform.Find("ParentDropPoint");
            if (parentDropPoint == null)
            {
                // Nếu không tìm thấy, tìm với tên khác
                parentDropPoint = transform.Find("DropArea");
                if (parentDropPoint == null)
                {
                    Debug.LogWarning($"Checkpoint: Không tìm thấy ParentDropPoint trong {gameObject.name}. Vui lòng assign hoặc tạo GameObject cha chứa các điểm drop.");
                }
            }
        }
        
        // Load các tầng drop positions
        if (parentDropPoint != null)
        {
            LoadTierPositions();
        }
    }
    
    /// <summary>
    /// Load các điểm drop từ 2 tầng
    /// </summary>
    private void LoadTierPositions()
    {
        tier1Positions.Clear();
        tier2Positions.Clear();
        
        // Tìm tầng 1
        Transform tier1Parent = parentDropPoint.Find(tier1Name);
        if (tier1Parent != null)
        {
            foreach (Transform child in tier1Parent)
        {
            if (child.gameObject.activeInHierarchy)
            {
                    tier1Positions.Add(child);
            }
        }
            Debug.Log($"Checkpoint: Đã load {tier1Positions.Count} điểm drop ở tầng 1.");
        }
        else
        {
            Debug.LogWarning($"Checkpoint: Không tìm thấy tầng 1 ({tier1Name}) trong {parentDropPoint.name}!");
        }
        
        // Tìm tầng 2
        Transform tier2Parent = parentDropPoint.Find(tier2Name);
        if (tier2Parent != null)
        {
            foreach (Transform child in tier2Parent)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    tier2Positions.Add(child);
                }
            }
            Debug.Log($"Checkpoint: Đã load {tier2Positions.Count} điểm drop ở tầng 2.");
        }
        else
        {
            Debug.LogWarning($"Checkpoint: Không tìm thấy tầng 2 ({tier2Name}) trong {parentDropPoint.name}!");
        }
        
        // Reset tầng hiện tại về 1
        currentTier = 1;
        usedTier1Positions.Clear();
        usedTier2Positions.Clear();
    }
    
    /// <summary>
    /// Được gọi khi player vào checkpoint
    /// </summary>
    public void OnPlayerEnter(PlayerController player)
    {
        if (player == null) return;
        
        // Kiểm tra xem player có đang mang item không
        if (player.HasCarriedItem())
        {
            // Lấy danh sách items đang mang (bản copy để tránh modify trong khi iterate)
            List<Item> itemsToDrop = new List<Item>(player.GetCarriedItems());
            
            if (itemsToDrop == null || itemsToDrop.Count == 0)
            {
                Debug.Log("Bạn cần mang item để thả tại checkpoint!");
                return;
            }
            
            // Thả từng item tại các vị trí khác nhau
            foreach (Item item in itemsToDrop)
            {
                if (item == null) continue;
                
                // Lấy ngẫu nhiên một điểm drop từ danh sách
                Transform dropPosition = GetRandomDropPosition();
                
                if (dropPosition == null)
                {
                    Debug.LogWarning("Checkpoint: Không có điểm drop nào khả dụng!");
                    continue;
                }
                
                // Lưu ItemType trước khi thả để dùng trong callback
                ItemType itemTypeToCollect = item.ItemType;
                Debug.Log($"Checkpoint: Đang thả item {itemTypeToCollect} tại checkpoint");
                
                // Thả item tại checkpoint - tính progress sau khi animation hoàn thành
                item.DropItemAtCheckpoint(dropPosition, () =>
                {
                    // Callback sau khi animation hoàn thành - tính progress và update UI
                    Debug.Log($"Checkpoint: Animation hoàn thành cho item {itemTypeToCollect}, đang tính progress");
                    
                    if (QuestManager.Instance != null)
                    {
                        QuestManager.Instance.OnItemCollected(itemTypeToCollect);
                    }
                    else
                    {
                        Debug.LogError("Checkpoint: QuestManager.Instance là null!");
                    }
                });
                
                // Spawn effect tại vị trí drop
                if (dropEffect != null)
                {
                    Instantiate(dropEffect, dropPosition.position, Quaternion.identity);
                }
            }
            
            // Clear danh sách items trong player
            player.ClearCarriedItems();
            
            // Play checkpoint sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCheckpointSound();
            }
            
            Debug.Log($"Đã thả {itemsToDrop.Count} item(s) tại checkpoint!");
            
            // Progress sẽ được tính trong callback sau khi animation hoàn thành
            // Kiểm tra win sẽ được gọi trong CheckQuestComplete() sau khi tăng progress
        }
        else
        {
            // Nếu không có item để thả, kiểm tra xem đã đủ progress chưa (trường hợp đã nhặt đủ item trước đó)
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.CheckAndCompleteQuest();
        }
        else
        {
            Debug.Log("Bạn cần mang item để thả tại checkpoint!");
            }
        }
    }
    
    /// <summary>
    /// Lấy ngẫu nhiên một điểm drop từ tầng hiện tại
    /// </summary>
    private Transform GetRandomDropPosition()
    {
        // Kiểm tra và chuyển tầng nếu cần
        CheckAndSwitchTier();
        
        // Lấy danh sách tầng hiện tại
        List<Transform> currentTierPositions = (currentTier == 1) ? tier1Positions : tier2Positions;
        List<Transform> usedCurrentTierPositions = (currentTier == 1) ? usedTier1Positions : usedTier2Positions;
            
        if (currentTierPositions == null || currentTierPositions.Count == 0)
            {
            Debug.LogWarning($"Checkpoint: Tầng {currentTier} không có điểm drop nào!");
                return null;
        }
        
        // Lấy danh sách các điểm chưa được sử dụng ở tầng hiện tại
        List<Transform> availablePositions = new List<Transform>();
        foreach (Transform pos in currentTierPositions)
        {
            if (!usedCurrentTierPositions.Contains(pos))
            {
                availablePositions.Add(pos);
            }
        }
        
        // Nếu không còn điểm nào ở tầng hiện tại, chuyển sang tầng tiếp theo
        if (availablePositions.Count == 0)
        {
            if (currentTier == 1 && tier2Positions.Count > 0)
            {
                Debug.Log("Checkpoint: Đã dùng hết tầng 1, chuyển sang tầng 2.");
                currentTier = 2;
                return GetRandomDropPosition(); // Gọi lại với tầng 2
            }
            else
            {
                Debug.LogWarning("Checkpoint: Đã dùng hết tất cả các điểm drop ở cả 2 tầng!");
                return null;
            }
        }
        
        // Chọn ngẫu nhiên một điểm từ danh sách chưa được sử dụng
        int randomIndex = Random.Range(0, availablePositions.Count);
        Transform selectedPosition = availablePositions[randomIndex];
        
        // Đánh dấu điểm này đã được sử dụng
        usedCurrentTierPositions.Add(selectedPosition);
        
        return selectedPosition;
    }
    
    /// <summary>
    /// Kiểm tra và chuyển tầng nếu tầng hiện tại đã hết
    /// </summary>
    private void CheckAndSwitchTier()
    {
        if (currentTier == 1)
        {
            // Kiểm tra xem tầng 1 đã hết chưa
            if (tier1Positions.Count > 0 && usedTier1Positions.Count >= tier1Positions.Count)
            {
                if (tier2Positions.Count > 0)
                {
                    currentTier = 2;
                    Debug.Log("Checkpoint: Tự động chuyển sang tầng 2.");
                }
            }
        }
    }
    
    /// <summary>
    /// Lấy parent drop point
    /// </summary>
    public Transform GetParentDropPoint()
    {
        return parentDropPoint;
    }
    
    /// <summary>
    /// Reload danh sách drop positions (dùng khi thêm/xóa điểm drop trong runtime)
    /// </summary>
    public void ReloadDropPositions()
    {
        LoadTierPositions();
    }
    
    /// <summary>
    /// Reset checkpoint (xóa tất cả item đã thả và reset danh sách drop positions)
    /// </summary>
    public void ResetCheckpoint()
    {
        usedTier1Positions.Clear();
        usedTier2Positions.Clear();
        currentTier = 1;
        LoadTierPositions();
    }
    
    /// <summary>
    /// Lấy tầng hiện tại đang sử dụng
    /// </summary>
    public int GetCurrentTier()
    {
        return currentTier;
    }
}


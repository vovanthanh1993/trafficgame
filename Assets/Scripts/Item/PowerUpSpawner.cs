using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner để spawn Health và Speed items tại các spawn points
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab cho Health Item")]
    [SerializeField] private GameObject healthItemPrefab;
    
    [Tooltip("Prefab cho Speed Item")]
    [SerializeField] private GameObject speedItemPrefab;
    
    [Header("Spawn Points")]
    [Tooltip("GameObject cha chứa tất cả các spawn points (sẽ tự động lấy tất cả các con)")]
    [SerializeField] private Transform spawnPointsParent;
    
    [Header("Spawn Timing")]
    [Tooltip("Thời gian giữa mỗi lần spawn (giây)")]
    [SerializeField] private float spawnInterval = 20f;
    
    [Tooltip("Tự động spawn khi Start")]
    [SerializeField] private bool spawnOnStart = true;
    
    [Tooltip("Thời gian delay trước khi bắt đầu spawn lần đầu (giây)")]
    [SerializeField] private float initialDelay = 10f;
    
    private List<Transform> spawnPoints = new List<Transform>();
    private List<GameObject> spawnedItems = new List<GameObject>();
    
    void Start()
    {
        InitializeSpawnPoints();
        
        if (spawnOnStart)
        {
            // Bắt đầu spawn định kỳ sau initialDelay giây
            InvokeRepeating(nameof(SpawnRandomPowerUp), initialDelay, spawnInterval);
        }
    }
    
    private void OnDestroy()
    {
        // Hủy spawn khi destroy
        CancelInvoke(nameof(SpawnRandomPowerUp));
    }
    
    /// <summary>
    /// Khởi tạo danh sách spawn points từ các con của spawnPointsParent
    /// </summary>
    private void InitializeSpawnPoints()
    {
        spawnPoints.Clear();
        
        if (spawnPointsParent == null)
        {
            Debug.LogWarning("PowerUpSpawner: spawnPointsParent không được set! Sẽ dùng các con của chính GameObject này.");
            // Dùng các con của chính GameObject này
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                {
                    spawnPoints.Add(child);
                }
            }
        }
        else
        {
            // Lấy tất cả các con của spawnPointsParent
            for (int i = 0; i < spawnPointsParent.childCount; i++)
            {
                Transform child = spawnPointsParent.GetChild(i);
                if (child != null)
                {
                    spawnPoints.Add(child);
                }
            }
        }
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("PowerUpSpawner: Không tìm thấy spawn point nào!");
        }
        else
        {
            Debug.Log($"PowerUpSpawner: Đã tìm thấy {spawnPoints.Count} spawn points");
        }
    }
    
    /// <summary>
    /// Spawn một power-up item ngẫu nhiên (Health hoặc Speed)
    /// </summary>
    private void SpawnRandomPowerUp()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("PowerUpSpawner: Không có spawn point nào!");
            return;
        }
        
        // Chọn ngẫu nhiên Health hoặc Speed
        bool spawnHealth = Random.Range(0, 2) == 0; // 50% chance
        GameObject prefabToSpawn = spawnHealth ? healthItemPrefab : speedItemPrefab;
        string itemName = spawnHealth ? "Health" : "Speed";
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"PowerUpSpawner: {itemName} Item prefab không được set!");
            return;
        }
        
        // Chọn ngẫu nhiên một spawn point
        int randomSpawnIndex = Random.Range(0, spawnPoints.Count);
        Transform spawnPoint = spawnPoints[randomSpawnIndex];
        
        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = spawnPoint.rotation;
        
        GameObject item = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
        spawnedItems.Add(item);
        
        Debug.Log($"PowerUpSpawner: Đã spawn {itemName} Item tại spawn point {randomSpawnIndex}");
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
    }
    
    /// <summary>
    /// Bắt đầu spawn định kỳ
    /// </summary>
    public void StartSpawning()
    {
        if (!IsInvoking(nameof(SpawnRandomPowerUp)))
        {
            InvokeRepeating(nameof(SpawnRandomPowerUp), 0f, spawnInterval);
        }
    }
    
    /// <summary>
    /// Dừng spawn định kỳ
    /// </summary>
    public void StopSpawning()
    {
        CancelInvoke(nameof(SpawnRandomPowerUp));
    }
    
    /// <summary>
    /// Spawn một item ngay lập tức (không đợi interval)
    /// </summary>
    public void SpawnOneNow()
    {
        SpawnRandomPowerUp();
    }
}


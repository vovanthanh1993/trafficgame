using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý danh sách các spawn points cho player
/// </summary>
public class PlayerSpawnPointManager : MonoBehaviour
{
    public static PlayerSpawnPointManager Instance { get; private set; }
    
    [Header("Spawn Points")]
    [Tooltip("Danh sách các spawn points cho player")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    /// <summary>
    /// Tìm spawn point gần player nhất và có x < player.x
    /// </summary>
    /// <param name="playerPosition">Vị trí hiện tại của player</param>
    /// <returns>Transform của spawn point phù hợp, null nếu không tìm thấy</returns>
    public Transform FindNearestSpawnPointBehindPlayer(Vector3 playerPosition)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("PlayerSpawnPointManager: Không có spawn point nào!");
            return null;
        }
        
        Transform nearestSpawnPoint = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;
            
            // Kiểm tra điều kiện: x của spawn point phải < x của player
            if (spawnPoint.position.x < playerPosition.x)
            {
                // Tính khoảng cách từ player đến spawn point
                float distance = Vector3.Distance(playerPosition, spawnPoint.position);
                
                // Nếu spawn point này gần hơn, cập nhật
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSpawnPoint = spawnPoint;
                }
            }
        }
        
        if (nearestSpawnPoint == null)
        {
            Debug.LogWarning($"PlayerSpawnPointManager: Không tìm thấy spawn point nào có x < player.x ({playerPosition.x:F2})");
        }
        else
        {
            Debug.Log($"PlayerSpawnPointManager: Tìm thấy spawn point gần nhất: {nearestSpawnPoint.name} (distance: {nearestDistance:F2})");
        }
        
        return nearestSpawnPoint;
    }
    
    /// <summary>
    /// Thêm spawn point vào danh sách
    /// </summary>
    public void AddSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint != null && !spawnPoints.Contains(spawnPoint))
        {
            spawnPoints.Add(spawnPoint);
        }
    }
    
    /// <summary>
    /// Xóa spawn point khỏi danh sách
    /// </summary>
    public void RemoveSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint != null)
        {
            spawnPoints.Remove(spawnPoint);
        }
    }
    
    /// <summary>
    /// Lấy tất cả spawn points
    /// </summary>
    public List<Transform> GetAllSpawnPoints()
    {
        return new List<Transform>(spawnPoints);
    }
}

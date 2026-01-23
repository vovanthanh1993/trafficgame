using UnityEngine;

/// <summary>
/// Item tăng mạng (health/life) cho player
/// </summary>
public class HealthItem : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Số mạng tăng thêm khi nhặt item")]
    [SerializeField] private int healthAmount = 1;
    
    [Header("Visual Settings")]
    [Tooltip("Effect khi nhặt item (particle, sound, etc.)")]
    [SerializeField] private GameObject pickupEffect;
    
    [Tooltip("Có tự động destroy sau khi nhặt không")]
    [SerializeField] private bool autoDestroy = true;
    
    [Header("Auto Destroy Settings")]
    [Tooltip("Tự động xóa item sau bao nhiêu giây (0 = không tự động xóa)")]
    [SerializeField] private float autoDestroyDelay = 30f;
    
    private bool isCollected = false;
    
    private void Start()
    {
        // Tự động xóa sau một khoảng thời gian nếu được set
        if (autoDestroyDelay > 0)
        {
            Invoke(nameof(AutoDestroy), autoDestroyDelay);
        }
    }
    
    /// <summary>
    /// Tự động destroy item sau thời gian delay
    /// </summary>
    private void AutoDestroy()
    {
        if (!isCollected)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Xử lý va chạm với trigger (nếu item là trigger)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Chỉ player mới nhặt được
        if (isCollected || !other.CompareTag("Player"))
            return;
        
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            CollectHealthItem(player);
        }
    }
    
    /// <summary>
    /// Xử lý khi player nhặt health item
    /// </summary>
    private void CollectHealthItem(PlayerController player)
    {
        if (isCollected)
            return;
        
        isCollected = true;
        
        // Tăng mạng cho player
        if (HealthPanel.Instance != null)
        {
            HealthPanel.Instance.AddLife(healthAmount);
            Debug.Log($"HealthItem: Đã tăng {healthAmount} mạng cho player");
        }
        else
        {
            Debug.LogWarning("HealthItem: HealthPanel.Instance không tồn tại!");
        }
        
        // Spawn VFX tại player's VFX point
        if (player != null)
        {
            player.SpawnHealthPickupVFX();
        }
        
        // Phát effect nếu có (tại vị trí item)
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
        
        // Phát sound nếu có
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHealSound();
        }
        
        // Hủy auto destroy nếu đã nhặt
        CancelInvoke(nameof(AutoDestroy));
        
        // Destroy hoặc disable item
        if (autoDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}


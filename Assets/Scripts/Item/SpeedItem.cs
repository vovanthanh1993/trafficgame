using UnityEngine;

/// <summary>
/// Item tăng tốc độ cho player trong thời gian nhất định
/// </summary>
public class SpeedItem : MonoBehaviour
{
    [Header("Speed Boost Settings")]
    [Tooltip("Tốc độ tăng thêm khi nhặt item")]
    [SerializeField] private float speedBoostAmount = 2f;
    
    [Tooltip("Thời gian speed boost (giây)")]
    [SerializeField] private float speedBoostDuration = 4f;
    
    [Header("Visual Settings")]
    [Tooltip("Effect khi nhặt item (particle, sound, etc.)")]
    [SerializeField] private GameObject pickupEffect;
    
    [Tooltip("Có tự động destroy sau khi nhặt không")]
    [SerializeField] private bool autoDestroy = true;
    
    [Header("Auto Destroy Settings")]
    [Tooltip("Tự động xóa item sau bao nhiêu giây (0 = không tự động xóa)")]
    [SerializeField] private float autoDestroyDelay = 10f;
    
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
            CollectSpeedItem(player);
        }
    }
    
    /// <summary>
    /// Xử lý khi player nhặt speed item
    /// </summary>
    private void CollectSpeedItem(PlayerController player)
    {
        if (isCollected)
            return;
        
        isCollected = true;
        
        // Kích hoạt speed boost cho player
        player.ActivateSpeedBoost(speedBoostAmount, speedBoostDuration);
        
        // Spawn VFX tại player's VFX point
        if (player != null)
        {
            player.SpawnSpeedPickupVFX();
        }
        
        // Phát effect nếu có (tại vị trí item)
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
        
        // Phát sound nếu có
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySpeedSound();
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


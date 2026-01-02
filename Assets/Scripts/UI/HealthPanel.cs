using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hệ thống mạng (lives) của player
/// Có 3 mạng, mỗi lần player về spawn point sẽ trừ 1 mạng
/// </summary>
public class HealthPanel : MonoBehaviour
{
    [Header("Health Images")]
    [Tooltip("3 ảnh health, thứ tự từ trái sang phải hoặc trên xuống dưới")]
    [SerializeField] private Image[] healthImages = new Image[3];
    
    [Header("Settings")]
    [Tooltip("Số mạng ban đầu")]
    [SerializeField] private int maxLives = 3;
    
    private int currentLives;
    public static HealthPanel Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Khởi tạo số mạng
        currentLives = maxLives;
        
        // Đảm bảo tất cả health images đều hiển thị ban đầu
        ResetHealth();
    }

    private void OnEnable()
    {
        // Reset health khi panel được enable lại (khi scene load lại)
        ResetHealth();
    }

    /// <summary>
    /// Reset health về trạng thái ban đầu (3 mạng)
    /// </summary>
    public void ResetHealth()
    {
        currentLives = maxLives;
        UpdateHealthDisplay();
    }

    /// <summary>
    /// Trừ 1 mạng khi player về spawn point
    /// </summary>
    /// <returns>true nếu còn mạng, false nếu hết mạng</returns>
    public bool LoseLife()
    {
        if (currentLives <= 0)
        {
            return false;
        }

        currentLives--;
        UpdateHealthDisplay();

        // Nếu hết mạng, hiển thị lose panel
        if (currentLives <= 0)
        {
            ShowLosePanel();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Cập nhật hiển thị health images dựa trên số mạng hiện tại
    /// </summary>
    private void UpdateHealthDisplay()
    {
        // Ẩn các health images tương ứng với số mạng đã mất
        for (int i = 0; i < healthImages.Length; i++)
        {
            if (healthImages[i] != null)
            {
                // Hiển thị image nếu còn mạng tương ứng
                healthImages[i].gameObject.SetActive(i < currentLives);
            }
        }

        Debug.Log($"HealthPanel: Số mạng hiện tại: {currentLives}/{maxLives}");
    }

    /// <summary>
    /// Hiển thị lose panel khi hết mạng
    /// </summary>
    private void ShowLosePanel()
    {
        if (UIManager.Instance != null && UIManager.Instance.gamePlayPanel != null)
        {
            UIManager.Instance.gamePlayPanel.ShowLosePanel(true);
            Time.timeScale = 0f;
            Debug.Log("HealthPanel: Đã hết mạng! Hiển thị Lose Panel.");
        }
        else
        {
            Debug.LogWarning("HealthPanel: Không thể hiển thị Lose Panel vì GamePlayPanel không tồn tại!");
        }
    }

    /// <summary>
    /// Lấy số mạng hiện tại
    /// </summary>
    public int GetCurrentLives()
    {
        return currentLives;
    }

    /// <summary>
    /// Kiểm tra xem còn mạng không
    /// </summary>
    public bool HasLives()
    {
        return currentLives > 0;
    }
    
    /// <summary>
    /// Tăng mạng cho player (khi nhặt health item)
    /// </summary>
    /// <param name="amount">Số mạng tăng thêm</param>
    public void AddLife(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("HealthPanel: Số mạng tăng phải lớn hơn 0!");
            return;
        }
        
        // Tăng mạng nhưng không vượt quá maxLives
        currentLives = Mathf.Min(currentLives + amount, maxLives);
        UpdateHealthDisplay();
        
        Debug.Log($"HealthPanel: Đã tăng {amount} mạng. Số mạng hiện tại: {currentLives}/{maxLives}");
    }
}

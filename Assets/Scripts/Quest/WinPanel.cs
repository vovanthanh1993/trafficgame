using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class WinPanel : MonoBehaviour
{
    [Header("Buttons")]
    public Button continueBtn;
    public Button returnHomeBtn;
    
    [Header("UI Elements")]
    public List<GameObject> starList = new List<GameObject>(); // List 3 star objects
    public TextMeshProUGUI rewardText;
    
    void Start() {
        if (continueBtn != null)
        {
            continueBtn.onClick.AddListener(OnContinueButtonClicked);
        }
        if (returnHomeBtn != null)
        {
            returnHomeBtn.onClick.AddListener(OnReturnHomeButtonClicked);
        }
    }

    void OnContinueButtonClicked()
    {
        // Lấy level hiện tại
        int currentLevel = 1;
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            currentLevel = PlayerPrefs.GetInt("CurrentLevel");
        }
        
        // Tính level tiếp theo
        int nextLevel = currentLevel + 1;
        
        // Kiểm tra xem level tiếp theo có tồn tại không
        int totalLevels = 50; // Default
        if (QuestDataManager.Instance != null)
        {
            totalLevels = QuestDataManager.Instance.GetQuestCount();
        }
        
        if (nextLevel > totalLevels)
        {
            // Đã hết level, quay về home
            Debug.Log($"Đã hoàn thành tất cả {totalLevels} level!");
            OnReturnHomeButtonClicked();
            return;
        }
        
        // Load level tiếp theo
        LoadNextLevel(nextLevel);
    }

    void OnReturnHomeButtonClicked()
    {
        GameCommonUtils.LoadScene("HomeScene");
        UIManager.Instance.ShowHomePanel(true);
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        AudioManager.Instance.PlaySelectSound();
    }
    
    /// <summary>
    /// Load level tiếp theo sử dụng logic tương tự StartPanel
    /// </summary>
    void LoadNextLevel(int level)
    {
        // Reset health trước khi load level tiếp theo
        if (HealthPanel.Instance != null)
        {
            HealthPanel.Instance.ResetHealth();
        }
        
        // Lưu level mới vào PlayerPrefs
        PlayerPrefs.SetInt("CurrentLevel", level);
        PlayerPrefs.Save();
        
        // Xác định scene dựa trên level (logic từ StartPanel)
        string sceneName = GetSceneNameForLevel(level);
        
        // Load scene
        GameCommonUtils.LoadScene(sceneName);
        UIManager.Instance.ShowGamePlayPanel(true);
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        AudioManager.Instance.PlaySelectSound();
    }
    
    /// <summary>
    /// Xác định tên scene dựa trên level (từ StartPanel)
    /// </summary>
    string GetSceneNameForLevel(int level)
    {
        // Tính vị trí trong chu kỳ 15 level (0-14)
        int positionInCycle = (level - 1) % 15;
        
        // Level 1-5 (position 0-4): GamePlay1
        // Level 6-10 (position 5-9): GamePlay2
        // Level 11-15 (position 10-14): GamePlay3
        if (positionInCycle < 5)
        {
            return "GamePlay1";
        }
        else if (positionInCycle < 10)
        {
            return "GamePlay2";
        }
        else
        {
            return "GamePlay3";
        }
    }

    public void Init(int star, int reward)
    {
        // Hiển thị stars dựa trên số sao đạt được (1-3)
        UpdateStarsDisplay(star);
        
        if (rewardText != null)
        {
            rewardText.text = reward.ToString();
        }
    }

    private void UpdateStarsDisplay(int starCount)
    {
        // Đảm bảo có đủ 3 stars
        if (starList.Count < 3)
        {
            Debug.LogWarning("WinPanel: Cần 3 star objects trong starList!");
            return;
        }

        // Hiển thị stars: hiện star nếu index < số sao đạt được, ẩn nếu không
        for (int i = 0; i < 3; i++)
        {
            if (starList[i] != null)
            {
                starList[i].SetActive(i < starCount);
            }
        }
    }
}

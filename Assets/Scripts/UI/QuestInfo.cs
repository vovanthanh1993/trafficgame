using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class QuestInfo : MonoBehaviour
{
    [Header("Panel UI")]
    public TextMeshProUGUI levelTitle;
    public TextMeshProUGUI description;
    public TextMeshProUGUI star2TimeText;
    public TextMeshProUGUI star3TimeText;

    public Button closeBtn;

    void Start() {
        closeBtn.onClick.AddListener(OnCloseButtonClicked);
    }

    void OnCloseButtonClicked() {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnEnable() {
        UpdateQuestInfo();
    }

    /// <summary>
    /// Cập nhật thông tin quest từ QuestManager
    /// </summary>
    public void UpdateQuestInfo()
    {
        if (QuestManager.Instance == null || QuestManager.Instance.currentQuest == null)
        {
            Debug.LogWarning("QuestInfo: QuestManager.Instance hoặc currentQuest là null");
            return;
        }

        QuestData questData = QuestManager.Instance.currentQuest;

        // Hiển thị level title từ scene name
        if (levelTitle != null)
        {
            int level = GetCurrentLevelFromScene();
            levelTitle.text = "Level " + level;
        }

        // Tạo và hiển thị description từ QuestObjective
        if (description != null)
        {
            string generatedDescription = GenerateDescriptionFromObjectives(questData.objectives);
            if (!string.IsNullOrEmpty(generatedDescription))
            {
                description.text = generatedDescription;
            }
        }

        // Hiển thị thời gian để đạt 2 sao
        if (star2TimeText != null)
        {
            string timeFormatted = FormatTime(questData.timeFor2Stars);
            star2TimeText.text = $"Complete quest in {timeFormatted}";
        }

        // Hiển thị thời gian để đạt 3 sao
        if (star3TimeText != null)
        {
            string timeFormatted = FormatTime(questData.timeFor3Stars);
            star3TimeText.text = $"Complete quest in {timeFormatted}";
        }
    }

    /// <summary>
    /// Lấy số level hiện tại từ PlayerPrefs (được lưu khi load scene từ StartPanel)
    /// </summary>
    private int GetCurrentLevelFromScene()
    {
        // Lấy level từ PlayerPrefs (được lưu trong StartPanel khi load scene)
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            int level = PlayerPrefs.GetInt("CurrentLevel");
            return level;
        }
        
        // Fallback: thử parse từ scene name nếu có (cho tương thích ngược)
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Level"))
        {
            string levelStr = sceneName.Substring(5);
            if (int.TryParse(levelStr, out int level))
            {
                return level;
            }
        }
        
        // Fallback cuối cùng: trả về 1
        return 1;
    }

    /// <summary>
    /// Tạo description từ QuestObjective array
    /// </summary>
    private string GenerateDescriptionFromObjectives(QuestObjective[] objectives)
    {
        if (objectives == null || objectives.Length == 0)
            return "";

        // Format gem collection objectives
        return FormatGemObjectives(new List<QuestObjective>(objectives));
    }

    /// <summary>
    /// Format các animal objectives thành "Collect 1 deer, 4 fox"
    /// </summary>
    private string FormatGemObjectives(List<QuestObjective> objectives)
    {
        if (objectives == null || objectives.Count == 0)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Collect ");

        for (int i = 0; i < objectives.Count; i++)
        {
            QuestObjective obj = objectives[i];
            string itemName = obj.itemType.ToString().ToLower();

            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append($"{obj.requiredAmount} {itemName}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format các CollectItem objectives
    /// </summary>
    private string FormatCollectItemObjectives(List<QuestObjective> objectives)
    {
        if (objectives == null || objectives.Count == 0)
            return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Collect ");

        for (int i = 0; i < objectives.Count; i++)
        {
            QuestObjective obj = objectives[i];

            if (i > 0)
            {
                sb.Append(" , ");
            }

            if (obj.requiredAmount == 1)
            {
                sb.Append("1 item");
            }
            else
            {
                sb.Append($"{obj.requiredAmount} items");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Format thời gian từ giây sang định dạng "min:ss"
    /// </summary>
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return string.Format("{0}:{1:00}", minutes, secs);
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class GUIPanel : MonoBehaviour
{
    public static GUIPanel Instance { get; private set; }
    public TextMeshProUGUI timeText;

    public Image healthBar;
    public TextMeshProUGUI healthText;

    public HintPanel hintPanel;

    public ObjectivesPanel objectivesPanelComponent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetTime(string time) {
        if (timeText != null)
        {
            timeText.text = time;
        }
    }

    public void OnSettingButtonClicked() {
        UIManager.Instance.ShowSettingPanel(true);
        Time.timeScale = 0f;
    }

    public void ShowHintPanel(bool isShow) {
        Time.timeScale = 0f;
        hintPanel.gameObject.SetActive(isShow);
    }

    public void SetHealthBar(float currentHealth, float maxHealth) {
        if (healthBar != null) {
            // Tính tỷ lệ fillAmount (0-1)
            healthBar.fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }
        if (healthText != null) {
            // Hiển thị dạng currentHealth/maxHealth
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }
}

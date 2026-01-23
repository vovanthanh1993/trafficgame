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

    public Button slowSkillButton;
    public TextMeshProUGUI slowSkillButtonText;
    public Image slowSkillButtonImage;

    void Start()
    {
        if (slowSkillButton != null)
        {
            slowSkillButton.onClick.AddListener(OnSlowSkillButtonClicked);
        }
    }

    void Update()
    {
        UpdateSlowSkillUI();
    }

    void OnSlowSkillButtonClicked()
    {
        if (PlayerController.Instance != null)
        {
            bool activated = PlayerController.Instance.ActivateSlowSkill();
            if (!activated)
            {
                // Skill đang trong cooldown, không làm gì
                return;
            }
        }
    }

    void UpdateSlowSkillUI()
    {
        if (PlayerController.Instance == null)
            return;

        bool isReady = PlayerController.Instance.IsSlowSkillReady();
        float cooldownRemaining = PlayerController.Instance.GetSlowSkillCooldownRemaining();
        float cooldownTotal = PlayerController.Instance.GetSlowSkillCooldownTotal();

        // Cập nhật fillAmount: 1 = bắt đầu cooldown, 0 = hết cooldown
        if (slowSkillButtonImage != null)
        {
            if (isReady)
            {
                // Khi đầy (skill sẵn sàng), ẩn image đi
                slowSkillButtonImage.gameObject.SetActive(false);
            }
            else
            {
                // Hiển thị image và fill từ 1 đến 0
                slowSkillButtonImage.gameObject.SetActive(true);
                slowSkillButtonImage.fillAmount = cooldownRemaining / cooldownTotal;
            }
        }

        // Cập nhật text: hiển thị số giây cooldown còn lại
        if (slowSkillButtonText != null)
        {
            if (isReady)
            {
                // Khi sẵn sàng, không hiển thị gì
                slowSkillButtonText.text = "";
            }
            else
            {
                // Hiển thị số giây còn lại (làm tròn lên)
                slowSkillButtonText.text = $"{Mathf.CeilToInt(cooldownRemaining)}s";
            }
        }

        // Cập nhật interactable: chỉ cho phép click khi skill sẵn sàng
        if (slowSkillButton != null)
        {
            slowSkillButton.interactable = isReady;
        }
    }

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

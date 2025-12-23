using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
public class HomePanel : MonoBehaviour
{
    public Button shopBtn;
    public Button playBtn;
    public TextMeshProUGUI coinText;

    void Start()
    {
        shopBtn.onClick.AddListener(OnShopButtonClicked); 
        playBtn.onClick.AddListener(OnPlayButtonClicked);
        UpdateRewardDisplay();
    }

    void OnShopButtonClicked()
    {
        UIManager.Instance.ShowHomePanel(false);
        UIManager.Instance.ShowCharacterSelectPanel(true);
    }

    void OnPlayButtonClicked()
    {
        UIManager.Instance.ShowSelectLevelPanel(true);
    }

    private void OnEnable() {
        UIManager.Instance.ShowGamePlayPanel(false);
        UpdateRewardDisplay();
    }

    /// <summary>
    /// Cập nhật hiển thị reward từ PlayerData
    /// </summary>
    public void UpdateRewardDisplay()
    {
        if (coinText != null)
        {
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.playerData != null)
            {
                coinText.text = PlayerDataManager.Instance.playerData.totalReward.ToString();
            }
            else
            {
                coinText.text = "0";
            }
        }
    }

}

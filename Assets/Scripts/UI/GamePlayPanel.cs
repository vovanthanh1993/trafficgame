using UnityEngine;

public class GamePlayPanel : MonoBehaviour
{
    public GameObject winPanel;
    public GameObject losePanel;

    private void OnEnable()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        // Reset health khi bắt đầu level mới
        if (HealthPanel.Instance != null)
        {
            HealthPanel.Instance.ResetHealth();
        }
    }

    public void ShowWinPanel(bool isShow, int star, int reward){
        AudioManager.Instance.PlayWinSound();
        AudioManager.Instance.PlayRewardSound();
        winPanel.SetActive(isShow);
        winPanel.GetComponent<WinPanel>().Init(star, reward);
    }

    public void ShowLosePanel(bool isShow){
        AudioManager.Instance.PlayLoseSound();
        losePanel.SetActive(isShow);
    }
}

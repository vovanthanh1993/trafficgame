using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class LosePanel : MonoBehaviour
{
    public Button retryBtn;
    public Button homeBtn;

    void Start() {
        retryBtn.onClick.AddListener(OnRetryButtonClicked);   
        homeBtn.onClick.AddListener(OnHomeButtonClicked);   
    }

    void OnRetryButtonClicked()
    {
        // Reset health trước khi load scene lại
        if (HealthPanel.Instance != null)
        {
            HealthPanel.Instance.ResetHealth();
        }
        
        string sceneName = SceneManager.GetActiveScene().name;
        GameCommonUtils.LoadScene(sceneName);
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        AudioManager.Instance.PlaySelectSound();
    }

    void OnHomeButtonClicked() {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        UIManager.Instance.ShowHomePanel(true);
        GameCommonUtils.LoadScene("HomeScene");
        AudioManager.Instance.PlaySelectSound();
    }
}

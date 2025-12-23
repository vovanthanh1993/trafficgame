using UnityEngine;
using UnityEngine.UI;

public class HintPanel : MonoBehaviour
{
    public Button closeBtn;

    void Start()
    {
        closeBtn.onClick.AddListener(OnCloseButtonClicked);
    }

    void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        AudioManager.Instance.PlayCloseSound();
    }
}

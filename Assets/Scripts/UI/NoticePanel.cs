using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class NoticePanel : MonoBehaviour
{
    public TextMeshProUGUI noticeText;
    public Button closeButton;
    public void Awake()
    {
        closeButton.onClick.AddListener(Close);
    }
    public void Init(string noticeText)
    {
        this.noticeText.text = noticeText;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        AudioManager.Instance.PlayCloseSound();
    }
}

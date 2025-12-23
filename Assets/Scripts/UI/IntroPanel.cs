using UnityEngine;
using UnityEngine.UI;
public class IntroPanel : MonoBehaviour
{
    public Button playBtn;

    void Start()
    {
        playBtn.onClick.AddListener(OnPlayButtonClicked);
    }
    void OnPlayButtonClicked()
    {
        GameCommonUtils.LoadScene("HomeScene");
        UIManager.Instance.ShowIntroPanel(false);
        UIManager.Instance.ShowHomePanel(true);
    }
}

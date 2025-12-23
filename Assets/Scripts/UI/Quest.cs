using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Quest : MonoBehaviour
{
    public Image starEmpty;
    public Image starFull;
    public TextMeshProUGUI questText;

    public void Init(string questText, bool isCompleted) {
        this.questText.text = questText;
        starEmpty.gameObject.SetActive(!isCompleted);
        starFull.gameObject.SetActive(isCompleted);
    }
}

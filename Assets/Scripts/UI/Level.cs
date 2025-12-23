using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class Level : MonoBehaviour
{
    public TextMeshProUGUI levelText;
    public List<GameObject> starOnList = new List<GameObject>(); // List 3 star on objects
    public bool isLocked = true;
    public Image lockImage;
    private PlayerLevelData levelData;

    public GameObject starGradient;

    public void Init(PlayerLevelData data) {
        levelData = data;
        isLocked = data.isLocked;
        
        if (isLocked) {
            // Khi lock: chỉ hiện ảnh lock, ẩn tất cả phần còn lại
            if (lockImage != null) {
                lockImage.gameObject.SetActive(true);
            }
            if (levelText != null) {
                levelText.gameObject.SetActive(false);
            }
            // Ẩn tất cả stars
            HideAllStars();
            starGradient.SetActive(false);
        } else {
            // Khi unlock: hiện level text và stars, ẩn lock
            if (lockImage != null) {
                lockImage.gameObject.SetActive(false);
            }
            if (levelText != null) {
                levelText.gameObject.SetActive(true);
                levelText.text = data.level.ToString();
            }
            // Hiển thị stars dựa trên số sao đạt được (0-3)
            UpdateStarsDisplay(data.star);
            starGradient.SetActive(true);
        }
    }

    private void UpdateStarsDisplay(int starCount) {
        // Đảm bảo có đủ 3 stars
        if (starOnList.Count < 3) {
            Debug.LogWarning("Level: Cần 3 star on objects!");
            return;
        }

        // Hiển thị stars: bật star on dựa trên số sao đạt được
        for (int i = 0; i < 3; i++) {
            if (starOnList[i] != null) {
                // Hiện star nếu index < số sao đạt được, ẩn nếu không
                starOnList[i].SetActive(i < starCount);
            }
        }
    }

    private void HideAllStars() {
        // Ẩn tất cả stars
        foreach (var starOn in starOnList) {
            if (starOn != null) starOn.SetActive(false);
        }
    }

    public void OnClick()
    {
        Debug.Log("OnClick: " + levelData.level);
        if (isLocked) return;
        if (UIManager.Instance.startPanel != null)
        {
            UIManager.Instance.startPanel.ShowForLevel(levelData);
            AudioManager.Instance.PlayPopupSound();
        }
    }
}

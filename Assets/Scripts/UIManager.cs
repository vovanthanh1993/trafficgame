using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    public GameObject introPanel;
    public HomePanel homePanel;

    public GameObject selectLevelPanel;

    public StartPanel startPanel;

    public GamePlayPanel gamePlayPanel;

    public GameObject loadingPanel;

    public NoticePanel noticePanel;

    public SettingPanel settingPanel;

    public GameObject characterSelectPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowLoadingPanel(bool isShow) {
        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(isShow);
        }
    }

    public void ShowSelectLevelPanel(bool isShow) {
        if (selectLevelPanel != null)
        {
            selectLevelPanel.SetActive(isShow);
            
            // Refresh SelectLevelPanel khi panel được hiển thị
            if (isShow)
            {
                SelectLevelPanel selectLevelPanelComponent = selectLevelPanel.GetComponentInChildren<SelectLevelPanel>();
                if (selectLevelPanelComponent != null)
                {
                    selectLevelPanelComponent.Refresh();
                }
            }
        }
    }

    public void ShowGamePlayPanel(bool isShow) {
        if (gamePlayPanel != null)
        {
            gamePlayPanel.gameObject.SetActive(isShow);
        }
    }

    public void ShowIntroPanel(bool isShow) {
        if (introPanel != null)
        {
            introPanel.SetActive(isShow);
        }
    }

    public void ShowHomePanel(bool isShow) {
        if (homePanel != null)
        {
            homePanel.gameObject.SetActive(isShow);
        }
    }

    private void OnEnable() {
        selectLevelPanel.gameObject.SetActive(false);
        gamePlayPanel.gameObject.SetActive(false);
        introPanel.SetActive(true);
        homePanel.gameObject.SetActive(false);
        noticePanel.gameObject.SetActive(false);
        settingPanel.gameObject.SetActive(false);
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Nhấn F1 để unlock tất cả level (cheat code)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            UnlockAllLevels();
        }
    }
    
    /// <summary>
    /// Unlock tất cả level (cheat code F1)
    /// </summary>
    private void UnlockAllLevels()
    {
        // Đếm số level đã unlock trước đó
        Dictionary<int, QuestData> allQuests = QuestDataStorage.LoadAllQuests();
        int lockedCountBefore = 0;
        foreach (var quest in allQuests.Values)
        {
            if (QuestDataStorage.IsQuestLocked(quest.questId))
            {
                lockedCountBefore++;
            }
        }
        
        // Unlock tất cả quest
        QuestDataStorage.UnlockAllQuests();
        
        // Đếm lại số level đã unlock
        allQuests = QuestDataStorage.LoadAllQuests();
        int unlockedCount = 0;
        foreach (var quest in allQuests.Values)
        {
            if (!QuestDataStorage.IsQuestLocked(quest.questId))
            {
                unlockedCount++;
            }
        }
        
        // Refresh SelectLevelPanel nếu đang mở
        if (selectLevelPanel != null && selectLevelPanel.activeSelf)
        {
            SelectLevelPanel selectLevelPanelComponent = selectLevelPanel.GetComponentInChildren<SelectLevelPanel>();
            if (selectLevelPanelComponent != null)
            {
                selectLevelPanelComponent.Refresh();
            }
        }
        
        // Hiển thị thông báo
        string message = lockedCountBefore > 0 
            ? $"Đã mở khóa {lockedCountBefore} level!\nTổng cộng: {unlockedCount} level đã mở khóa."
            : $"Tất cả level đã được mở khóa!\nTổng cộng: {unlockedCount} level.";
        
        if (noticePanel != null)
        {
            noticePanel.Init(message);
        }
        
        Debug.Log($"Cheat Code F1: {message}");
    }

    public void ShowSettingPanel(bool isShow) {
        if (settingPanel != null)
        {
            settingPanel.gameObject.SetActive(isShow);
        }
    }

    public void ShowCharacterSelectPanel(bool isShow) {
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(isShow);
        }
    }
}

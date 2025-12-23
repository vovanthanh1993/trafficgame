using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SelectLevelPanel : MonoBehaviour
{
    [Header("Level Setup")]
    [Tooltip("Content của Scroll-Snap. Nếu để trống, sẽ tự động tìm")]
    public Transform contentRoot; // Content của Scroll-Snap

    [Range(1, 200)]
    public int totalLevels = 50;

    private PlayerData playerData;

    private void Awake()
    {
        // Tự động tìm Content nếu chưa gán
        if (contentRoot == null)
        {
            FindContent();
        }
    }

    private void Start()
    {
        LoadPlayerData();
        // Đảm bảo quest data đã được tạo
        EnsureQuestDataExists();
        // Init tất cả StageComplete
        InitializeAllStageCompletes();
    }

    /// <summary>
    /// Tự động tìm Content từ Scroll-Snap
    /// </summary>
    private void FindContent()
    {
        // Tìm GameObject có tên chứa "Scroll-Snap" hoặc "Scroll_Snap"
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        Transform scrollSnap = null;
        
        foreach (Transform child in allChildren)
        {
            if (child.name.Contains("Scroll-Snap") || child.name.Contains("Scroll_Snap"))
            {
                scrollSnap = child;
                break;
            }
        }
        
        if (scrollSnap != null)
        {
            // Tìm Viewport -> Content
            Transform viewport = scrollSnap.Find("Viewport");
            if (viewport != null)
            {
                Transform content = viewport.Find("Content");
                if (content != null)
                {
                    contentRoot = content;
                    Debug.Log("SelectLevelPanel: Đã tự động tìm thấy Content");
                    return;
                }
            }
            
            // Nếu không tìm thấy theo cách trên, tìm trực tiếp Content trong Scroll-Snap
            Transform contentDirect = scrollSnap.Find("Content");
            if (contentDirect == null)
            {
                // Tìm trong tất cả children
                foreach (Transform child in scrollSnap)
                {
                    if (child.name == "Content")
                    {
                        contentDirect = child;
                        break;
                    }
                }
            }
            if (contentDirect != null)
            {
                contentRoot = contentDirect;
                Debug.Log("SelectLevelPanel: Đã tự động tìm thấy Content");
                return;
            }
        }
        
        // Nếu vẫn chưa tìm thấy, tìm tất cả các GameObject có tên "Content"
        foreach (Transform child in allChildren)
        {
            if (child.name == "Content")
            {
                contentRoot = child;
                Debug.Log("SelectLevelPanel: Đã tự động tìm thấy Content");
                return;
            }
        }
        
        Debug.LogWarning("SelectLevelPanel: Không tìm thấy Content! Vui lòng gán thủ công trong Inspector.");
    }
    
    /// <summary>
    /// Refresh lại UI với dữ liệu mới nhất từ PlayerData
    /// </summary>
    public void Refresh()
    {
        LoadPlayerData();
        // Refresh tất cả StageComplete để hiển thị số sao mới nhất
        InitializeAllStageCompletes();
    }

    private void LoadPlayerData()
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.playerData != null)
        {
            playerData = PlayerDataManager.Instance.playerData;
        }
        else
        {
            // Fallback nếu PlayerDataManager chưa được khởi tạo
            playerData = PlayerDataStorage.LoadOrCreateDefault(totalLevels);
        }
        
        // Lấy số lượng quest từ QuestDataManager
        if (QuestDataManager.Instance != null)
        {
            totalLevels = QuestDataManager.Instance.GetQuestCount();
        }
        else
        {
            // Fallback: load trực tiếp từ storage
            var allQuests = QuestDataStorage.LoadAllQuests();
            if (allQuests != null && allQuests.Count > 0)
            {
                totalLevels = allQuests.Count;
            }
        }
    }

    /// <summary>
    /// Đảm bảo quest data đã được tạo
    /// </summary>
    private void EnsureQuestDataExists()
    {
        if (QuestDataManager.Instance == null)
        {
            Debug.LogWarning("SelectLevelPanel: QuestDataManager chưa được khởi tạo! Quest data có thể chưa được tạo.");
            return;
        }

        // Load hoặc tạo quest data
        QuestDataManager.Instance.LoadOrCreateQuests();
    }

    /// <summary>
    /// Khởi tạo tất cả StageComplete dựa trên quest data
    /// </summary>
    private void InitializeAllStageCompletes()
    {
        // Tự động tìm lại Content nếu chưa có
        if (contentRoot == null)
        {
            FindContent();
        }

        // Kiểm tra Content có tồn tại không
        if (contentRoot == null)
        {
            Debug.LogWarning("SelectLevelPanel: Content không tồn tại! Vui lòng gán trong Inspector.");
            return;
        }

        // Tìm tất cả StageComplete trong Content
        List<GameObject> stageCompletes = FindAllStageCompletes();

        if (stageCompletes.Count == 0)
        {
            Debug.LogWarning("SelectLevelPanel: Không tìm thấy StageComplete nào!");
            return;
        }

        Debug.Log($"SelectLevelPanel: Tìm thấy {stageCompletes.Count} StageComplete, đang khởi tạo...");

        // Khởi tạo từng StageComplete
        int levelNumber = 1;
        foreach (GameObject stageComplete in stageCompletes)
        {
            if (stageComplete == null) continue;

            // Thêm Level component nếu chưa có
            Level levelComponent = stageComplete.GetComponent<Level>();
            if (levelComponent == null)
            {
                levelComponent = stageComplete.AddComponent<Level>();
                Debug.Log($"SelectLevelPanel: Đã thêm Level component vào {stageComplete.name}");
            }

            // Lấy level data từ quest
            PlayerLevelData levelData = GetLevelData(levelNumber);

            // Init level data
            if (levelComponent != null)
            {
                levelComponent.Init(levelData);
            }

            levelNumber++;

            // Dừng nếu đã đủ totalLevels
            if (levelNumber > totalLevels)
            {
                break;
            }
        }

        Debug.Log($"SelectLevelPanel: Đã khởi tạo {Mathf.Min(stageCompletes.Count, totalLevels)} StageComplete!");
    }

    /// <summary>
    /// Tìm tất cả StageComplete trong Content
    /// </summary>
    private List<GameObject> FindAllStageCompletes()
    {
        List<GameObject> stageCompletes = new List<GameObject>();

        if (contentRoot == null) return stageCompletes;

        // Tìm tất cả StageComplete trong Content (có thể nằm trong LevelPage)
        Transform[] allChildren = contentRoot.GetComponentsInChildren<Transform>(true);
        
        foreach (Transform child in allChildren)
        {
            // Kiểm tra tên có chứa "StageComplete" không
            if (child.name.Contains("StageComplete"))
            {
                // Chỉ lấy các StageComplete là con trực tiếp của LevelPage hoặc Content
                Transform parent = child.parent;
                if (parent != null && (parent.name.Contains("LevelPage") || parent == contentRoot))
                {
                    if (!stageCompletes.Contains(child.gameObject))
                    {
                        stageCompletes.Add(child.gameObject);
                    }
                }
            }
        }

        // Sắp xếp theo thứ tự trong hierarchy
        stageCompletes.Sort((a, b) =>
        {
            // Sắp xếp theo parent trước, sau đó theo sibling index
            int parentCompare = a.transform.parent.GetSiblingIndex().CompareTo(b.transform.parent.GetSiblingIndex());
            if (parentCompare != 0) return parentCompare;
            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        });

        return stageCompletes;
    }

    /// <summary>
    /// Lấy level data từ quest data
    /// </summary>
    private PlayerLevelData GetLevelData(int levelNumber)
    {
        int stars = 0;
        bool isLocked = true;

        if (QuestDataManager.Instance != null)
        {
            stars = QuestDataManager.Instance.GetQuestStars(levelNumber);
            isLocked = QuestDataManager.Instance.IsQuestLocked(levelNumber);
        }
        else
        {
            // Fallback: load trực tiếp từ storage
            stars = QuestDataStorage.GetQuestStars(levelNumber);
            isLocked = QuestDataStorage.IsQuestLocked(levelNumber);
        }

        return new PlayerLevelData
        {
            level = levelNumber,
            star = stars,
            isLocked = isLocked
        };
    }


}


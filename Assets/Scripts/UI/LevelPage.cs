using UnityEngine;

public class LevelPage : MonoBehaviour
{
    [Header("Level Setup")]
    public int levelsPerPage = 5; // Mỗi LevelPage có 5 level

    private System.Collections.Generic.List<Level> levelComponents = new System.Collections.Generic.List<Level>();

    private void Awake()
    {
        // Tìm tất cả các Level component có sẵn trong LevelPage
        FindLevelComponents();
    }

    /// <summary>
    /// Tìm tất cả các Level component có sẵn trong LevelPage
    /// </summary>
    private void FindLevelComponents()
    {
        levelComponents.Clear();
        Level[] levels = GetComponentsInChildren<Level>(true);
        foreach (Level level in levels)
        {
            // Chỉ lấy các level là con trực tiếp của LevelPage
            if (level.transform.parent == transform)
            {
                levelComponents.Add(level);
            }
        }
        // Sắp xếp theo thứ tự trong hierarchy
        levelComponents.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
    }

    /// <summary>
    /// Build levels vào LevelPage này - chỉ cập nhật data cho các level có sẵn
    /// </summary>
    /// <param name="startLevelIndex">Index bắt đầu của level (0-based)</param>
    /// <param name="totalLevels">Tổng số level có sẵn</param>
    /// <returns>Số level đã được cập nhật</returns>
    public int BuildLevels(int startLevelIndex, int totalLevels)
    {
        // Tìm lại các level component nếu chưa có
        if (levelComponents.Count == 0)
        {
            FindLevelComponents();
        }

        int levelIndex = startLevelIndex;
        int levelsUpdated = 0;

        // Cập nhật data cho các level có sẵn
        for (int i = 0; i < levelComponents.Count && i < levelsPerPage; i++)
        {
            if (levelIndex >= totalLevels)
            {
                // Đã hết level, ẩn level này
                if (levelComponents[i] != null)
                {
                    levelComponents[i].gameObject.SetActive(false);
                }
                continue;
            }

            if (levelComponents[i] != null)
            {
                PlayerLevelData levelInfo = GetLevelInfo(levelIndex);
                if (levelInfo == null)
                {
                    levelInfo = new PlayerLevelData
                    {
                        level = levelIndex + 1,
                        star = 0,
                        isLocked = levelIndex != 0
                    };
                }

                levelComponents[i].Init(levelInfo);
                levelComponents[i].gameObject.SetActive(true);
                levelIndex++;
                levelsUpdated++;
            }
        }

        return levelsUpdated;
    }

    /// <summary>
    /// Refresh lại levels với dữ liệu mới nhất
    /// </summary>
    public void RefreshLevels(int startLevelIndex, int totalLevels)
    {
        // Tìm lại các level component nếu chưa có
        if (levelComponents.Count == 0)
        {
            FindLevelComponents();
        }

        // Cập nhật thông tin cho các level đã có
        for (int i = 0; i < levelComponents.Count; i++)
        {
            if (levelComponents[i] != null)
            {
                int levelNumber = startLevelIndex + i + 1;
                if (levelNumber <= totalLevels)
                {
                    PlayerLevelData levelInfo = GetLevelInfo(startLevelIndex + i);
                    if (levelInfo != null)
                    {
                        levelComponents[i].Init(levelInfo);
                    }
                }
            }
        }
    }

    private PlayerLevelData GetLevelInfo(int index)
    {
        int levelNumber = index + 1;
        
        // Lấy thông tin từ QuestDataManager
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


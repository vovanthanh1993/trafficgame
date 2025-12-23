using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Class để lưu giá của character (có thể set trong Inspector)
/// </summary>
[System.Serializable]
public class CharacterPrice
{
    public string characterName;
    public int price;
}

/// <summary>
/// Quản lý logic nghiệp vụ cho Character Selection
/// </summary>
public class CharacterSelectManager : MonoBehaviour
{
    [Header("Character Settings")]
    [Tooltip("Đường dẫn trong Resources folder (ví dụ: 'Characters' hoặc 'Players')")]
    public string characterResourcePath = "Characters";
    [Tooltip("Tên character mặc định (sẽ được unlock tự động)")]
    public string defaultCharacterName = "Player";
    [Tooltip("Giá mua mặc định cho các character (có thể override bằng CharacterPrices)")]
    public int defaultPrice = 100;

    [Header("Character Prices")]
    [Tooltip("Danh sách giá cho từng character. Nếu không có trong list sẽ dùng defaultPrice")]
    public List<CharacterPrice> characterPrices = new List<CharacterPrice>();

    [Header("Spawn Point")]
    [Tooltip("Nơi hiển thị character prefab (kéo từ scene vào đây)")]
    public Transform characterSpawnPoint;

    private List<string> availableCharacterNames = new List<string>();
    private List<string> unlockedCharacters = new List<string>();
    private List<string> allCharacters = new List<string>(); // Tất cả characters (bao gồm cả locked)
    private string selectedCharacterName;
    private GameObject currentSpawnedCharacter; // Character đã spawn

    // Singleton instance
    private static CharacterSelectManager _instance;
    public static CharacterSelectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CharacterSelectManager>();
                if (_instance == null)
                {
                    Debug.LogError("CharacterSelectManager: Không tìm thấy CharacterSelectManager trong scene! Vui lòng thêm GameObject với CharacterSelectManager component.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("CharacterSelectManager: Đã có instance khác, xóa duplicate.");
            Destroy(gameObject);
            return;
        }

        // Kiểm tra characterSpawnPoint đã được gán chưa
        if (characterSpawnPoint == null)
        {
            Debug.LogWarning("CharacterSelectManager: characterSpawnPoint chưa được gán! Vui lòng kéo GameObject từ scene vào Inspector.");
        }
    }

    private void Start()
    {
        LoadAvailableCharacters();
        LoadUnlockedCharacters();
        
        // Spawn nhân vật đã chọn
        SpawnSelectedCharacter();
    }

    /// <summary>
    /// Load tất cả character prefab từ Resources
    /// </summary>
    public void LoadAvailableCharacters()
    {
        availableCharacterNames.Clear();
        allCharacters.Clear();
        
        // Load tất cả prefab từ Resources
        GameObject[] characters = Resources.LoadAll<GameObject>(characterResourcePath);
        
        foreach (GameObject character in characters)
        {
            if (character != null)
            {
                availableCharacterNames.Add(character.name);
                allCharacters.Add(character.name);
            }
        }

        // Sắp xếp theo tên để đảm bảo thứ tự nhất quán
        availableCharacterNames.Sort();
        allCharacters.Sort();

        // Đảm bảo character mặc định luôn có trong list
        if (!availableCharacterNames.Contains(defaultCharacterName))
        {
            availableCharacterNames.Insert(0, defaultCharacterName);
        }
        if (!allCharacters.Contains(defaultCharacterName))
        {
            allCharacters.Insert(0, defaultCharacterName);
        }

        Debug.Log($"CharacterSelectManager: Đã load {allCharacters.Count} characters từ Resources/{characterResourcePath}");
    }

    /// <summary>
    /// Load danh sách nhân vật đã được mua từ PlayerPrefs
    /// </summary>
    public void LoadUnlockedCharacters()
    {
        unlockedCharacters.Clear();
        
        // Character mặc định luôn được unlock
        unlockedCharacters.Add(defaultCharacterName);

        // Load từ PlayerPrefs
        string unlockedString = PlayerPrefs.GetString("UnlockedCharacters", defaultCharacterName);
        if (!string.IsNullOrEmpty(unlockedString))
        {
            string[] unlocked = unlockedString.Split(',');
            foreach (string charName in unlocked)
            {
                if (!string.IsNullOrEmpty(charName) && !unlockedCharacters.Contains(charName))
                {
                    unlockedCharacters.Add(charName);
                }
            }
        }

        // Lọc chỉ giữ lại các character có sẵn
        unlockedCharacters = unlockedCharacters.Where(name => availableCharacterNames.Contains(name)).ToList();

        // Load character đã chọn
        selectedCharacterName = PlayerPrefs.GetString("SelectedCharacter", defaultCharacterName);
        if (!unlockedCharacters.Contains(selectedCharacterName))
        {
            selectedCharacterName = defaultCharacterName;
        }
    }

    /// <summary>
    /// Lấy danh sách tất cả characters (bao gồm cả locked)
    /// </summary>
    public List<string> GetAllCharacters()
    {
        return new List<string>(allCharacters);
    }

    /// <summary>
    /// Lấy danh sách characters đã unlock
    /// </summary>
    public List<string> GetUnlockedCharacters()
    {
        return new List<string>(unlockedCharacters);
    }

    /// <summary>
    /// Lấy index của character trong allCharacters
    /// </summary>
    public int GetCharacterIndex(string characterName)
    {
        return allCharacters.IndexOf(characterName);
    }

    /// <summary>
    /// Lấy character name tại index
    /// </summary>
    public string GetCharacterByIndex(int index)
    {
        if (index < 0 || index >= allCharacters.Count)
        {
            return null;
        }
        return allCharacters[index];
    }

    /// <summary>
    /// Lấy character tiếp theo
    /// </summary>
    public string GetNextCharacter(string currentCharacter)
    {
        int currentIndex = allCharacters.IndexOf(currentCharacter);
        if (currentIndex < 0) currentIndex = 0;
        int nextIndex = (currentIndex + 1) % allCharacters.Count;
        return allCharacters[nextIndex];
    }

    /// <summary>
    /// Lấy character trước đó
    /// </summary>
    public string GetPreviousCharacter(string currentCharacter)
    {
        int currentIndex = allCharacters.IndexOf(currentCharacter);
        if (currentIndex < 0) currentIndex = 0;
        int prevIndex = (currentIndex - 1 + allCharacters.Count) % allCharacters.Count;
        return allCharacters[prevIndex];
    }

    /// <summary>
    /// Lấy giá mua của character
    /// </summary>
    public int GetCharacterPrice(string characterName)
    {
        if (characterPrices != null)
        {
            CharacterPrice priceEntry = characterPrices.FirstOrDefault(p => p.characterName == characterName);
            if (priceEntry != null)
            {
                return priceEntry.price;
            }
        }
        return defaultPrice;
    }

    /// <summary>
    /// Kiểm tra character đã được unlock chưa
    /// </summary>
    public bool IsCharacterUnlocked(string characterName)
    {
        return unlockedCharacters.Contains(characterName);
    }

    /// <summary>
    /// Unlock một character (khi mua)
    /// </summary>
    public void UnlockCharacter(string characterName)
    {
        if (!unlockedCharacters.Contains(characterName) && availableCharacterNames.Contains(characterName))
        {
            unlockedCharacters.Add(characterName);
            SaveUnlockedCharacters();
            Debug.Log($"CharacterSelectManager: Đã unlock character {characterName}");
        }
    }

    /// <summary>
    /// Mua character
    /// </summary>
    public bool BuyCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return false;

        // Kiểm tra đã unlock chưa
        if (IsCharacterUnlocked(characterName))
        {
            Debug.LogWarning($"CharacterSelectManager: Character {characterName} đã được unlock!");
            return false;
        }

        // Lấy giá mua
        int price = GetCharacterPrice(characterName);

        // Kiểm tra gold
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.playerData == null)
        {
            Debug.LogWarning("CharacterSelectManager: PlayerDataManager không tồn tại!");
            return false;
        }

        int currentGold = PlayerDataManager.Instance.playerData.totalReward;
        if (currentGold < price)
        {
            Debug.Log($"CharacterSelectManager: Không đủ gold! Cần {price}, hiện có {currentGold}");
            return false;
        }

        // Trừ gold và unlock character
        PlayerDataManager.Instance.playerData.totalReward -= price;
        PlayerDataManager.Instance.Save();

        // Unlock character
        UnlockCharacter(characterName);

        Debug.Log($"CharacterSelectManager: Đã mua character {characterName} với giá {price}. Gold còn lại: {PlayerDataManager.Instance.playerData.totalReward}");
        return true;
    }

    /// <summary>
    /// Chọn character
    /// </summary>
    public bool SelectCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return false;

        // Kiểm tra đã unlock chưa
        if (!IsCharacterUnlocked(characterName))
        {
            Debug.LogWarning($"CharacterSelectManager: Character {characterName} chưa được unlock!");
            return false;
        }

        // Disable input của character đang spawn (nếu có)
        DisableCharacterInput();

        // Chọn character
        selectedCharacterName = characterName;
        PlayerPrefs.SetString("SelectedCharacter", selectedCharacterName);
        PlayerPrefs.Save();

        Debug.Log($"CharacterSelectManager: Đã chọn character {selectedCharacterName}");
        return true;
    }
    
    /// <summary>
    /// Disable input của character đang được spawn
    /// </summary>
    private void DisableCharacterInput()
    {
        // Disable input từ InputManager
        if (InputManager.Instance != null)
        {
            InputManager.Instance.DisablePlayerInput();
        }

        // Disable input từ character đang spawn (nếu có)
        if (currentSpawnedCharacter != null)
        {
            PlayerController playerController = currentSpawnedCharacter.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetDisable(true);
            }
            else
            {
                // Tìm PlayerController trong children
                playerController = currentSpawnedCharacter.GetComponentInChildren<PlayerController>();
                if (playerController != null)
                {
                    playerController.SetDisable(true);
                }
            }
        }
    }

    /// <summary>
    /// Lấy tên character hiện tại đang được chọn
    /// </summary>
    public string GetSelectedCharacterName()
    {
        return selectedCharacterName;
    }

    /// <summary>
    /// Kiểm tra character có được chọn không
    /// </summary>
    public bool IsCharacterSelected(string characterName)
    {
        return selectedCharacterName == characterName;
    }

    /// <summary>
    /// Lưu danh sách character đã unlock
    /// </summary>
    private void SaveUnlockedCharacters()
    {
        string unlockedString = string.Join(",", unlockedCharacters);
        PlayerPrefs.SetString("UnlockedCharacters", unlockedString);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load character prefab từ Resources
    /// </summary>
    public GameObject LoadCharacterPrefab(string characterName)
    {
        string resourcePath = $"{characterResourcePath}/{characterName}";
        GameObject characterPrefab = Resources.Load<GameObject>(resourcePath);
        
        if (characterPrefab == null)
        {
            Debug.LogWarning($"CharacterSelectManager: Không tìm thấy prefab {characterName} tại path: Resources/{resourcePath}!");
        }
        
        return characterPrefab;
    }

    /// <summary>
    /// Lấy CharacterSpawnPoint transform
    /// </summary>
    public Transform GetCharacterSpawnPoint()
    {
        return characterSpawnPoint;
    }

    /// <summary>
    /// Spawn nhân vật đã chọn
    /// </summary>
    public void SpawnSelectedCharacter()
    {
        if (string.IsNullOrEmpty(selectedCharacterName))
        {
            Debug.LogWarning("CharacterSelectManager: Không có character nào được chọn!");
            return;
        }

        if (characterSpawnPoint == null)
        {
            Debug.LogWarning("CharacterSelectManager: characterSpawnPoint chưa được gán! Không thể spawn character.");
            return;
        }

        // Xóa character cũ nếu có
        if (currentSpawnedCharacter != null)
        {
            DestroyImmediate(currentSpawnedCharacter);
            currentSpawnedCharacter = null;
        }

        // Load và spawn character
        GameObject characterPrefab = LoadCharacterPrefab(selectedCharacterName);
        if (characterPrefab == null)
        {
            Debug.LogWarning($"CharacterSelectManager: Không thể load prefab cho character {selectedCharacterName}!");
            return;
        }

        // Đảm bảo characterSpawnPoint được active
        if (!characterSpawnPoint.gameObject.activeSelf)
        {
            characterSpawnPoint.gameObject.SetActive(true);
        }

        // Spawn character
        currentSpawnedCharacter = Instantiate(characterPrefab, characterSpawnPoint);
        
        if (currentSpawnedCharacter == null)
        {
            Debug.LogError($"CharacterSelectManager: Không thể spawn character {selectedCharacterName}!");
            return;
        }

        // Set transform properties (giữ nguyên scale của prefab)
        currentSpawnedCharacter.transform.localPosition = Vector3.zero;
        currentSpawnedCharacter.transform.localRotation = Quaternion.identity;
        
        // Đảm bảo character được active
        currentSpawnedCharacter.SetActive(true);
        
        // Đặt tên để dễ debug
        currentSpawnedCharacter.name = $"Character_{selectedCharacterName}";

        // Disable input của character khi spawn trong character select screen
        DisableCharacterInput();

        Debug.Log($"CharacterSelectManager: Đã spawn character {selectedCharacterName} tại characterSpawnPoint.");
    }

    private void OnDestroy()
    {
        // Cleanup
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }
    }
}


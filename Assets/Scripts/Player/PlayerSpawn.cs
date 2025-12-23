using UnityEngine;

/// <summary>
/// Spawn player character trong gameplay dựa trên character đã chọn
/// </summary>
public class PlayerSpawn : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Vị trí spawn player (nếu null sẽ dùng vị trí của GameObject này)")]
    public Transform spawnPoint;

    [Tooltip("Tự động spawn khi Start")]
    public bool spawnOnStart = true;

    [Header("Character Settings")]
    [Tooltip("Đường dẫn trong Resources folder (ví dụ: 'Characters' hoặc 'Players')")]
    public string characterResourcePath = "Characters";
    
    [Tooltip("Tên character mặc định (nếu không có character nào được chọn)")]
    public string defaultCharacterName = "Player";

    private GameObject spawnedPlayer;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnPlayer();
        }
    }

    /// <summary>
    /// Spawn player character dựa trên character đã chọn (load từ PlayerPrefs)
    /// </summary>
    public void SpawnPlayer()
    {
        // Load tên character đã chọn từ PlayerPrefs
        string selectedCharacterName = PlayerPrefs.GetString("SelectedCharacter", defaultCharacterName);
        if (string.IsNullOrEmpty(selectedCharacterName))
        {
            Debug.LogWarning("PlayerSpawn: Không có character nào được chọn! Sử dụng character mặc định.");
            selectedCharacterName = defaultCharacterName;
        }

        // Xóa player cũ nếu có
        if (spawnedPlayer != null)
        {
            DestroyImmediate(spawnedPlayer);
            spawnedPlayer = null;
        }

        // Load character prefab từ Resources
        GameObject characterPrefab = LoadCharacterPrefab(selectedCharacterName);
        if (characterPrefab == null)
        {
            Debug.LogError($"PlayerSpawn: Không thể load prefab cho character {selectedCharacterName}!");
            return;
        }

        // Xác định vị trí spawn
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        
        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }
        else
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        // Spawn player
        spawnedPlayer = Instantiate(characterPrefab, spawnPosition, spawnRotation);
        
        if (spawnedPlayer == null)
        {
            Debug.LogError($"PlayerSpawn: Không thể spawn player {selectedCharacterName}!");
            return;
        }

        // Đặt tên và tag để dễ tìm
        spawnedPlayer.name = $"Player_{selectedCharacterName}";
        spawnedPlayer.tag = "Player";

        // Enable input cho player
        EnablePlayerInput(spawnedPlayer);

        Debug.Log($"PlayerSpawn: Đã spawn player {selectedCharacterName} tại vị trí {spawnPosition}");
    }
    
    /// <summary>
    /// Enable input cho player đã spawn
    /// </summary>
    private void EnablePlayerInput(GameObject player)
    {
        if (player == null) return;

        // Tìm PlayerController component
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Enable input cho player
            playerController.SetCanReceiveInput(true);
            playerController.SetDisable(false);
            Debug.Log("PlayerSpawn: Đã enable input cho player");
        }
        else
        {
            Debug.LogWarning("PlayerSpawn: Không tìm thấy PlayerController component trong player!");
        }

        // Enable InputManager nếu có
        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnablePlayerInput();
        }
    }

    /// <summary>
    /// Xóa player đã spawn
    /// </summary>
    public void DestroyPlayer()
    {
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer);
            spawnedPlayer = null;
        }
    }

    /// <summary>
    /// Load character prefab từ Resources
    /// </summary>
    private GameObject LoadCharacterPrefab(string characterName)
    {
        string resourcePath = $"{characterResourcePath}/{characterName}";
        GameObject characterPrefab = Resources.Load<GameObject>(resourcePath);
        
        if (characterPrefab == null)
        {
            Debug.LogWarning($"PlayerSpawn: Không tìm thấy prefab {characterName} tại path: Resources/{resourcePath}!");
        }
        
        return characterPrefab;
    }

    private void OnDestroy()
    {
        // Cleanup
        if (spawnedPlayer != null)
        {
            Destroy(spawnedPlayer);
        }
    }
}


using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý UI cho Character Selection Panel
/// </summary>
public class CharacterSelectPanel : MonoBehaviour
{
    [Header("UI References")]
    public Button nextBtn;
    public Button backBtn;
    public Button buyBtn;
    public Button selectBtn;
    public TextMeshProUGUI priceText; // Hiển thị giá mua

    public Button homeBtn;

    private CharacterSelectManager manager;
    private GameObject currentCharacterInstance;
    private string currentDisplayedCharacter; // Character đang được hiển thị (có thể là locked)

    private void Start()
    {
        // Setup home button
        if (homeBtn != null)
        {
            homeBtn.onClick.AddListener(OnHomeButtonClicked);
        }

        // Lấy reference đến manager
        manager = CharacterSelectManager.Instance;
        if (manager == null)
        {
            Debug.LogError("CharacterSelectPanel: Không tìm thấy CharacterSelectManager!");
            return;
        }

        SetupButtons();
        
        // Hiển thị character đã chọn hoặc character đầu tiên
        string selectedName = manager.GetSelectedCharacterName();
        var allCharacters = manager.GetAllCharacters();
        
        if (!string.IsNullOrEmpty(selectedName) && allCharacters.Contains(selectedName))
        {
            ShowCharacterByName(selectedName);
        }
        else if (allCharacters.Count > 0)
        {
            ShowCharacterByName(allCharacters[0]);
        }
    }

    void OnHomeButtonClicked()
    {
        // Quay lại hiển thị character đang được select trước khi về home
        if (manager != null)
        {
            string selectedCharacterName = manager.GetSelectedCharacterName();
            if (!string.IsNullOrEmpty(selectedCharacterName))
            {
                ShowCharacterByName(selectedCharacterName);
            }
        }
        
        UIManager.Instance.ShowHomePanel(true);
        UIManager.Instance.ShowCharacterSelectPanel(false);
        AudioManager.Instance.PlayCloseSound();
    }

    private void OnEnable()
    {
        // Đảm bảo manager được khởi tạo
        if (manager == null)
        {
            manager = CharacterSelectManager.Instance;
            if (manager == null)
            {
                Debug.LogError("CharacterSelectPanel: Không tìm thấy CharacterSelectManager!");
                return;
            }
        }

        // Setup buttons lại để đảm bảo listeners hoạt động
        SetupButtons();
        
        // Refresh UI khi panel được hiển thị
        var allCharacters = manager.GetAllCharacters();
        if (allCharacters.Count > 0)
        {
            // Nếu currentDisplayedCharacter chưa được set hoặc không hợp lệ, khởi tạo lại
            if (string.IsNullOrEmpty(currentDisplayedCharacter) || !allCharacters.Contains(currentDisplayedCharacter))
            {
                // Hiển thị character đã chọn hoặc character đầu tiên
                string selectedName = manager.GetSelectedCharacterName();
                
                if (!string.IsNullOrEmpty(selectedName) && allCharacters.Contains(selectedName))
                {
                    ShowCharacterByName(selectedName);
                }
                else
                {
                    ShowCharacterByName(allCharacters[0]);
                }
            }
            else
            {
                // Nếu đã có character được hiển thị, chỉ cần update button states
                UpdateButtonStates();
            }
        }
    }

    /// <summary>
    /// Setup các nút Next, Back, Buy và Select
    /// </summary>
    private void SetupButtons()
    {
        // Remove listeners cũ trước khi add mới để tránh duplicate
        if (nextBtn != null)
        {
            nextBtn.onClick.RemoveAllListeners();
            nextBtn.onClick.AddListener(OnNextButtonClicked);
        }
        if (backBtn != null)
        {
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(OnBackButtonClicked);
        }
        if (buyBtn != null)
        {
            buyBtn.onClick.RemoveAllListeners();
            buyBtn.onClick.AddListener(OnBuyButtonClicked);
        }
        if (selectBtn != null)
        {
            selectBtn.onClick.RemoveAllListeners();
            selectBtn.onClick.AddListener(OnSelectButtonClicked);
        }
        if (homeBtn != null)
        {
            homeBtn.onClick.RemoveAllListeners();
            homeBtn.onClick.AddListener(OnHomeButtonClicked);
        }
    }

    /// <summary>
    /// Xử lý khi nhấn nút Next
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (manager == null || string.IsNullOrEmpty(currentDisplayedCharacter)) return;

        string nextCharacter = manager.GetNextCharacter(currentDisplayedCharacter);
        if (!string.IsNullOrEmpty(nextCharacter))
        {
            ShowCharacterByName(nextCharacter);
            AudioManager.Instance?.PlayChangeSound();
        }
    }

    /// <summary>
    /// Xử lý khi nhấn nút Back
    /// </summary>
    private void OnBackButtonClicked()
    {
        if (manager == null || string.IsNullOrEmpty(currentDisplayedCharacter)) return;

        string prevCharacter = manager.GetPreviousCharacter(currentDisplayedCharacter);
        if (!string.IsNullOrEmpty(prevCharacter))
        {
            ShowCharacterByName(prevCharacter);
            AudioManager.Instance?.PlayChangeSound();
        }
    }

    /// <summary>
    /// Xử lý khi nhấn nút Buy
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (manager == null || string.IsNullOrEmpty(currentDisplayedCharacter)) return;

        // Gọi manager để mua character
        bool success = manager.BuyCharacter(currentDisplayedCharacter);
        
        if (success)
        {
            // Cập nhật UI
            UpdateButtonStates();
            
            // Thông báo thành công
            if (UIManager.Instance != null && UIManager.Instance.noticePanel != null)
            {
                UIManager.Instance.noticePanel.Init($"Purchased successfully!");
            }
            AudioManager.Instance?.PlaySuccessSound();
        }
        else
        {
            // Kiểm tra lý do thất bại
            int price = manager.GetCharacterPrice(currentDisplayedCharacter);
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.playerData != null)
            {
                int currentGold = PlayerDataManager.Instance.playerData.totalReward;
                if (currentGold < price)
                {
                    // Không đủ gold
                    if (UIManager.Instance != null && UIManager.Instance.noticePanel != null)
                    {
                        UIManager.Instance.noticePanel.Init("Not enough gold!");
                    }
                    AudioManager.Instance?.PlayFailSound();
                }
            }
        }
    }

    /// <summary>
    /// Xử lý khi nhấn nút Select
    /// </summary>
    private void OnSelectButtonClicked()
    {
        if (manager == null || string.IsNullOrEmpty(currentDisplayedCharacter)) return;

        // Gọi manager để chọn character
        bool success = manager.SelectCharacter(currentDisplayedCharacter);
        
        if (success)
        {
            // Cập nhật UI để ẩn nút Select
            UpdateButtonStates();

            // Hiển thị popup success
            ShowSuccessPopup();

            AudioManager.Instance?.PlaySelectSound();
        }
    }

    /// <summary>
    /// Hiển thị character theo tên
    /// </summary>
    private void ShowCharacterByName(string characterName)
    {
        if (string.IsNullOrEmpty(characterName) || manager == null)
        {
            Debug.LogWarning("CharacterSelectPanel: Character name không hợp lệ hoặc manager null!");
            return;
        }

        currentDisplayedCharacter = characterName;

        // Lấy characterSpawnPoint từ manager
        Transform spawnPoint = manager.GetCharacterSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogError("CharacterSelectPanel: characterSpawnPoint là null! Không thể tạo character.");
            return;
        }

        // Xóa tất cả children của spawnPoint (bao gồm cả character đã spawn bởi Manager và Panel)
        for (int i = spawnPoint.childCount - 1; i >= 0; i--)
        {
            Transform child = spawnPoint.GetChild(i);
            if (child != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Reset reference
        currentCharacterInstance = null;

        // Load character prefab từ manager
        GameObject characterPrefab = manager.LoadCharacterPrefab(characterName);
        
        if (characterPrefab == null)
        {
            return;
        }

        // Đảm bảo characterSpawnPoint được active
        if (!spawnPoint.gameObject.activeSelf)
        {
            spawnPoint.gameObject.SetActive(true);
            Debug.LogWarning("CharacterSelectPanel: characterSpawnPoint đã bị inactive, đã kích hoạt lại!");
        }

        // Tạo character mới
        currentCharacterInstance = Instantiate(characterPrefab, spawnPoint);
        
        if (currentCharacterInstance == null)
        {
            Debug.LogError($"CharacterSelectPanel: Không thể tạo character {characterName}!");
            return;
        }

        // Set transform properties (giữ nguyên scale của prefab)
        currentCharacterInstance.transform.localPosition = Vector3.zero;
        currentCharacterInstance.transform.localRotation = Quaternion.identity;
        
        // Đảm bảo character được active
        currentCharacterInstance.SetActive(true);
        
        // Đặt tên để dễ debug
        currentCharacterInstance.name = $"Character_{characterName}";
        
        // Kiểm tra và log thông tin
        Debug.Log($"CharacterSelectPanel: Đã tạo character {characterName}. " +
                 $"Name: {currentCharacterInstance.name}, " +
                 $"Position: {currentCharacterInstance.transform.position}, " +
                 $"LocalPosition: {currentCharacterInstance.transform.localPosition}, " +
                 $"Scale: {currentCharacterInstance.transform.localScale}, " +
                 $"Active: {currentCharacterInstance.activeSelf}, " +
                 $"Parent: {(currentCharacterInstance.transform.parent != null ? currentCharacterInstance.transform.parent.name : "null")}, " +
                 $"ChildCount của parent: {(currentCharacterInstance.transform.parent != null ? currentCharacterInstance.transform.parent.childCount : 0)}");

        // Cập nhật UI buttons
        UpdateButtonStates();
    }

    /// <summary>
    /// Cập nhật trạng thái các nút Buy và Select
    /// </summary>
    private void UpdateButtonStates()
    {
        if (manager == null || string.IsNullOrEmpty(currentDisplayedCharacter)) return;

        bool isUnlocked = manager.IsCharacterUnlocked(currentDisplayedCharacter);
        bool isSelected = manager.IsCharacterSelected(currentDisplayedCharacter);

        // Hiển thị/ẩn nút Buy và Select
        if (buyBtn != null)
        {
            buyBtn.gameObject.SetActive(!isUnlocked);
        }
        if (selectBtn != null)
        {
            // Chỉ hiển thị Select nếu đã unlock và chưa được chọn
            selectBtn.gameObject.SetActive(isUnlocked && !isSelected);
        }

        // Cập nhật giá hiển thị
        if (priceText != null)
        {
            if (isUnlocked)
            {
                priceText.text = "";
            }
            else
            {
                int price = manager.GetCharacterPrice(currentDisplayedCharacter);
                priceText.text = price.ToString();
            }
        }
    }

    /// <summary>
    /// Hiển thị popup success khi chọn nhân vật
    /// </summary>
    private void ShowSuccessPopup()
    {
        if (UIManager.Instance != null && UIManager.Instance.noticePanel != null)
        {
            UIManager.Instance.noticePanel.Init($"Character selected successfully!");
        }
        else
        {
            Debug.LogWarning("CharacterSelectPanel: NoticePanel không tồn tại!");
        }
    }

    private void OnDestroy()
    {
        // Cleanup
        if (currentCharacterInstance != null)
        {
            Destroy(currentCharacterInstance);
        }
    }
}

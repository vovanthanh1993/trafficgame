using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingPanel : MonoBehaviour
{
    public Button homeBtn;
    public Button closeBtn;
    
    [Header("SFX Settings")]
    [Tooltip("Slider để điều chỉnh SFX volume")]
    public Slider sfxSlider;
    
    [Header("Music Settings")]
    [Tooltip("Slider để điều chỉnh Music volume")]
    public Slider musicSlider;
    
    private float currentSFXVolume = 1f;
    private float currentMusicVolume = 1f;

    private void OnEnable() {
        if(SceneManager.GetActiveScene().name == "HomeScene") 
            homeBtn.gameObject.SetActive(false);
        else homeBtn.gameObject.SetActive(true);
        
        // Đảm bảo Slider được enable và interactable
        if (sfxSlider != null)
        {
            sfxSlider.interactable = true;
            sfxSlider.enabled = true;
        }
        if (musicSlider != null)
        {
            musicSlider.interactable = true;
            musicSlider.enabled = true;
        }
        
        // Load volume từ PlayerPrefs hoặc dùng giá trị mặc định
        LoadVolumeSettings();
        UpdateUI();
    }

    void Start() {
        homeBtn.onClick.AddListener(OnHomeButtonClicked);   
        closeBtn.onClick.AddListener(OnCloseButtonClicked);
        
        // SFX Slider
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.interactable = true;
            sfxSlider.enabled = true;
            // Xóa listener cũ nếu có để tránh duplicate
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSFXValueChanged);
        }
        
        // Music Slider
        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.interactable = true;
            musicSlider.enabled = true;
            // Xóa listener cũ nếu có để tránh duplicate
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(OnMusicValueChanged);
        }
    }

    public void OnHomeButtonClicked(){
        GameCommonUtils.LoadScene("HomeScene");
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        UIManager.Instance.ShowHomePanel(true);
    }

    public void OnCloseButtonClicked(){
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
    
    #region SFX Volume Control
    
    private void OnSFXValueChanged(float value)
    {
        currentSFXVolume = value;
        ApplySFXVolume();
        SaveVolumeSettings();
        
        // Phát sound khi điều chỉnh (chỉ khi người dùng thả tay)
        // Có thể thêm logic để chỉ phát khi drag end nếu cần
    }
    
    private void ApplySFXVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(currentSFXVolume);
        }
    }
    
    #endregion
    
    #region Music Volume Control
    
    private void OnMusicValueChanged(float value)
    {
        currentMusicVolume = value;
        ApplyMusicVolume();
        SaveVolumeSettings();
        
        // Phát sound khi điều chỉnh (chỉ khi người dùng thả tay)
        // Có thể thêm logic để chỉ phát khi drag end nếu cần
    }
    
    private void ApplyMusicVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(currentMusicVolume);
        }
    }
    
    #endregion
    
    #region UI Update
    
    private void UpdateUI()
    {
        // Update SFX Slider
        if (sfxSlider != null)
        {
            sfxSlider.value = currentSFXVolume;
        }
        
        // Update Music Slider
        if (musicSlider != null)
        {
            musicSlider.value = currentMusicVolume;
        }
    }
    
    #endregion
    
    #region Save/Load Settings
    
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("SFXVolume", currentSFXVolume);
        PlayerPrefs.SetFloat("MusicVolume", currentMusicVolume);
        PlayerPrefs.Save();
    }
    
    private void LoadVolumeSettings()
    {
        currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        currentMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        
        // Áp dụng volume ngay khi load
        ApplySFXVolume();
        ApplyMusicVolume();
    }
    
    #endregion
}

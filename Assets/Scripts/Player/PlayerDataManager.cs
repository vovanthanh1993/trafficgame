using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public PlayerData playerData;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerData = PlayerDataStorage.LoadOrCreateDefault();
    }

    /// <summary>
    /// Lưu PlayerData vào file
    /// </summary>
    public void Save()
    {
        if (playerData != null)
        {
            PlayerDataStorage.Save(playerData);
        }
    }
}

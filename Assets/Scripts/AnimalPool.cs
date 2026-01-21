using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class để lưu trữ thông tin pool cho một prefab
/// </summary>
[System.Serializable]
public class AnimalPrefabPool
{
    public GameObject prefab;
    public Queue<GameObject> availableAnimals = new Queue<GameObject>();
    public List<GameObject> allAnimals = new List<GameObject>();
    public Transform poolParent;
}

/// <summary>
/// Object pool để quản lý và tái sử dụng animal objects (hỗ trợ nhiều prefab)
/// </summary>
public class AnimalPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Đường dẫn thư mục trong Resources chứa animal prefabs (ví dụ: 'Prefabs/Animals')")]
    [SerializeField] private string prefabsFolderPath = "Prefabs/Animals";
    
    [Tooltip("Tự động load prefabs từ Resources và tạo pool")]
    [SerializeField] private bool autoLoadPrefabs = true;
    
    [Tooltip("Số lượng animal ban đầu cho mỗi prefab trong pool")]
    [SerializeField] private int initialPoolSize = 3;
    
    [Tooltip("Số lượng animal tối đa cho mỗi prefab trong pool (0 = không giới hạn)")]
    [SerializeField] private int maxPoolSize = 50;
    
    [Tooltip("Tự động mở rộng pool nếu hết animal")]
    [SerializeField] private bool autoExpand = true;
    
    // Dictionary để lưu pool cho từng prefab
    private Dictionary<GameObject, AnimalPrefabPool> prefabPools = new Dictionary<GameObject, AnimalPrefabPool>();
    private Transform mainPoolParent;
    private List<GameObject> loadedPrefabs = new List<GameObject>();
    
    public static AnimalPool Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Tạo parent chính để chứa các pool con
        mainPoolParent = new GameObject("AnimalPool").transform;
        mainPoolParent.SetParent(transform);
    }
    
    void Start()
    {
        // Tự động load prefabs từ Resources nếu được bật
        if (autoLoadPrefabs)
        {
            LoadPrefabsFromResources();
        }
    }
    
    /// <summary>
    /// Load tất cả prefabs từ Resources folder và tạo pool cho mỗi prefab
    /// </summary>
    private void LoadPrefabsFromResources()
    {
        if (string.IsNullOrEmpty(prefabsFolderPath))
        {
            Debug.LogWarning("AnimalPool: prefabsFolderPath không được set!");
            return;
        }
        
        // Load tất cả GameObject từ Resources/Prefabs/Animals
        GameObject[] loadedPrefabsArray = Resources.LoadAll<GameObject>(prefabsFolderPath);
        
        if (loadedPrefabsArray == null || loadedPrefabsArray.Length == 0)
        {
            Debug.LogWarning($"AnimalPool: Không tìm thấy prefab nào trong Resources/{prefabsFolderPath}!");
            return;
        }
        
        // Lọc các prefab có AnimalController component
        foreach (GameObject prefab in loadedPrefabsArray)
        {
            if (prefab == null) continue;
            
            // Kiểm tra xem prefab có AnimalController component không
            AnimalController animalController = prefab.GetComponent<AnimalController>();
            if (animalController != null)
            {
                loadedPrefabs.Add(prefab);
                // Tạo pool cho prefab này
                GetOrCreatePool(prefab);
                Debug.Log($"AnimalPool: Đã load và tạo pool cho prefab '{prefab.name}'");
            }
        }
        
        Debug.Log($"AnimalPool: Đã load {loadedPrefabs.Count} animal prefabs từ Resources/{prefabsFolderPath}");
    }
    
    /// <summary>
    /// Lấy hoặc tạo pool cho một prefab
    /// </summary>
    private AnimalPrefabPool GetOrCreatePool(GameObject prefab)
    {
        if (prefab == null)
            return null;
        
        // Nếu đã có pool cho prefab này, trả về
        if (prefabPools.ContainsKey(prefab))
        {
            return prefabPools[prefab];
        }
        
        // Tạo pool mới cho prefab này
        AnimalPrefabPool newPool = new AnimalPrefabPool();
        newPool.prefab = prefab;
        newPool.poolParent = new GameObject($"Pool_{prefab.name}").transform;
        newPool.poolParent.SetParent(mainPoolParent);
        
        prefabPools[prefab] = newPool;
        
        // Khởi tạo pool với số lượng ban đầu
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAnimal(newPool);
        }
        
        Debug.Log($"AnimalPool: Đã tạo pool mới cho prefab '{prefab.name}' với {initialPoolSize} animals");
        
        return newPool;
    }
    
    /// <summary>
    /// Tạo một animal mới và thêm vào pool
    /// </summary>
    private GameObject CreateNewAnimal(AnimalPrefabPool pool)
    {
        if (pool == null || pool.prefab == null)
            return null;
        
        GameObject animal = Instantiate(pool.prefab, pool.poolParent);
        animal.SetActive(false);
        animal.name = $"{pool.prefab.name}_{pool.allAnimals.Count}";
        
        pool.allAnimals.Add(animal);
        pool.availableAnimals.Enqueue(animal);
        
        return animal;
    }
    
    /// <summary>
    /// Lấy một animal từ pool dựa trên prefab
    /// </summary>
    public GameObject GetAnimal(GameObject prefab)
    {
        if (prefab == null)
            return null;
        
        AnimalPrefabPool pool = GetOrCreatePool(prefab);
        if (pool == null)
            return null;
        
        GameObject animal = null;
        
        // Nếu còn animal trong pool, lấy ra
        if (pool.availableAnimals.Count > 0)
        {
            animal = pool.availableAnimals.Dequeue();
        }
        // Nếu hết animal và cho phép auto expand, tạo mới
        else if (autoExpand && (maxPoolSize <= 0 || pool.allAnimals.Count < maxPoolSize))
        {
            animal = CreateNewAnimal(pool);
        }
        
        if (animal != null)
        {
            animal.SetActive(true);
        }
        
        return animal;
    }
    
    /// <summary>
    /// Lấy một animal từ pool (dùng prefab đầu tiên nếu có nhiều prefab)
    /// </summary>
    public GameObject GetAnimal()
    {
        // Nếu có prefab nào đó trong dictionary, dùng prefab đầu tiên
        if (prefabPools.Count > 0)
        {
            foreach (var kvp in prefabPools)
            {
                return GetAnimal(kvp.Key);
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Trả animal về pool (tự động tìm pool phù hợp dựa trên tên hoặc prefab)
    /// </summary>
    public void ReturnAnimal(GameObject animal)
    {
        if (animal == null)
            return;
        
        // Tìm pool chứa animal này
        AnimalPrefabPool pool = FindPoolForAnimal(animal);
        if (pool == null)
        {
            Debug.LogWarning($"AnimalPool: Không tìm thấy pool cho animal '{animal.name}'");
            return;
        }
        
        // Reset animal về trạng thái ban đầu
        animal.SetActive(false);
        animal.transform.SetParent(pool.poolParent);
        animal.transform.position = Vector3.zero;
        animal.transform.rotation = Quaternion.identity;
        
        // Reset AnimalController về trạng thái ban đầu
        AnimalController animalController = animal.GetComponent<AnimalController>();
        if (animalController != null)
        {
            animalController.ResetAnimal();
        }
        
        // Thêm lại vào queue
        if (!pool.availableAnimals.Contains(animal))
        {
            pool.availableAnimals.Enqueue(animal);
        }
    }
    
    /// <summary>
    /// Tìm pool chứa animal này
    /// </summary>
    private AnimalPrefabPool FindPoolForAnimal(GameObject animal)
    {
        // Tìm trong tất cả pools
        foreach (var kvp in prefabPools)
        {
            if (kvp.Value.allAnimals.Contains(animal))
            {
                return kvp.Value;
            }
        }
        
        // Nếu không tìm thấy, thử tìm dựa trên tên animal (tên animal thường có format: PrefabName_Index)
        string animalName = animal.name;
        foreach (var kvp in prefabPools)
        {
            if (animalName.StartsWith(kvp.Key.name))
            {
                return kvp.Value;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Lấy số lượng animal đang sử dụng (tổng của tất cả pools)
    /// </summary>
    public int GetActiveAnimalCount()
    {
        int total = 0;
        foreach (var pool in prefabPools.Values)
        {
            total += pool.allAnimals.Count - pool.availableAnimals.Count;
        }
        return total;
    }
    
    /// <summary>
    /// Lấy số lượng animal có sẵn trong pool (tổng của tất cả pools)
    /// </summary>
    public int GetAvailableAnimalCount()
    {
        int total = 0;
        foreach (var pool in prefabPools.Values)
        {
            total += pool.availableAnimals.Count;
        }
        return total;
    }
    
    /// <summary>
    /// Lấy số lượng animal đang sử dụng cho một prefab cụ thể
    /// </summary>
    public int GetActiveAnimalCount(GameObject prefab)
    {
        if (prefab == null || !prefabPools.ContainsKey(prefab))
            return 0;
        
        AnimalPrefabPool pool = prefabPools[prefab];
        return pool.allAnimals.Count - pool.availableAnimals.Count;
    }
    
    /// <summary>
    /// Lấy số lượng animal có sẵn cho một prefab cụ thể
    /// </summary>
    public int GetAvailableAnimalCount(GameObject prefab)
    {
        if (prefab == null || !prefabPools.ContainsKey(prefab))
            return 0;
        
        return prefabPools[prefab].availableAnimals.Count;
    }
    
    /// <summary>
    /// Xóa tất cả animals trong pool
    /// </summary>
    public void ClearPool()
    {
        foreach (var pool in prefabPools.Values)
        {
            while (pool.availableAnimals.Count > 0)
            {
                GameObject animal = pool.availableAnimals.Dequeue();
                if (animal != null)
                {
                    Destroy(animal);
                }
            }
            
            foreach (GameObject animal in pool.allAnimals)
            {
                if (animal != null)
                {
                    Destroy(animal);
                }
            }
            
            if (pool.poolParent != null)
            {
                Destroy(pool.poolParent.gameObject);
            }
        }
        
        prefabPools.Clear();
    }
    
    /// <summary>
    /// Xóa pool cho một prefab cụ thể
    /// </summary>
    public void ClearPool(GameObject prefab)
    {
        if (prefab == null || !prefabPools.ContainsKey(prefab))
            return;
        
        AnimalPrefabPool pool = prefabPools[prefab];
        
        while (pool.availableAnimals.Count > 0)
        {
            GameObject animal = pool.availableAnimals.Dequeue();
            if (animal != null)
            {
                Destroy(animal);
            }
        }
        
        foreach (GameObject animal in pool.allAnimals)
        {
            if (animal != null)
            {
                Destroy(animal);
            }
        }
        
        if (pool.poolParent != null)
        {
            Destroy(pool.poolParent.gameObject);
        }
        
        prefabPools.Remove(prefab);
    }
    
    /// <summary>
    /// Lấy tất cả prefabs đang có trong pool
    /// </summary>
    public List<GameObject> GetAllPrefabs()
    {
        return new List<GameObject>(prefabPools.Keys);
    }
}

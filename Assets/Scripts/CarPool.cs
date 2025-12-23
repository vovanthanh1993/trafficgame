using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class để lưu trữ thông tin pool cho một prefab
/// </summary>
[System.Serializable]
public class PrefabPool
{
    public GameObject prefab;
    public Queue<GameObject> availableCars = new Queue<GameObject>();
    public List<GameObject> allCars = new List<GameObject>();
    public Transform poolParent;
}

/// <summary>
/// Object pool để quản lý và tái sử dụng car objects (hỗ trợ nhiều prefab)
/// </summary>
public class CarPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Đường dẫn thư mục trong Resources chứa car prefabs (ví dụ: 'Prefabs/Cars')")]
    [SerializeField] private string prefabsFolderPath = "Prefabs/Cars";
    
    [Tooltip("Tự động load prefabs từ Resources và tạo pool")]
    [SerializeField] private bool autoLoadPrefabs = true;
    
    [Tooltip("Số lượng car ban đầu cho mỗi prefab trong pool")]
    [SerializeField] private int initialPoolSize = 3;
    
    [Tooltip("Số lượng car tối đa cho mỗi prefab trong pool (0 = không giới hạn)")]
    [SerializeField] private int maxPoolSize = 50;
    
    [Tooltip("Tự động mở rộng pool nếu hết car")]
    [SerializeField] private bool autoExpand = true;
    
    // Dictionary để lưu pool cho từng prefab
    private Dictionary<GameObject, PrefabPool> prefabPools = new Dictionary<GameObject, PrefabPool>();
    private Transform mainPoolParent;
    private List<GameObject> loadedPrefabs = new List<GameObject>();
    
    public static CarPool Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Tạo parent chính để chứa các pool con
        mainPoolParent = new GameObject("CarPool").transform;
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
            Debug.LogWarning("CarPool: prefabsFolderPath không được set!");
            return;
        }
        
        // Load tất cả GameObject từ Resources/Prefabs/Cars
        GameObject[] loadedPrefabsArray = Resources.LoadAll<GameObject>(prefabsFolderPath);
        
        if (loadedPrefabsArray == null || loadedPrefabsArray.Length == 0)
        {
            Debug.LogWarning($"CarPool: Không tìm thấy prefab nào trong Resources/{prefabsFolderPath}!");
            return;
        }
        
        // Lọc các prefab có CarController component
        foreach (GameObject prefab in loadedPrefabsArray)
        {
            if (prefab == null) continue;
            
            // Kiểm tra xem prefab có CarController component không
            CarController carController = prefab.GetComponent<CarController>();
            if (carController != null)
            {
                loadedPrefabs.Add(prefab);
                // Tạo pool cho prefab này
                GetOrCreatePool(prefab);
                Debug.Log($"CarPool: Đã load và tạo pool cho prefab '{prefab.name}'");
            }
        }
        
        Debug.Log($"CarPool: Đã load {loadedPrefabs.Count} car prefabs từ Resources/{prefabsFolderPath}");
    }
    
    /// <summary>
    /// Lấy hoặc tạo pool cho một prefab
    /// </summary>
    private PrefabPool GetOrCreatePool(GameObject prefab)
    {
        if (prefab == null)
            return null;
        
        // Nếu đã có pool cho prefab này, trả về
        if (prefabPools.ContainsKey(prefab))
        {
            return prefabPools[prefab];
        }
        
        // Tạo pool mới cho prefab này
        PrefabPool newPool = new PrefabPool();
        newPool.prefab = prefab;
        newPool.poolParent = new GameObject($"Pool_{prefab.name}").transform;
        newPool.poolParent.SetParent(mainPoolParent);
        
        prefabPools[prefab] = newPool;
        
        // Khởi tạo pool với số lượng ban đầu
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewCar(newPool);
        }
        
        Debug.Log($"CarPool: Đã tạo pool mới cho prefab '{prefab.name}' với {initialPoolSize} cars");
        
        return newPool;
    }
    
    /// <summary>
    /// Tạo một car mới và thêm vào pool
    /// </summary>
    private GameObject CreateNewCar(PrefabPool pool)
    {
        if (pool == null || pool.prefab == null)
            return null;
        
        GameObject car = Instantiate(pool.prefab, pool.poolParent);
        car.SetActive(false);
        car.name = $"{pool.prefab.name}_{pool.allCars.Count}";
        
        pool.allCars.Add(car);
        pool.availableCars.Enqueue(car);
        
        return car;
    }
    
    /// <summary>
    /// Lấy một car từ pool dựa trên prefab
    /// </summary>
    public GameObject GetCar(GameObject prefab)
    {
        if (prefab == null)
            return null;
        
        PrefabPool pool = GetOrCreatePool(prefab);
        if (pool == null)
            return null;
        
        GameObject car = null;
        
        // Nếu còn car trong pool, lấy ra
        if (pool.availableCars.Count > 0)
        {
            car = pool.availableCars.Dequeue();
        }
        // Nếu hết car và cho phép auto expand, tạo mới
        else if (autoExpand && (maxPoolSize <= 0 || pool.allCars.Count < maxPoolSize))
        {
            car = CreateNewCar(pool);
        }
        
        if (car != null)
        {
            car.SetActive(true);
        }
        
        return car;
    }
    
    /// <summary>
    /// Lấy một car từ pool (dùng prefab đầu tiên nếu có nhiều prefab)
    /// </summary>
    public GameObject GetCar()
    {
        // Nếu có prefab nào đó trong dictionary, dùng prefab đầu tiên
        if (prefabPools.Count > 0)
        {
            foreach (var kvp in prefabPools)
            {
                return GetCar(kvp.Key);
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Trả car về pool (tự động tìm pool phù hợp dựa trên tên hoặc prefab)
    /// </summary>
    public void ReturnCar(GameObject car)
    {
        if (car == null)
            return;
        
        // Tìm pool chứa car này
        PrefabPool pool = FindPoolForCar(car);
        if (pool == null)
        {
            Debug.LogWarning($"CarPool: Không tìm thấy pool cho car '{car.name}'");
            return;
        }
        
        // Reset car về trạng thái ban đầu
        car.SetActive(false);
        car.transform.SetParent(pool.poolParent);
        car.transform.position = Vector3.zero;
        car.transform.rotation = Quaternion.identity;
        
        // Reset CarController
        CarController carController = car.GetComponent<CarController>();
        if (carController != null)
        {
            carController.SetReverse(false);
        }
        
        // Thêm lại vào queue
        if (!pool.availableCars.Contains(car))
        {
            pool.availableCars.Enqueue(car);
        }
    }
    
    /// <summary>
    /// Tìm pool chứa car này
    /// </summary>
    private PrefabPool FindPoolForCar(GameObject car)
    {
        // Tìm trong tất cả pools
        foreach (var kvp in prefabPools)
        {
            if (kvp.Value.allCars.Contains(car))
            {
                return kvp.Value;
            }
        }
        
        // Nếu không tìm thấy, thử tìm dựa trên tên car (tên car thường có format: PrefabName_Index)
        string carName = car.name;
        foreach (var kvp in prefabPools)
        {
            if (carName.StartsWith(kvp.Key.name))
            {
                return kvp.Value;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Lấy số lượng car đang sử dụng (tổng của tất cả pools)
    /// </summary>
    public int GetActiveCarCount()
    {
        int total = 0;
        foreach (var pool in prefabPools.Values)
        {
            total += pool.allCars.Count - pool.availableCars.Count;
        }
        return total;
    }
    
    /// <summary>
    /// Lấy số lượng car có sẵn trong pool (tổng của tất cả pools)
    /// </summary>
    public int GetAvailableCarCount()
    {
        int total = 0;
        foreach (var pool in prefabPools.Values)
        {
            total += pool.availableCars.Count;
        }
        return total;
    }
    
    /// <summary>
    /// Lấy số lượng car đang sử dụng cho một prefab cụ thể
    /// </summary>
    public int GetActiveCarCount(GameObject prefab)
    {
        if (prefab == null || !prefabPools.ContainsKey(prefab))
            return 0;
        
        PrefabPool pool = prefabPools[prefab];
        return pool.allCars.Count - pool.availableCars.Count;
    }
    
    /// <summary>
    /// Lấy số lượng car có sẵn cho một prefab cụ thể
    /// </summary>
    public int GetAvailableCarCount(GameObject prefab)
    {
        if (prefab == null || !prefabPools.ContainsKey(prefab))
            return 0;
        
        return prefabPools[prefab].availableCars.Count;
    }
    
    /// <summary>
    /// Xóa tất cả cars trong pool
    /// </summary>
    public void ClearPool()
    {
        foreach (var pool in prefabPools.Values)
        {
            while (pool.availableCars.Count > 0)
            {
                GameObject car = pool.availableCars.Dequeue();
                if (car != null)
                {
                    Destroy(car);
                }
            }
            
            foreach (GameObject car in pool.allCars)
            {
                if (car != null)
                {
                    Destroy(car);
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
        
        PrefabPool pool = prefabPools[prefab];
        
        while (pool.availableCars.Count > 0)
        {
            GameObject car = pool.availableCars.Dequeue();
            if (car != null)
            {
                Destroy(car);
            }
        }
        
        foreach (GameObject car in pool.allCars)
        {
            if (car != null)
            {
                Destroy(car);
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


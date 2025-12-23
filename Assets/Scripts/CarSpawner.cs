using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner để spawn cars tại các spawn points
/// </summary>
public class CarSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Tự động load prefabs từ Resources/Prefabs (nếu bật, sẽ bỏ qua carPrefabs list)")]
    [SerializeField] private bool autoLoadFromResources = true;
    
    [Tooltip("Đường dẫn thư mục trong Resources chứa car prefabs (ví dụ: 'Prefabs' hoặc 'Prefabs/Cars')")]
    [SerializeField] private string prefabsFolderPath = "Prefabs/Cars";
    
    [Tooltip("List các car prefabs (chỉ dùng khi autoLoadFromResources = false)")]
    [SerializeField] private List<GameObject> carPrefabs = new List<GameObject>();
    
    [Header("Spawn Points")]
    [Tooltip("GameObject cha chứa các spawn points đi tiến (sẽ tự động lấy tất cả các con)")]
    [SerializeField] private Transform forwardSpawnPointsParent;
    
    [Tooltip("GameObject cha chứa các spawn points đi lùi (sẽ tự động lấy tất cả các con)")]
    [SerializeField] private Transform reverseSpawnPointsParent;
    
    [Header("Spawn Configuration")]
    [Tooltip("Khoảng thời gian giữa mỗi lần spawn (giây)")]
    [SerializeField] private float spawnInterval = 1.5f;
    
    [Tooltip("Chọn ngẫu nhiên prefab cho mỗi xe (nếu có nhiều prefab)")]
    [SerializeField] private bool randomPrefab = true;
    
    [Tooltip("Số lượng xe tối đa trên scene (0 = không giới hạn)")]
    [SerializeField] private int maxCarsOnScene = 0;
    
    [Tooltip("Tự động spawn liên tục")]
    [SerializeField] private bool autoSpawn = true;
    
    [Tooltip("Sử dụng object pool (khuyến nghị)")]
    [SerializeField] private bool usePool = true;
    
    private List<Transform> forwardSpawnPoints = new List<Transform>();
    private List<Transform> reverseSpawnPoints = new List<Transform>();
    private List<GameObject> spawnedCars = new List<GameObject>();
    private float lastSpawnTime = 0f;
    
    void Start()
    {
        InitializeSpawnPoints();
        InitializePrefabList();
        
        // Pool sẽ tự động tạo khi cần (không cần khởi tạo trước)
        
        // Spawn xe đầu tiên ngay lập tức
        if (autoSpawn)
        {
            SpawnSingleCar();
        }
    }
    
    void Update()
    {
        if (autoSpawn)
        {
            // Kiểm tra xem đã đến lúc spawn xe mới chưa
            if (Time.time - lastSpawnTime >= spawnInterval)
            {
                // Kiểm tra số lượng xe tối đa
                if (maxCarsOnScene <= 0 || spawnedCars.Count < maxCarsOnScene)
                {
                    SpawnSingleCar();
                }
            }
        }
        
        // Dọn dẹp danh sách xe đã bị destroy
        CleanupDestroyedCars();
    }
    
    /// <summary>
    /// Khởi tạo danh sách spawn points từ các con của forward và reverse parents
    /// </summary>
    private void InitializeSpawnPoints()
    {
        forwardSpawnPoints.Clear();
        reverseSpawnPoints.Clear();
        
        // Lấy spawn points đi tiến
        if (forwardSpawnPointsParent != null)
        {
            for (int i = 0; i < forwardSpawnPointsParent.childCount; i++)
            {
                Transform child = forwardSpawnPointsParent.GetChild(i);
                if (child != null)
                {
                    forwardSpawnPoints.Add(child);
                }
            }
            Debug.Log($"CarSpawner: Đã tìm thấy {forwardSpawnPoints.Count} spawn points đi tiến");
        }
        else
        {
            Debug.LogWarning("CarSpawner: forwardSpawnPointsParent không được set!");
        }
        
        // Lấy spawn points đi lùi
        if (reverseSpawnPointsParent != null)
        {
            for (int i = 0; i < reverseSpawnPointsParent.childCount; i++)
            {
                Transform child = reverseSpawnPointsParent.GetChild(i);
                if (child != null)
                {
                    reverseSpawnPoints.Add(child);
                }
            }
            Debug.Log($"CarSpawner: Đã tìm thấy {reverseSpawnPoints.Count} spawn points đi lùi");
        }
        else
        {
            Debug.LogWarning("CarSpawner: reverseSpawnPointsParent không được set!");
        }
        
        if (forwardSpawnPoints.Count == 0 && reverseSpawnPoints.Count == 0)
        {
            Debug.LogError("CarSpawner: Không tìm thấy spawn point nào! Vui lòng gán forwardSpawnPointsParent hoặc reverseSpawnPointsParent.");
        }
    }
    
    /// <summary>
    /// Khởi tạo danh sách prefabs từ list hoặc load từ Resources
    /// </summary>
    private void InitializePrefabList()
    {
        carPrefabs.Clear();
        
        if (autoLoadFromResources)
        {
            LoadPrefabsFromResources();
        }
        
        if (carPrefabs.Count == 0)
        {
            Debug.LogWarning("CarSpawner: Không có prefab nào được load! Vui lòng kiểm tra prefabsFolderPath hoặc thêm prefabs vào list.");
        }
        else
        {
            Debug.Log($"CarSpawner: Đã load {carPrefabs.Count} prefabs");
        }
    }
    
    /// <summary>
    /// Load prefabs từ Resources folder
    /// </summary>
    private void LoadPrefabsFromResources()
    {
        if (string.IsNullOrEmpty(prefabsFolderPath))
        {
            Debug.LogWarning("CarSpawner: prefabsFolderPath không được set!");
            return;
        }
        
        // Load tất cả GameObject từ Resources/Prefabs
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(prefabsFolderPath);
        
        if (loadedPrefabs == null || loadedPrefabs.Length == 0)
        {
            Debug.LogWarning($"CarSpawner: Không tìm thấy prefab nào trong Resources/{prefabsFolderPath}!");
            return;
        }
        
        // Lọc các prefab có CarController component (để đảm bảo là car prefab)
        foreach (GameObject prefab in loadedPrefabs)
        {
            if (prefab == null) continue;
            
            // Kiểm tra xem prefab có CarController component không
            CarController carController = prefab.GetComponent<CarController>();
            if (carController != null)
            {
                carPrefabs.Add(prefab);
                Debug.Log($"CarSpawner: Đã load prefab '{prefab.name}'");
            }
        }
    }
    
    /// <summary>
    /// Spawn một xe tại một spawn point ngẫu nhiên
    /// </summary>
    public void SpawnSingleCar()
    {
        if (carPrefabs.Count == 0)
        {
            Debug.LogError("CarSpawner: Không có prefab nào để spawn!");
            return;
        }
        
        // Tạo danh sách tất cả spawn points có sẵn
        List<Transform> allSpawnPoints = new List<Transform>();
        List<bool> spawnPointDirections = new List<bool>(); // true = reverse, false = forward
        
        // Thêm forward spawn points
        foreach (Transform point in forwardSpawnPoints)
        {
            allSpawnPoints.Add(point);
            spawnPointDirections.Add(false); // false = forward
        }
        
        // Thêm reverse spawn points
        foreach (Transform point in reverseSpawnPoints)
        {
            allSpawnPoints.Add(point);
            spawnPointDirections.Add(true); // true = reverse
        }
        
        if (allSpawnPoints.Count == 0)
        {
            Debug.LogError("CarSpawner: Không có spawn point nào!");
            return;
        }
        
        // Chọn spawn point ngẫu nhiên
        int spawnIndex = Random.Range(0, allSpawnPoints.Count);
        Transform selectedSpawnPoint = allSpawnPoints[spawnIndex];
        bool isReverse = spawnPointDirections[spawnIndex];
        
        // Chọn prefab
        GameObject prefabToSpawn = GetRandomPrefab();
        if (prefabToSpawn == null)
        {
            Debug.LogError("CarSpawner: Không thể lấy prefab để spawn!");
            return;
        }
        
        // Spawn car
        Vector3 spawnPosition = selectedSpawnPoint.position;
        // Set rotation: Y = 180 nếu reverse, Y = 0 nếu forward
        float rotationY = isReverse ? 180f : 0f;
        Quaternion spawnRotation = Quaternion.Euler(selectedSpawnPoint.rotation.eulerAngles.x, rotationY, selectedSpawnPoint.rotation.eulerAngles.z);
        
        GameObject car = GetCarFromPoolOrInstantiate(prefabToSpawn, spawnPosition, spawnRotation);
        
        // Đảm bảo car có CarController component
        CarController carController = car.GetComponent<CarController>();
        if (carController == null)
        {
            Debug.LogWarning($"CarSpawner: Prefab '{prefabToSpawn.name}' không có CarController component!");
        }
        else
        {
            // Gán hướng di chuyển cho xe (true = đi lùi, false = đi tiến)
            carController.SetReverse(isReverse);
        }
        
        spawnedCars.Add(car);
        lastSpawnTime = Time.time;
        
        string directionText = isReverse ? "lùi" : "tiến";
        Debug.Log($"CarSpawner: Đã spawn 1 xe đi {directionText} tại spawn point {spawnIndex + 1}. Tổng số xe: {spawnedCars.Count}");
    }
    
    /// <summary>
    /// Spawn cars tại tất cả các spawn points (giữ lại để tương thích)
    /// </summary>
    public void SpawnCars()
    {
        if (carPrefabs.Count == 0)
        {
            Debug.LogError("CarSpawner: Không có prefab nào để spawn!");
            return;
        }
        
        // Spawn tại tất cả forward spawn points
        foreach (Transform spawnPoint in forwardSpawnPoints)
        {
            SpawnCarAtPoint(spawnPoint, false);
        }
        
        // Spawn tại tất cả reverse spawn points
        foreach (Transform spawnPoint in reverseSpawnPoints)
        {
            SpawnCarAtPoint(spawnPoint, true);
        }
        
        Debug.Log($"CarSpawner: Đã spawn {spawnedCars.Count} objects (Forward: {forwardSpawnPoints.Count}, Reverse: {reverseSpawnPoints.Count})");
    }
    
    /// <summary>
    /// Spawn một xe tại một spawn point cụ thể
    /// </summary>
    private void SpawnCarAtPoint(Transform spawnPoint, bool isReverse)
    {
        GameObject prefabToSpawn = GetRandomPrefab();
        if (prefabToSpawn == null)
        {
            return;
        }
        
        Vector3 spawnPosition = spawnPoint.position;
        // Set rotation: Y = 180 nếu reverse, Y = 0 nếu forward
        float rotationY = isReverse ? 180f : 0f;
        Quaternion spawnRotation = Quaternion.Euler(spawnPoint.rotation.eulerAngles.x, rotationY, spawnPoint.rotation.eulerAngles.z);
        
        GameObject car = GetCarFromPoolOrInstantiate(prefabToSpawn, spawnPosition, spawnRotation);
        
        CarController carController = car.GetComponent<CarController>();
        if (carController == null)
        {
            Debug.LogWarning($"CarSpawner: Prefab '{prefabToSpawn.name}' không có CarController component!");
        }
        else
        {
            // Gán hướng di chuyển cho xe
            carController.SetReverse(isReverse);
        }
        
        spawnedCars.Add(car);
    }
    
    /// <summary>
    /// Lấy car từ pool hoặc instantiate mới
    /// </summary>
    private GameObject GetCarFromPoolOrInstantiate(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject car = null;
        
        // Sử dụng pool nếu được bật
        if (usePool && CarPool.Instance != null)
        {
            // Lấy car từ pool dựa trên prefab
            car = CarPool.Instance.GetCar(prefab);
            if (car != null)
            {
                car.transform.position = position;
                car.transform.rotation = rotation;
            }
        }
        
        // Nếu không dùng pool hoặc pool không có car, spawn mới
        if (car == null)
        {
            car = Instantiate(prefab, position, rotation);
        }
        
        return car;
    }
    
    /// <summary>
    /// Lấy một prefab ngẫu nhiên từ danh sách
    /// </summary>
    private GameObject GetRandomPrefab()
    {
        if (carPrefabs.Count == 0)
        {
            return null;
        }
        
        if (randomPrefab && carPrefabs.Count > 1)
        {
            int randomIndex = Random.Range(0, carPrefabs.Count);
            return carPrefabs[randomIndex];
        }
        else
        {
            // Nếu không random hoặc chỉ có 1 prefab, trả về prefab đầu tiên
            return carPrefabs[0];
        }
    }
    
    /// <summary>
    /// Dọn dẹp các xe đã bị destroy khỏi danh sách
    /// </summary>
    private void CleanupDestroyedCars()
    {
        spawnedCars.RemoveAll(car => car == null);
    }
    
    /// <summary>
    /// Xóa tất cả cars đã spawn
    /// </summary>
    public void ClearSpawnedCars()
    {
        foreach (var car in spawnedCars)
        {
            if (car != null)
            {
                if (usePool && CarPool.Instance != null)
                {
                    CarPool.Instance.ReturnCar(car);
                }
                else
                {
                    Destroy(car);
                }
            }
        }
        spawnedCars.Clear();
    }
    
    /// <summary>
    /// Respawn cars (xóa cũ và spawn lại)
    /// </summary>
    public void RespawnCars()
    {
        ClearSpawnedCars();
        SpawnCars();
    }
}


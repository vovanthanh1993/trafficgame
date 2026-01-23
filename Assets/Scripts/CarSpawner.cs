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
    [Tooltip("Danh sách các GameObject cha chứa spawn points đi tiến (mỗi nhóm spawn độc lập)")]
    [SerializeField] private List<Transform> forwardSpawnPointsParents = new List<Transform>();
    
    [Tooltip("Danh sách các GameObject cha chứa spawn points đi lùi (mỗi nhóm spawn độc lập)")]
    [SerializeField] private List<Transform> reverseSpawnPointsParents = new List<Transform>();
    
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
    
    [Header("Spawn Weight Settings")]
    [Tooltip("Tên các car có tỉ lệ spawn cao hơn (ví dụ: Mtb1, Mtb2)")]
    [SerializeField] private string[] highSpawnRateCarNames = new string[] { "Mtb1", "Mtb2" };
    
    [Tooltip("Trọng số spawn cho các car đặc biệt (càng cao càng dễ spawn)")]
    [SerializeField] private int highSpawnRateWeight = 5;
    
    [Tooltip("Trọng số spawn cho các car thường (mặc định = 1)")]
    [SerializeField] private int normalSpawnRateWeight = 1;
    
    private List<Transform> forwardSpawnPoints = new List<Transform>();
    private List<Transform> reverseSpawnPoints = new List<Transform>();
    private List<List<Transform>> forwardSpawnPointGroups = new List<List<Transform>>(); // Danh sách các nhóm spawn points đi tiến
    private List<List<Transform>> reverseSpawnPointGroups = new List<List<Transform>>(); // Danh sách các nhóm spawn points đi lùi
    private List<float> forwardGroupLastSpawnTimes = new List<float>(); // Thời gian spawn cuối cùng của mỗi nhóm forward
    private List<float> reverseGroupLastSpawnTimes = new List<float>(); // Thời gian spawn cuối cùng của mỗi nhóm reverse
    private List<GameObject> spawnedCars = new List<GameObject>();
    private float lastSpawnTime = 0f;
    
    void Start()
    {
        InitializeSpawnPoints();
        InitializePrefabList();
        
        // Pool sẽ tự động tạo khi cần (không cần khởi tạo trước)
        
        // Spawn xe đầu tiên ngay lập tức (chỉ nếu không dùng nhóm - nhóm sẽ spawn độc lập trong Update)
        if (autoSpawn && forwardSpawnPointGroups.Count == 0 && reverseSpawnPointGroups.Count == 0)
        {
            SpawnSingleCar();
        }
    }
    
    void Update()
    {
        if (autoSpawn)
        {
            // Nếu có nhiều nhóm, spawn độc lập cho từng nhóm
            if (forwardSpawnPointGroups.Count > 0 || reverseSpawnPointGroups.Count > 0)
            {
                // Spawn cho các nhóm forward
                for (int i = 0; i < forwardSpawnPointGroups.Count; i++)
                {
                    if (Time.time - forwardGroupLastSpawnTimes[i] >= spawnInterval)
                    {
                        // Kiểm tra slow skill có đang active hoặc trong thời gian delay không
                        if (PlayerController.Instance != null && PlayerController.Instance.IsInSlowSkillSpawnBlock())
                        {
                            continue; // Bỏ qua spawn nếu slow skill đang active hoặc trong delay
                        }
                        
                        if (maxCarsOnScene <= 0 || spawnedCars.Count < maxCarsOnScene)
                        {
                            SpawnCarFromGroup(forwardSpawnPointGroups[i], false, i);
                            forwardGroupLastSpawnTimes[i] = Time.time;
                        }
                    }
                }
                
                // Spawn cho các nhóm reverse
                for (int i = 0; i < reverseSpawnPointGroups.Count; i++)
                {
                    if (Time.time - reverseGroupLastSpawnTimes[i] >= spawnInterval)
                    {
                        // Kiểm tra slow skill có đang active hoặc trong thời gian delay không
                        if (PlayerController.Instance != null && PlayerController.Instance.IsInSlowSkillSpawnBlock())
                        {
                            continue; // Bỏ qua spawn nếu slow skill đang active hoặc trong delay
                        }
                        
                        if (maxCarsOnScene <= 0 || spawnedCars.Count < maxCarsOnScene)
                        {
                            SpawnCarFromGroup(reverseSpawnPointGroups[i], true, i);
                            reverseGroupLastSpawnTimes[i] = Time.time;
                        }
                    }
                }
            }
            else
            {
                // Cách cũ: spawn chung tất cả
                if (Time.time - lastSpawnTime >= spawnInterval)
                {
                    // Kiểm tra slow skill có đang active hoặc trong thời gian delay không
                    if (PlayerController.Instance != null && PlayerController.Instance.IsInSlowSkillSpawnBlock())
                    {
                        // Bỏ qua spawn nếu slow skill đang active hoặc trong delay
                    }
                    else if (maxCarsOnScene <= 0 || spawnedCars.Count < maxCarsOnScene)
                    {
                        SpawnSingleCar();
                        lastSpawnTime = Time.time;
                    }
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
        forwardSpawnPointGroups.Clear();
        reverseSpawnPointGroups.Clear();
        forwardGroupLastSpawnTimes.Clear();
        reverseGroupLastSpawnTimes.Clear();
        
        // Lấy spawn points đi tiến từ nhiều nhóm
        if (forwardSpawnPointsParents != null && forwardSpawnPointsParents.Count > 0)
        {
            // Dùng danh sách nhiều nhóm - mỗi nhóm riêng biệt
            foreach (Transform parent in forwardSpawnPointsParents)
            {
                if (parent == null) continue;
                
                List<Transform> group = new List<Transform>();
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child != null)
                    {
                        group.Add(child);
                        forwardSpawnPoints.Add(child);
                    }
                }
                if (group.Count > 0)
                {
                    forwardSpawnPointGroups.Add(group);
                    forwardGroupLastSpawnTimes.Add(0f); // Khởi tạo thời gian spawn
                }
            }
            Debug.Log($"CarSpawner: Đã tìm thấy {forwardSpawnPoints.Count} spawn points đi tiến từ {forwardSpawnPointGroups.Count} nhóm (mỗi nhóm spawn độc lập)");
        }
        else
        {
            Debug.LogWarning("CarSpawner: forwardSpawnPointsParents không được set!");
        }
        
        // Lấy spawn points đi lùi từ nhiều nhóm
        if (reverseSpawnPointsParents != null && reverseSpawnPointsParents.Count > 0)
        {
            // Dùng danh sách nhiều nhóm - mỗi nhóm riêng biệt
            foreach (Transform parent in reverseSpawnPointsParents)
            {
                if (parent == null) continue;
                
                List<Transform> group = new List<Transform>();
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child != null)
                    {
                        group.Add(child);
                        reverseSpawnPoints.Add(child);
                    }
                }
                if (group.Count > 0)
                {
                    reverseSpawnPointGroups.Add(group);
                    reverseGroupLastSpawnTimes.Add(0f); // Khởi tạo thời gian spawn
                }
            }
            Debug.Log($"CarSpawner: Đã tìm thấy {reverseSpawnPoints.Count} spawn points đi lùi từ {reverseSpawnPointGroups.Count} nhóm (mỗi nhóm spawn độc lập)");
        }
        else
        {
            Debug.LogWarning("CarSpawner: reverseSpawnPointsParents không được set!");
        }
        
        if (forwardSpawnPointGroups.Count == 0 && reverseSpawnPointGroups.Count == 0)
        {
            Debug.LogError("CarSpawner: Không tìm thấy spawn point nào! Vui lòng gán forwardSpawnPointsParents/reverseSpawnPointsParents.");
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
    /// Spawn một xe từ một nhóm spawn points cụ thể
    /// </summary>
    private void SpawnCarFromGroup(List<Transform> spawnPointGroup, bool isReverse, int groupIndex)
    {
        if (spawnPointGroup == null || spawnPointGroup.Count == 0)
        {
            return;
        }
        
        if (carPrefabs.Count == 0)
        {
            Debug.LogError("CarSpawner: Không có prefab nào để spawn!");
            return;
        }
        
        // Chọn ngẫu nhiên một spawn point trong nhóm này
        int pointIndex = Random.Range(0, spawnPointGroup.Count);
        Transform selectedSpawnPoint = spawnPointGroup[pointIndex];
        
        // Chọn prefab
        GameObject prefabToSpawn = GetRandomPrefab();
        if (prefabToSpawn == null)
        {
            Debug.LogError("CarSpawner: Không thể lấy prefab để spawn!");
            return;
        }
        
        // Spawn car
        Vector3 spawnPosition = selectedSpawnPoint.position;
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
            carController.SetReverse(isReverse);
        }
        
        spawnedCars.Add(car);
        
        string directionText = isReverse ? "lùi" : "tiến";
        string groupType = isReverse ? "reverse" : "forward";
        Debug.Log($"CarSpawner: Đã spawn 1 xe đi {directionText} từ nhóm {groupType} #{groupIndex + 1} (spawn point {pointIndex + 1}). Tổng số xe: {spawnedCars.Count}");
    }
    
    /// <summary>
    /// Spawn một xe tại một spawn point ngẫu nhiên (cách cũ - dùng khi không có nhóm)
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
    /// Lấy một prefab ngẫu nhiên từ danh sách (có weighted random cho các car đặc biệt)
    /// </summary>
    private GameObject GetRandomPrefab()
    {
        if (carPrefabs.Count == 0)
        {
            return null;
        }
        
        if (!randomPrefab || carPrefabs.Count == 1)
        {
            // Nếu không random hoặc chỉ có 1 prefab, trả về prefab đầu tiên
            return carPrefabs[0];
        }
        
        // Tạo danh sách weighted cho weighted random
        List<GameObject> weightedPrefabs = new List<GameObject>();
        
        foreach (GameObject prefab in carPrefabs)
        {
            if (prefab == null) continue;
            
            // Kiểm tra xem prefab có nằm trong danh sách car đặc biệt không
            bool isHighSpawnRate = IsHighSpawnRateCar(prefab.name);
            int weight = isHighSpawnRate ? highSpawnRateWeight : normalSpawnRateWeight;
            
            // Thêm prefab vào danh sách theo trọng số
            for (int i = 0; i < weight; i++)
            {
                weightedPrefabs.Add(prefab);
            }
        }
        
        // Chọn ngẫu nhiên từ danh sách weighted
        if (weightedPrefabs.Count > 0)
        {
            int randomIndex = Random.Range(0, weightedPrefabs.Count);
            return weightedPrefabs[randomIndex];
        }
        
        // Fallback: chọn ngẫu nhiên bình thường
        int fallbackIndex = Random.Range(0, carPrefabs.Count);
        return carPrefabs[fallbackIndex];
    }
    
    /// <summary>
    /// Kiểm tra xem car có nằm trong danh sách car có tỉ lệ spawn cao không
    /// </summary>
    private bool IsHighSpawnRateCar(string carName)
    {
        if (highSpawnRateCarNames == null || highSpawnRateCarNames.Length == 0)
            return false;
        
        foreach (string highSpawnName in highSpawnRateCarNames)
        {
            if (carName.Contains(highSpawnName) || carName.Equals(highSpawnName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
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


using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner để spawn animals tại các spawn points
/// </summary>
public class AnimalSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Tự động load prefabs từ Resources/Prefabs (nếu bật, sẽ bỏ qua animalPrefabs list)")]
    [SerializeField] private bool autoLoadFromResources = true;
    
    [Tooltip("Đường dẫn thư mục trong Resources chứa animal prefabs (ví dụ: 'Prefabs/Animals')")]
    [SerializeField] private string prefabsFolderPath = "Prefabs/Animals";
    
    [Tooltip("List các animal prefabs (chỉ dùng khi autoLoadFromResources = false)")]
    [SerializeField] private List<GameObject> animalPrefabs = new List<GameObject>();
    
    [Header("Spawn Points")]
    [Tooltip("Danh sách các GameObject cha chứa spawn points đi tiến (mỗi nhóm spawn độc lập)")]
    [SerializeField] private List<Transform> forwardSpawnPointsParents = new List<Transform>();
    
    [Tooltip("Danh sách các GameObject cha chứa spawn points đi lùi (mỗi nhóm spawn độc lập)")]
    [SerializeField] private List<Transform> reverseSpawnPointsParents = new List<Transform>();
    
    [Header("Spawn Configuration")]
    [Tooltip("Khoảng thời gian giữa mỗi lần spawn (giây)")]
    [SerializeField] private float spawnInterval = 2f;
    
    [Tooltip("Chọn ngẫu nhiên prefab cho mỗi animal (nếu có nhiều prefab)")]
    [SerializeField] private bool randomPrefab = true;
    
    [Tooltip("Số lượng animal tối đa trên scene (0 = không giới hạn)")]
    [SerializeField] private int maxAnimalsOnScene = 0;
    
    [Tooltip("Tự động spawn liên tục")]
    [SerializeField] private bool autoSpawn = true;
    
    [Tooltip("Sử dụng object pool (khuyến nghị)")]
    [SerializeField] private bool usePool = true;
    
    [Header("Spawn Weight Settings")]
    [Tooltip("Tên các animal có tỉ lệ spawn cao hơn")]
    [SerializeField] private string[] highSpawnRateAnimalNames = new string[] { };
    
    [Tooltip("Trọng số spawn cho các animal đặc biệt (càng cao càng dễ spawn)")]
    [SerializeField] private int highSpawnRateWeight = 5;
    
    [Tooltip("Trọng số spawn cho các animal thường (mặc định = 1)")]
    [SerializeField] private int normalSpawnRateWeight = 1;
    
    private List<Transform> forwardSpawnPoints = new List<Transform>();
    private List<Transform> reverseSpawnPoints = new List<Transform>();
    private List<List<Transform>> forwardSpawnPointGroups = new List<List<Transform>>(); // Danh sách các nhóm spawn points đi tiến
    private List<List<Transform>> reverseSpawnPointGroups = new List<List<Transform>>(); // Danh sách các nhóm spawn points đi lùi
    private List<float> forwardGroupLastSpawnTimes = new List<float>(); // Thời gian spawn cuối cùng của mỗi nhóm forward
    private List<float> reverseGroupLastSpawnTimes = new List<float>(); // Thời gian spawn cuối cùng của mỗi nhóm reverse
    private List<GameObject> spawnedAnimals = new List<GameObject>();
    private float lastSpawnTime = 0f;
    
    void Start()
    {
        InitializeSpawnPoints();
        InitializePrefabList();
        
        // Pool sẽ tự động tạo khi cần (không cần khởi tạo trước)
        
        // Spawn animal đầu tiên ngay lập tức (chỉ nếu không dùng nhóm - nhóm sẽ spawn độc lập trong Update)
        if (autoSpawn && forwardSpawnPointGroups.Count == 0 && reverseSpawnPointGroups.Count == 0)
        {
            SpawnSingleAnimal();
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
                        
                        if (maxAnimalsOnScene <= 0 || spawnedAnimals.Count < maxAnimalsOnScene)
                        {
                            SpawnAnimalFromGroup(forwardSpawnPointGroups[i], false, i);
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
                        
                        if (maxAnimalsOnScene <= 0 || spawnedAnimals.Count < maxAnimalsOnScene)
                        {
                            SpawnAnimalFromGroup(reverseSpawnPointGroups[i], true, i);
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
                    else if (maxAnimalsOnScene <= 0 || spawnedAnimals.Count < maxAnimalsOnScene)
                    {
                        SpawnSingleAnimal();
                        lastSpawnTime = Time.time;
                    }
                }
            }
        }
        
        // Dọn dẹp danh sách animal đã bị destroy
        CleanupDestroyedAnimals();
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
            Debug.Log($"AnimalSpawner: Đã tìm thấy {forwardSpawnPoints.Count} spawn points đi tiến từ {forwardSpawnPointGroups.Count} nhóm (mỗi nhóm spawn độc lập)");
        }
        else
        {
            Debug.LogWarning("AnimalSpawner: forwardSpawnPointsParents không được set!");
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
            Debug.Log($"AnimalSpawner: Đã tìm thấy {reverseSpawnPoints.Count} spawn points đi lùi từ {reverseSpawnPointGroups.Count} nhóm (mỗi nhóm spawn độc lập)");
        }
        else
        {
            Debug.LogWarning("AnimalSpawner: reverseSpawnPointsParents không được set!");
        }
        
        if (forwardSpawnPointGroups.Count == 0 && reverseSpawnPointGroups.Count == 0)
        {
            Debug.LogError("AnimalSpawner: Không tìm thấy spawn point nào! Vui lòng gán forwardSpawnPointsParents/reverseSpawnPointsParents.");
        }
    }
    
    /// <summary>
    /// Khởi tạo danh sách prefabs từ list hoặc load từ Resources
    /// </summary>
    private void InitializePrefabList()
    {
        animalPrefabs.Clear();
        
        if (autoLoadFromResources)
        {
            LoadPrefabsFromResources();
        }
        
        if (animalPrefabs.Count == 0)
        {
            Debug.LogWarning("AnimalSpawner: Không có prefab nào được load! Vui lòng kiểm tra prefabsFolderPath hoặc thêm prefabs vào list.");
        }
        else
        {
            Debug.Log($"AnimalSpawner: Đã load {animalPrefabs.Count} prefabs");
        }
    }
    
    /// <summary>
    /// Load prefabs từ Resources folder
    /// </summary>
    private void LoadPrefabsFromResources()
    {
        if (string.IsNullOrEmpty(prefabsFolderPath))
        {
            Debug.LogWarning("AnimalSpawner: prefabsFolderPath không được set!");
            return;
        }
        
        // Load tất cả GameObject từ Resources/Prefabs
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(prefabsFolderPath);
        
        if (loadedPrefabs == null || loadedPrefabs.Length == 0)
        {
            Debug.LogWarning($"AnimalSpawner: Không tìm thấy prefab nào trong Resources/{prefabsFolderPath}!");
            return;
        }
        
        // Lọc các prefab có AnimalController component (để đảm bảo là animal prefab)
        foreach (GameObject prefab in loadedPrefabs)
        {
            if (prefab == null) continue;
            
            // Kiểm tra xem prefab có AnimalController component không
            AnimalController animalController = prefab.GetComponent<AnimalController>();
            if (animalController != null)
            {
                animalPrefabs.Add(prefab);
                Debug.Log($"AnimalSpawner: Đã load prefab '{prefab.name}'");
            }
        }
    }
    
    /// <summary>
    /// Spawn một animal từ một nhóm spawn points cụ thể
    /// </summary>
    private void SpawnAnimalFromGroup(List<Transform> spawnPointGroup, bool isReverse, int groupIndex)
    {
        if (spawnPointGroup == null || spawnPointGroup.Count == 0)
        {
            return;
        }
        
        if (animalPrefabs.Count == 0)
        {
            Debug.LogError("AnimalSpawner: Không có prefab nào để spawn!");
            return;
        }
        
        // Chọn ngẫu nhiên một spawn point trong nhóm này
        int pointIndex = Random.Range(0, spawnPointGroup.Count);
        Transform selectedSpawnPoint = spawnPointGroup[pointIndex];
        
        // Chọn prefab
        GameObject prefabToSpawn = GetRandomPrefab();
        if (prefabToSpawn == null)
        {
            Debug.LogError("AnimalSpawner: Không thể lấy prefab để spawn!");
            return;
        }
        
        // Spawn animal
        Vector3 spawnPosition = selectedSpawnPoint.position;
        float rotationY = isReverse ? 180f : 0f;
        Quaternion spawnRotation = Quaternion.Euler(selectedSpawnPoint.rotation.eulerAngles.x, rotationY, selectedSpawnPoint.rotation.eulerAngles.z);
        
        GameObject animal = GetAnimalFromPoolOrInstantiate(prefabToSpawn, spawnPosition, spawnRotation);
        
        // Đảm bảo animal có AnimalController component
        AnimalController animalController = animal.GetComponent<AnimalController>();
        if (animalController == null)
        {
            Debug.LogWarning($"AnimalSpawner: Prefab '{prefabToSpawn.name}' không có AnimalController component!");
        }
        else
        {
            animalController.SetReverse(isReverse);
        }
        
        spawnedAnimals.Add(animal);
        
        string directionText = isReverse ? "lùi" : "tiến";
        string groupType = isReverse ? "reverse" : "forward";
        Debug.Log($"AnimalSpawner: Đã spawn 1 animal đi {directionText} từ nhóm {groupType} #{groupIndex + 1} (spawn point {pointIndex + 1}). Tổng số animal: {spawnedAnimals.Count}");
    }
    
    /// <summary>
    /// Spawn một animal tại một spawn point ngẫu nhiên (cách cũ - dùng khi không có nhóm)
    /// </summary>
    public void SpawnSingleAnimal()
    {
        if (animalPrefabs.Count == 0)
        {
            Debug.LogError("AnimalSpawner: Không có prefab nào để spawn!");
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
            Debug.LogError("AnimalSpawner: Không có spawn point nào!");
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
            Debug.LogError("AnimalSpawner: Không thể lấy prefab để spawn!");
            return;
        }
        
        // Spawn animal
        Vector3 spawnPosition = selectedSpawnPoint.position;
        // Set rotation: Y = 180 nếu reverse, Y = 0 nếu forward
        float rotationY = isReverse ? 180f : 0f;
        Quaternion spawnRotation = Quaternion.Euler(selectedSpawnPoint.rotation.eulerAngles.x, rotationY, selectedSpawnPoint.rotation.eulerAngles.z);
        
        GameObject animal = GetAnimalFromPoolOrInstantiate(prefabToSpawn, spawnPosition, spawnRotation);
        
        // Đảm bảo animal có AnimalController component
        AnimalController animalController = animal.GetComponent<AnimalController>();
        if (animalController == null)
        {
            Debug.LogWarning($"AnimalSpawner: Prefab '{prefabToSpawn.name}' không có AnimalController component!");
        }
        else
        {
            // Gán hướng di chuyển cho animal (true = đi lùi, false = đi tiến)
            animalController.SetReverse(isReverse);
        }
        
        spawnedAnimals.Add(animal);
        lastSpawnTime = Time.time;
        
        string directionText = isReverse ? "lùi" : "tiến";
        Debug.Log($"AnimalSpawner: Đã spawn 1 animal đi {directionText} tại spawn point {spawnIndex + 1}. Tổng số animal: {spawnedAnimals.Count}");
    }
    
    /// <summary>
    /// Spawn animals tại tất cả các spawn points (giữ lại để tương thích)
    /// </summary>
    public void SpawnAnimals()
    {
        if (animalPrefabs.Count == 0)
        {
            Debug.LogError("AnimalSpawner: Không có prefab nào để spawn!");
            return;
        }
        
        // Spawn tại tất cả forward spawn points
        foreach (Transform spawnPoint in forwardSpawnPoints)
        {
            SpawnAnimalAtPoint(spawnPoint, false);
        }
        
        // Spawn tại tất cả reverse spawn points
        foreach (Transform spawnPoint in reverseSpawnPoints)
        {
            SpawnAnimalAtPoint(spawnPoint, true);
        }
        
        Debug.Log($"AnimalSpawner: Đã spawn {spawnedAnimals.Count} objects (Forward: {forwardSpawnPoints.Count}, Reverse: {reverseSpawnPoints.Count})");
    }
    
    /// <summary>
    /// Spawn một animal tại một spawn point cụ thể
    /// </summary>
    private void SpawnAnimalAtPoint(Transform spawnPoint, bool isReverse)
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
        
        GameObject animal = GetAnimalFromPoolOrInstantiate(prefabToSpawn, spawnPosition, spawnRotation);
        
        AnimalController animalController = animal.GetComponent<AnimalController>();
        if (animalController == null)
        {
            Debug.LogWarning($"AnimalSpawner: Prefab '{prefabToSpawn.name}' không có AnimalController component!");
        }
        else
        {
            // Gán hướng di chuyển cho animal
            animalController.SetReverse(isReverse);
        }
        
        spawnedAnimals.Add(animal);
    }
    
    /// <summary>
    /// Lấy animal từ pool hoặc instantiate mới
    /// </summary>
    private GameObject GetAnimalFromPoolOrInstantiate(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject animal = null;
        
        // Sử dụng pool nếu được bật (cần tạo AnimalPool tương tự CarPool)
        if (usePool && AnimalPool.Instance != null)
        {
            // Lấy animal từ pool dựa trên prefab
            animal = AnimalPool.Instance.GetAnimal(prefab);
            if (animal != null)
            {
                animal.transform.position = position;
                animal.transform.rotation = rotation;
            }
        }
        
        // Nếu không dùng pool hoặc pool không có animal, spawn mới
        if (animal == null)
        {
            animal = Instantiate(prefab, position, rotation);
        }
        
        return animal;
    }
    
    /// <summary>
    /// Lấy một prefab ngẫu nhiên từ danh sách (có weighted random cho các animal đặc biệt)
    /// </summary>
    private GameObject GetRandomPrefab()
    {
        if (animalPrefabs.Count == 0)
        {
            return null;
        }
        
        if (!randomPrefab || animalPrefabs.Count == 1)
        {
            // Nếu không random hoặc chỉ có 1 prefab, trả về prefab đầu tiên
            return animalPrefabs[0];
        }
        
        // Tạo danh sách weighted cho weighted random
        List<GameObject> weightedPrefabs = new List<GameObject>();
        
        foreach (GameObject prefab in animalPrefabs)
        {
            if (prefab == null) continue;
            
            // Kiểm tra xem prefab có nằm trong danh sách animal đặc biệt không
            bool isHighSpawnRate = IsHighSpawnRateAnimal(prefab.name);
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
        int fallbackIndex = Random.Range(0, animalPrefabs.Count);
        return animalPrefabs[fallbackIndex];
    }
    
    /// <summary>
    /// Kiểm tra xem animal có nằm trong danh sách animal có tỉ lệ spawn cao không
    /// </summary>
    private bool IsHighSpawnRateAnimal(string animalName)
    {
        if (highSpawnRateAnimalNames == null || highSpawnRateAnimalNames.Length == 0)
            return false;
        
        foreach (string highSpawnName in highSpawnRateAnimalNames)
        {
            if (animalName.Contains(highSpawnName) || animalName.Equals(highSpawnName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Dọn dẹp các animal đã bị destroy khỏi danh sách
    /// </summary>
    private void CleanupDestroyedAnimals()
    {
        spawnedAnimals.RemoveAll(animal => animal == null);
    }
    
    /// <summary>
    /// Xóa tất cả animals đã spawn
    /// </summary>
    public void ClearSpawnedAnimals()
    {
        foreach (var animal in spawnedAnimals)
        {
            if (animal != null)
            {
                if (usePool && AnimalPool.Instance != null)
                {
                    AnimalPool.Instance.ReturnAnimal(animal);
                }
                else
                {
                    Destroy(animal);
                }
            }
        }
        spawnedAnimals.Clear();
    }
    
    /// <summary>
    /// Respawn animals (xóa cũ và spawn lại)
    /// </summary>
    public void RespawnAnimals()
    {
        ClearSpawnedAnimals();
        SpawnAnimals();
    }
}

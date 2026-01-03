using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f; // Tốc độ gốc (từ Inspector)
    public Vector3 moveDirection = Vector3.forward; // Hướng di chuyển
    
    [Tooltip("Đi lùi (reverse) - nếu true, sẽ đảo ngược moveDirection")]
    public bool isReversing = false;
    
    [Header("Collision Avoidance")]
    [Tooltip("Khoảng cách phát hiện xe phía trước (meters)")]
    [SerializeField] private float detectionDistance = 8f;
    
    [Tooltip("Layer mask để phát hiện xe (để trống sẽ phát hiện tất cả)")]
    [SerializeField] private LayerMask carLayerMask = -1;
    
    [Tooltip("Độ cao của raycast so với vị trí xe (để tránh phát hiện mặt đất)")]
    [SerializeField] private float raycastHeight = 0.5f;
    
    private float originalSpeed; // Lưu tốc độ ban đầu (đã bao gồm level boost)
    private float baseSpeed; // Tốc độ gốc từ Inspector (chưa có level boost)
    private bool wasSlowingDown = false; // Để track trạng thái giảm tốc độ
    private bool hasDetectedCar = false; // Đã phát hiện xe phía trước, ngừng kiểm tra raycast
    private bool levelBoostApplied = false; // Đã áp dụng level boost chưa
    
    [Header("Wheel Settings")]
    [Tooltip("Bật/tắt quay bánh xe")]
    [SerializeField] private bool enableWheel = true;
    
    [Tooltip("Tag hoặc tên để nhận diện bánh xe (để trống sẽ lấy tất cả các con)")]
    public string wheelTag = ""; // Tag của bánh xe (để trống sẽ lấy tất cả children)
    
    [Tooltip("Bán kính bánh xe (để tính tốc độ quay chính xác, mặc định 0.3)")]
    public float wheelRadius = 0.3f;
    
    [Tooltip("Trục quay của bánh xe (X, Y, hoặc Z)")]
    public Vector3 wheelRotationAxis = Vector3.right; // Trục X để quay quanh trục ngang
    
    private Transform[] wheels; // Mảng các bánh xe (tự động lấy từ children)
    
    [Header("Limit Detection")]
    public string limitTag = "Limit"; // Tag của đối tượng limit
    
    void Awake()
    {
        // Lưu tốc độ gốc ban đầu từ Inspector (chỉ lần đầu, trước khi bị thay đổi)
        if (baseSpeed == 0)
        {
            baseSpeed = moveSpeed;
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Tự động lấy các bánh xe từ children
        InitializeWheels();
        
        // Áp dụng tốc độ tăng theo level: mỗi level +0.1 (chỉ một lần)
        if (!levelBoostApplied)
        {
            ApplyLevelSpeedBoost();
            levelBoostApplied = true;
        }
        
        // Reset về tốc độ gốc (đã bao gồm level boost) khi spawn lại
        moveSpeed = originalSpeed;
        wasSlowingDown = false;
        hasDetectedCar = false; // Reset flag khi spawn lại
    }
    
    /// <summary>
    /// Áp dụng tốc độ tăng theo level: mỗi level +0.1
    /// </summary>
    private void ApplyLevelSpeedBoost()
    {
        // Lấy level hiện tại từ PlayerPrefs
        int currentLevel = 1;
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            currentLevel = PlayerPrefs.GetInt("CurrentLevel");
        }
        
        // Tính tốc độ tăng: level 1 = +0, level 2 = +0.1, level 3 = +0.2, ...
        float speedBoost = (currentLevel - 1) * 0.1f;
        
        // Áp dụng tốc độ tăng vào originalSpeed (dựa trên baseSpeed)
        originalSpeed = baseSpeed + speedBoost;
        
        Debug.Log($"CarController: Level {currentLevel} - Tốc độ gốc: {baseSpeed:F1}, Tăng: +{speedBoost:F1}, Tốc độ mới: {originalSpeed:F1}");
    }
    
    // Khởi tạo danh sách bánh xe từ children
    private void InitializeWheels()
    {
        System.Collections.Generic.List<Transform> wheelList = new System.Collections.Generic.List<Transform>();
        
        // Lấy tất cả các con của object này
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            
            // Nếu có wheelTag, chỉ lấy những child có tag đó
            if (!string.IsNullOrEmpty(wheelTag))
            {
                if (child.CompareTag(wheelTag))
                {
                    wheelList.Add(child);
                }
            }
            else
            {
                // Nếu không có wheelTag, lấy tất cả children
                wheelList.Add(child);
            }
        }
        
        wheels = wheelList.ToArray();
        
        if (wheels.Length == 0)
        {
            Debug.LogWarning($"CarController: Không tìm thấy bánh xe nào trong {gameObject.name}! Vui lòng kiểm tra cấu trúc object.");
        }
        else
        {
            Debug.Log($"CarController: Đã tìm thấy {wheels.Length} bánh xe");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Kiểm tra và điều chỉnh tốc độ nếu có xe phía trước
        CheckAndAdjustSpeed();
        
        // Di chuyển thẳng về phía trước
        MoveForward();
        
        // Xoay bánh xe
        RotateWheels();
    }
    
    /// <summary>
    /// Kiểm tra xe phía trước và điều chỉnh tốc độ
    /// </summary>
    private void CheckAndAdjustSpeed()
    {
        // Nếu đã phát hiện xe phía trước, ngừng kiểm tra raycast, giữ nguyên tốc độ
        if (hasDetectedCar)
        {
            return;
        }
        
        Vector3 direction = isReversing ? -moveDirection.normalized : moveDirection.normalized;
        
        // Raycast để phát hiện xe phía trước
        RaycastHit hit;
        // Đặt rayOrigin lên cao hơn một chút để tránh phát hiện mặt đất
        Vector3 rayOrigin = transform.position + Vector3.up * raycastHeight;
        Vector3 rayDirection = direction;
        
        // Vẽ raycast trong Scene view (màu đỏ khi không phát hiện, xanh khi phát hiện xe)
        Color rayColor = wasSlowingDown ? Color.green : Color.red;
        Debug.DrawRay(rayOrigin, rayDirection * detectionDistance, rayColor);
        
        // Kiểm tra xem có xe nào phía trước không
        bool isSlowingDown = false;
        
        // Raycast với QueryTriggerInteraction.Collide để phát hiện cả trigger colliders
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, detectionDistance, carLayerMask, QueryTriggerInteraction.Collide))
        {
            // Kiểm tra xem object phát hiện được có phải là xe khác không
            CarController otherCar = hit.collider.GetComponent<CarController>();
            if (otherCar != null && otherCar != this)
            {
                // Có xe phía trước, giảm tốc độ
                isSlowingDown = true;
                
                // Lấy tốc độ của xe phía trước
                float otherCarSpeed = otherCar.moveSpeed;
                
                // Nếu tốc độ xe sau (originalSpeed) lớn hơn tốc độ xe trước, dùng tốc độ xe trước
                // Ngược lại, dùng originalSpeed (giữ nguyên tốc độ ban đầu)
                float targetSpeed = originalSpeed > otherCarSpeed ? otherCarSpeed : originalSpeed;
                moveSpeed = targetSpeed;
                
                // Đánh dấu đã phát hiện xe, ngừng kiểm tra raycast
                hasDetectedCar = true;
                
                // Chỉ log khi bắt đầu giảm tốc độ (thay đổi trạng thái)
                if (!wasSlowingDown)
                {
                    Debug.LogWarning($"[{gameObject.name}] Phát hiện xe phía trước: {otherCar.name} (distance: {hit.distance:F2}m), tốc độ xe trước: {otherCarSpeed}, giảm tốc độ về {targetSpeed} và ngừng kiểm tra");
                }
            }
            else
            {
                // Phát hiện được object nhưng không phải là CarController
                if (!wasSlowingDown)
                {
                   // Debug.LogWarning($"[{gameObject.name}] Raycast hit nhưng không phải xe: {hit.collider.name} (distance: {hit.distance:F2}m)");
                }
            }
        }
        
        // Cập nhật trạng thái
        wasSlowingDown = isSlowingDown;
    }
    
    // Di chuyển thẳng về phía trước hoặc lùi
    private void MoveForward()
    {
        Vector3 direction = isReversing ? -moveDirection.normalized : moveDirection.normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }
    
    /// <summary>
    /// Set hướng di chuyển (tiến hoặc lùi)
    /// </summary>
    public void SetReverse(bool reverse)
    {
        isReversing = reverse;
    }
    
    /// <summary>
    /// Reset car về trạng thái ban đầu (dùng khi return về pool)
    /// </summary>
    public void ResetCar()
    {
        // Reset tốc độ về giá trị gốc (đã bao gồm level boost)
        moveSpeed = originalSpeed;
        
        // Reset các flags
        wasSlowingDown = false;
        hasDetectedCar = false;
        
        // Reset hướng di chuyển
        isReversing = false;
        moveDirection = Vector3.forward;
        
        // Reset transform (nếu cần)
        // transform.position và transform.rotation sẽ được reset bởi CarPool
    }
    
    // Xoay bánh xe khi xe di chuyển
    private void RotateWheels()
    {
        // Nếu không bật quay bánh xe, return ngay
        if (!enableWheel)
            return;
            
        if (wheels == null || wheels.Length == 0)
            return;
        
        // Tính góc quay dựa trên tốc độ di chuyển và bán kính bánh xe
        // Công thức: góc quay (độ) = (quãng đường / chu vi bánh xe) * 360
        // Quãng đường = moveSpeed * Time.deltaTime
        // Chu vi = 2 * PI * wheelRadius
        float distance = moveSpeed * Time.deltaTime;
        float circumference = 2f * Mathf.PI * wheelRadius;
        float rotationAngle = (distance / circumference) * 360f;
        
        // Xoay tất cả các bánh xe
        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
            {
                wheel.Rotate(wheelRotationAxis * rotationAngle, Space.Self);
            }
        }
    }
    
    // Xử lý va chạm với trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(limitTag))
        {
            // Khi chạm limit, trả về pool hoặc destroy
            if (CarPool.Instance != null)
            {
                CarPool.Instance.ReturnCar(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    
    /// <summary>
    /// Vẽ raycast trong Scene view để debug
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
            
        Vector3 direction = isReversing ? -moveDirection.normalized : moveDirection.normalized;
        Vector3 rayOrigin = transform.position + Vector3.up * raycastHeight;
        Vector3 rayEnd = rayOrigin + direction * detectionDistance;
        
        // Vẽ raycast màu đỏ khi không phát hiện gì, màu xanh khi phát hiện xe
        Gizmos.color = wasSlowingDown ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayEnd);
        
        // Vẽ sphere ở điểm bắt đầu raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rayOrigin, 0.2f);
        
        // Vẽ sphere ở điểm kết thúc raycast
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rayEnd, 0.2f);
    }
}

using UnityEngine;

public class AnimalController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Tốc độ gốc (từ Inspector)
    public Vector3 moveDirection = Vector3.forward; // Hướng di chuyển
    
    [Tooltip("Đi lùi (reverse) - nếu true, sẽ đảo ngược moveDirection")]
    public bool isReversing = false;
    
    [Header("Collision Avoidance")]
    [Tooltip("Khoảng cách phát hiện vật thể phía trước (meters)")]
    [SerializeField] private float detectionDistance = 6f;
    
    [Tooltip("Layer mask để phát hiện vật thể (để trống sẽ phát hiện tất cả)")]
    [SerializeField] private LayerMask detectionLayerMask = -1;
    
    [Tooltip("Độ cao của raycast so với vị trí animal (để tránh phát hiện mặt đất)")]
    [SerializeField] private float raycastHeight = 0.5f;
    
    private float originalSpeed; // Lưu tốc độ ban đầu (đã bao gồm level boost)
    private float baseSpeed; // Tốc độ gốc từ Inspector (chưa có level boost)
    private bool wasSlowingDown = false; // Để track trạng thái giảm tốc độ
    private bool hasDetectedObstacle = false; // Đã phát hiện vật thể phía trước, ngừng kiểm tra raycast
    private bool levelBoostApplied = false; // Đã áp dụng level boost chưa
    private Coroutine slowEffectCoroutine; // Coroutine để quản lý slow effect
    private bool isSlowed = false; // Đang trong trạng thái bị làm chậm
    private float speedBeforeSlow = 0f; // Lưu tốc độ trước khi slow để restore sau
    
    [Header("Animation Settings")]
    [Tooltip("Animator component của animal (tự động lấy nếu null)")]
    private Animator animator;
    private float originalAnimationSpeed = 1f; // Tốc độ animation gốc
    
    [Header("Limit Detection")]
    public string limitTag = "Limit"; // Tag của đối tượng limit
    
    void Awake()
    {
        // Lưu tốc độ gốc ban đầu từ Inspector (chỉ lần đầu, trước khi bị thay đổi)
        if (baseSpeed == 0)
        {
            baseSpeed = moveSpeed;
        }
        
        // Tự động lấy Animator component nếu chưa có
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                originalAnimationSpeed = animator.speed;
            }
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Áp dụng tốc độ tăng theo level: mỗi level +0.1 (chỉ một lần)
        if (!levelBoostApplied)
        {
            ApplyLevelSpeedBoost();
            levelBoostApplied = true;
        }
        
        // Reset về tốc độ gốc (đã bao gồm level boost) khi spawn lại
        moveSpeed = originalSpeed;
        wasSlowingDown = false;
        hasDetectedObstacle = false; // Reset flag khi spawn lại
        
        // Reset animation speed về bình thường
        if (animator != null)
        {
            animator.speed = originalAnimationSpeed;
        }
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
        
        Debug.Log($"AnimalController: Level {currentLevel} - Tốc độ gốc: {baseSpeed:F1}, Tăng: +{speedBoost:F1}, Tốc độ mới: {originalSpeed:F1}");
    }

    // Update is called once per frame
    void Update()
    {
        // Kiểm tra và điều chỉnh tốc độ nếu có vật thể phía trước
        CheckAndAdjustSpeed();
        
        // Di chuyển thẳng về phía trước
        MoveForward();
    }
    
    /// <summary>
    /// Kiểm tra vật thể phía trước và điều chỉnh tốc độ
    /// </summary>
    private void CheckAndAdjustSpeed()
    {
        // Nếu đã phát hiện vật thể phía trước, ngừng kiểm tra raycast, giữ nguyên tốc độ
        if (hasDetectedObstacle)
        {
            return;
        }
        
        Vector3 direction = isReversing ? -moveDirection.normalized : moveDirection.normalized;
        
        // Raycast để phát hiện vật thể phía trước
        RaycastHit hit;
        // Đặt rayOrigin lên cao hơn một chút để tránh phát hiện mặt đất
        Vector3 rayOrigin = transform.position + Vector3.up * raycastHeight;
        Vector3 rayDirection = direction;
        
        // Vẽ raycast trong Scene view (màu đỏ khi không phát hiện, xanh khi phát hiện vật thể)
        Color rayColor = wasSlowingDown ? Color.green : Color.red;
        Debug.DrawRay(rayOrigin, rayDirection * detectionDistance, rayColor);
        
        // Kiểm tra xem có vật thể nào phía trước không
        bool isSlowingDown = false;
        
        // Raycast với QueryTriggerInteraction.Collide để phát hiện cả trigger colliders
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, detectionDistance, detectionLayerMask, QueryTriggerInteraction.Collide))
        {
            // Kiểm tra xem object phát hiện được có phải là animal hoặc car khác không
            AnimalController otherAnimal = hit.collider.GetComponent<AnimalController>();
            CarController otherCar = hit.collider.GetComponent<CarController>();
            
            if ((otherAnimal != null && otherAnimal != this) || otherCar != null)
            {
                // Có vật thể phía trước, giảm tốc độ
                isSlowingDown = true;
                
                // Lấy tốc độ của vật thể phía trước
                float otherSpeed = originalSpeed;
                if (otherAnimal != null && otherAnimal != this)
                {
                    otherSpeed = otherAnimal.moveSpeed;
                }
                else if (otherCar != null)
                {
                    otherSpeed = otherCar.moveSpeed;
                }
                
                // Nếu tốc độ animal sau (originalSpeed) lớn hơn tốc độ vật thể trước, dùng tốc độ vật thể trước
                // Ngược lại, dùng originalSpeed (giữ nguyên tốc độ ban đầu)
                float targetSpeed = originalSpeed > otherSpeed ? otherSpeed : originalSpeed;
                moveSpeed = targetSpeed;
                
                // Đánh dấu đã phát hiện vật thể, ngừng kiểm tra raycast
                hasDetectedObstacle = true;
                
                // Chỉ log khi bắt đầu giảm tốc độ (thay đổi trạng thái)
                if (!wasSlowingDown)
                {
                    string obstacleName = otherAnimal != null ? otherAnimal.name : otherCar.name;
                    string obstacleType = otherAnimal != null ? "animal" : "car";
                    Debug.LogWarning($"[{gameObject.name}] Phát hiện {obstacleType} phía trước: {obstacleName} (distance: {hit.distance:F2}m), tốc độ vật thể trước: {otherSpeed}, giảm tốc độ về {targetSpeed} và ngừng kiểm tra");
                }
            }
            else
            {
                // Phát hiện được object nhưng không phải là AnimalController hoặc CarController
                if (!wasSlowingDown)
                {
                   // Debug.LogWarning($"[{gameObject.name}] Raycast hit nhưng không phải animal/car: {hit.collider.name} (distance: {hit.distance:F2}m)");
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
    /// Áp dụng slow effect (làm chậm tốc độ) trong thời gian nhất định
    /// </summary>
    /// <param name="slowPercent">Phần trăm giảm tốc độ (0.5 = giảm 50%)</param>
    /// <param name="duration">Thời gian slow effect (giây)</param>
    public void ApplySlowEffect(float slowPercent, float duration)
    {
        // Dừng coroutine cũ nếu có
        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
        }
        
        // Bắt đầu coroutine mới
        slowEffectCoroutine = StartCoroutine(SlowEffectCoroutine(slowPercent, duration));
    }
    
    /// <summary>
    /// Coroutine để áp dụng và tự động restore tốc độ sau khi hết thời gian
    /// </summary>
    private System.Collections.IEnumerator SlowEffectCoroutine(float slowPercent, float duration)
    {
        isSlowed = true;
        
        // Lưu tốc độ hiện tại (có thể đã bị giảm do phát hiện vật thể phía trước)
        speedBeforeSlow = moveSpeed;
        
        // Set tốc độ về 1 khi slow skill active
        moveSpeed = 1f;
        
        // Làm chậm animation nếu có Animator
        // Tính animation speed dựa trên tỷ lệ giữa tốc độ mới (1) và tốc độ trước khi slow
        if (animator != null && speedBeforeSlow > 0)
        {
            float animationSpeedRatio = 1f / speedBeforeSlow;
            animator.speed = originalAnimationSpeed * animationSpeedRatio;
            Debug.Log($"AnimalController: Animation speed giảm từ {originalAnimationSpeed:F2} xuống {animator.speed:F2} (tỷ lệ: {animationSpeedRatio:F2})");
        }
        
        Debug.Log($"AnimalController: Áp dụng slow effect - Tốc độ: {speedBeforeSlow:F2} -> 1.00");
        
        // Đợi duration giây
        yield return new WaitForSeconds(duration);
        
        // Restore tốc độ về giá trị trước khi slow (không phải originalSpeed)
        moveSpeed = speedBeforeSlow;
        
        // Restore animation speed về bình thường
        if (animator != null)
        {
            animator.speed = originalAnimationSpeed;
            Debug.Log($"AnimalController: Animation speed đã restore về {originalAnimationSpeed:F2}");
        }
        
        isSlowed = false;
        
        Debug.Log($"AnimalController: Slow effect hết hạn - Tốc độ đã restore về {speedBeforeSlow:F2} (tốc độ trước khi slow)");
        
        slowEffectCoroutine = null;
    }
    
    /// <summary>
    /// Reset animal về trạng thái ban đầu (dùng khi return về pool)
    /// </summary>
    public void ResetAnimal()
    {
        // Dừng slow effect nếu đang chạy
        if (slowEffectCoroutine != null)
        {
            StopCoroutine(slowEffectCoroutine);
            slowEffectCoroutine = null;
        }
        
        // Reset tốc độ về giá trị gốc (đã bao gồm level boost)
        moveSpeed = originalSpeed;
        isSlowed = false;
        
        // Reset animation speed về bình thường
        if (animator != null)
        {
            animator.speed = originalAnimationSpeed;
        }
        
        // Reset các flags
        wasSlowingDown = false;
        hasDetectedObstacle = false;
        
        // Reset hướng di chuyển
        isReversing = false;
        moveDirection = Vector3.forward;
        
        // Reset transform (nếu cần)
        // transform.position và transform.rotation sẽ được reset bởi AnimalPool
    }
    
    // Xử lý va chạm với trigger
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(limitTag))
        {
            // Khi chạm limit, trả về pool hoặc destroy
            if (AnimalPool.Instance != null)
            {
                AnimalPool.Instance.ReturnAnimal(gameObject);
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
        
        // Vẽ raycast màu đỏ khi không phát hiện gì, màu xanh khi phát hiện vật thể
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

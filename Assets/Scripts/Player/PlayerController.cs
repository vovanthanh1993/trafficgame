using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GameObject model;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    
    private float baseMoveSpeed; // Tốc độ gốc
    private bool isSpeedBoosted = false; // Đang trong trạng thái speed boost
    
    [Header("Camera Settings")]
    [SerializeField] private Transform camTarget;

    [Header("Item Collection")]
    [Tooltip("Điểm để hiển thị item khi đã nhặt")]
    [SerializeField] private Transform itemPoint;
    
    // Item đang được mang theo (chỉ 1 item)
    private Item carriedItem = null;

    [Header("Spawn Settings")]
    [Tooltip("Vị trí spawn point (nếu null sẽ dùng vị trí ban đầu của player)")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("Input Control")]
    [Tooltip("Cho phép nhận input từ người chơi hay không")]
    [SerializeField] private bool canReceiveInput = false;
    [SerializeField] private bool isDisable = false;
    
    // Components
    public PlayerAnimation playerAnimation;
    
    private Vector3 initialSpawnPosition;
    
    // Flag để tránh trừ mạng nhiều lần khi va chạm với nhiều ResetTag cùng lúc
    private bool isReturningToSpawn = false;
    private float lastSpawnReturnTime = 0f;
    [Header("Spawn Return Settings")]
    [Tooltip("Thời gian cooldown sau khi về spawn point (giây)")]
    [SerializeField] private float spawnReturnCooldown = 0.5f;
    
    // Flag để tránh trigger animation hit quá nhiều lần liên tiếp
    private float lastHitTime = 0f;
    [Header("Hit Animation Settings")]
    [Tooltip("Thời gian cooldown giữa các lần trigger animation hit (giây)")]
    [SerializeField] private float hitAnimationCooldown = 0.5f;
    
    [Header("Hit Control Settings")]
    [Tooltip("Thời gian disable điều khiển sau khi bị hit (giây)")]
    [SerializeField] private float hitControlDisableDuration = 1f;
    
    [Header("Hit VFX Settings")]
    [Tooltip("VFX effect khi va chạm với xe (particle, explosion, etc.)")]
    [SerializeField] private GameObject hitVFXPrefab;
    
    [Tooltip("Vị trí spawn VFX (null = vị trí va chạm)")]
    [SerializeField] private Transform hitVFXSpawnPoint;
    
    [Header("Pickup VFX Settings")]
    [Tooltip("Vị trí spawn VFX khi nhặt item (health, speed)")]
    [SerializeField] private Transform pickupVFXPoint;
    
    [Tooltip("VFX effect khi nhặt Health Item")]
    [SerializeField] private GameObject healthPickupVFXPrefab;
    
    [Tooltip("VFX effect khi nhặt Speed Item")]
    [SerializeField] private GameObject speedPickupVFXPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Get components
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        playerAnimation = GetComponent<PlayerAnimation>();
        
        // Tự động tìm ItemPoint nếu chưa được assign
        if (itemPoint == null)
        {
            itemPoint = transform.Find("ItemPoint");
            if (itemPoint == null)
            {
                // Tìm trong tất cả các con
                foreach (Transform child in transform)
                {
                    if (child.name == "ItemPoint")
                    {
                        itemPoint = child;
                        break;
                    }
                }
            }
        }
        
        // Tự động tìm PickupVFXPoint nếu chưa được assign
        if (pickupVFXPoint == null)
        {
            pickupVFXPoint = transform.Find("PickupVFXPoint");
            if (pickupVFXPoint == null)
            {
                // Tìm trong tất cả các con
                foreach (Transform child in transform)
                {
                    if (child.name == "PickupVFXPoint" || child.name == "VFXPoint")
                    {
                        pickupVFXPoint = child;
                        break;
                    }
                }
            }
        }
        
        // Đảm bảo CharacterController tồn tại
        if (characterController == null)
        {
            Debug.LogError("PlayerController: CharacterController component is missing! Please add CharacterController to the player GameObject.");
        }
    }

    private void Start()
    {
        // Lưu tốc độ gốc
        baseMoveSpeed = moveSpeed;
        
        // Lưu vị trí spawn point
        if (spawnPoint != null)
        {
            initialSpawnPosition = spawnPoint.position;
        }
        else
        {
            initialSpawnPosition = transform.position;
        }
        
        // Reset flag khi bắt đầu level mới
        isReturningToSpawn = false;
        lastSpawnReturnTime = 0f;
        
        // Camera sẽ được quản lý bởi CameraFollowController tự động
    }

    private void Update()
    {
        if (isDisable || !canReceiveInput)
        {
            // Nếu không cho phép input, dừng animation
            if (playerAnimation != null)
            {
                playerAnimation.SetMovement(false, 0f);
            }
            return;
        }

        HandleInput();
    }

    private void LateUpdate()
    {
        if (!isDisable)
        {
            UpdateCameraTarget();
        }
    }

    #region Initialization
    #endregion

    #region Input Handling

    private void HandleInput()
    {
        if (InputManager.Instance == null) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        if (characterController == null || InputManager.Instance == null)
        {
            return;
        }

        Vector2 moveInput = InputManager.Instance.InputMoveVector();
        
        if (moveInput.magnitude < 0.1f)
        {
            // Không có input - chỉ áp dụng gravity
            characterController.Move(Physics.gravity * Time.deltaTime);
            
            // Cập nhật animation
            if (playerAnimation != null)
            {
                playerAnimation.SetMovement(false, 0f);
            }
            return;
        }

        // Tính toán hướng di chuyển tương đối với camera rotation
        Vector3 worldDirection = GetWorldDirection(moveInput);
        
        // Xoay player theo hướng di chuyển
        if (worldDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Di chuyển player
        Vector3 velocity = worldDirection * moveSpeed + Physics.gravity;
        characterController.Move(velocity * Time.deltaTime);

        // Cập nhật animation walk
        if (playerAnimation != null)
        {
            float moveSpeedValue = moveInput.magnitude;
            playerAnimation.SetMovement(true, moveSpeedValue);
        }
    }

    /// <summary>
    /// Chuyển đổi input direction sang world direction dựa trên camera rotation
    /// </summary>
    private Vector3 GetWorldDirection(Vector2 inputDirection)
    {
        // Lấy rotation từ camera hoặc từ player rotation
        Quaternion rotation = Quaternion.identity;
        
        if (Camera.main != null)
        {
            // Dùng camera yaw để tính hướng di chuyển
            float cameraYaw = Camera.main.transform.eulerAngles.y;
            rotation = Quaternion.Euler(0f, cameraYaw, 0f);
        }
        else
        {
            // Nếu không có camera, dùng player rotation
            rotation = transform.rotation;
        }
        
        // Chuyển đổi input direction sang world direction
        Vector3 direction = new Vector3(inputDirection.x, 0f, inputDirection.y);
        return rotation * direction;
    }


    #endregion

    #region Visual & Camera

    private void UpdateCameraTarget()
    {
        // Không cần xoay camTarget nữa vì camera top-down chỉ follow position
        // Giữ hàm này để không phá vỡ code khác nhưng không làm gì
    }

    #endregion

    #region Collision Detection

    /// <summary>
    /// Xử lý va chạm với trigger (vật thể có tag "end")
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EndTag"))
        {
            ShowVictory();
        }
        
        // Kiểm tra va chạm với xe (xe có isTrigger = true)
        CarController car = other.GetComponent<CarController>();
        if (car != null)
        {
            // Trigger animation hit và bay về spawn point khi va chạm với xe (có cooldown)
            if (Time.time - lastHitTime >= hitAnimationCooldown)
            {
                // Spawn VFX effect khi va chạm
                SpawnHitVFX(transform.position);
                
                if (playerAnimation != null)
                {
                    playerAnimation.SetHit();
                    lastHitTime = Time.time;
                }
                
                // Nếu đang mang item, cho item bay về vị trí ban đầu
                if (carriedItem != null)
                {
                    carriedItem.ReturnToOriginalPosition();
                    carriedItem = null; // Reset carried item
                }
                
                // Disable điều khiển trong 0.5s sau khi bị hit
                StartCoroutine(DisableControlAfterHit());
                
                // Bay về spawn point (tương tự như khi va chạm với ResetTag)
                ReturnToSpawnPoint();
            }
        }
        
        // Xử lý checkpoint
        Checkpoint checkpoint = other.GetComponent<Checkpoint>();
        if (checkpoint != null)
        {
            checkpoint.OnPlayerEnter(this);
        }
    }
    
    /// <summary>
    /// Xử lý va chạm với collider khi dùng Character Controller
    /// Character Controller không trigger OnCollisionEnter, cần dùng OnControllerColliderHit
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Kiểm tra va chạm với item (item không phải trigger)
        Item item = hit.gameObject.GetComponent<Item>();
        if (item != null && carriedItem == null && !item.IsPickedUp && !item.IsCollected)
        {
            // Chỉ lượm được nếu chưa có item nào đang mang
            // Lấy ItemPoint
            Transform itemPointTransform = GetItemPoint();
            
            // Lượm item (chưa tính điểm)
            item.PickupItem(itemPointTransform);
            
            // Thông báo cho PlayerController
            PickupItem(item);
        }
    }
    
    /// <summary>
    /// Lượm item (chỉ lượm được 1 item) - được gọi từ Item
    /// </summary>
    public void PickupItem(Item item)
    {
        if (carriedItem != null)
        {
            Debug.Log("Đã có item đang mang theo, không thể lượm thêm!");
            return;
        }
        
        carriedItem = item;
    }
    
    /// <summary>
    /// Thả item tại checkpoint
    /// </summary>
    public void DropItemAtCheckpoint(Transform checkpointPosition)
    {
        if (carriedItem == null)
        {
            Debug.Log("Không có item để thả!");
            return;
        }
        
        // Lưu item type trước khi thả
        ItemType itemType = carriedItem.ItemType;
        
        // Thả item tại checkpoint với callback để tính điểm sau khi animation hoàn thành
        carriedItem.DropItemAtCheckpoint(checkpointPosition, () =>
        {
            // Tính điểm khi animation thả item hoàn thành
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnItemCollected(itemType);
            }
        });
        
        // Reset carried item
        carriedItem = null;
    }
    
    /// <summary>
    /// Kiểm tra xem có đang mang item không
    /// </summary>
    public bool HasCarriedItem()
    {
        return carriedItem != null;
    }
    
    /// <summary>
    /// Lấy item đang mang theo
    /// </summary>
    public Item GetCarriedItem()
    {
        return carriedItem;
    }
    
    /// <summary>
    /// Quay về spawn point
    /// </summary>
    private void ReturnToSpawnPoint()
    {
        // Đánh dấu đang trong quá trình về spawn để tránh xử lý nhiều lần
        if (isReturningToSpawn)
        {
            return;
        }
        
        isReturningToSpawn = true;
        lastSpawnReturnTime = Time.time;
        
        AudioManager.Instance.PlayFallSound();
        
        // Trừ 1 mạng khi về spawn point
        if (HealthPanel.Instance != null)
        {
            bool stillHasLives = HealthPanel.Instance.LoseLife();
            
            // Nếu hết mạng, không cần teleport nữa vì đã hiển thị lose panel
            if (!stillHasLives)
            {
                Debug.Log("Player đã hết mạng! Không thể tiếp tục.");
                isReturningToSpawn = false; // Reset flag
                return;
            }
        }
        
        // Tắt CharacterController tạm thời để teleport
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // Teleport về spawn point
        Vector3 targetPosition = spawnPoint != null ? spawnPoint.position : initialSpawnPosition;
        transform.position = targetPosition;
        
        // Bật lại CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        Debug.Log("Player đã quay về spawn point!");
        
        // Reset flag sau một khoảng thời gian ngắn để cho phép xử lý lần tiếp theo
        StartCoroutine(ResetSpawnReturnFlag());
    }
    
    /// <summary>
    /// Reset flag sau khi hoàn thành quá trình về spawn point
    /// </summary>
    private System.Collections.IEnumerator ResetSpawnReturnFlag()
    {
        yield return new WaitForSeconds(spawnReturnCooldown);
        isReturningToSpawn = false;
    }
    
    /// <summary>
    /// Disable điều khiển sau khi bị hit và enable lại sau một khoảng thời gian
    /// </summary>
    private System.Collections.IEnumerator DisableControlAfterHit()
    {
        // Lưu trạng thái input ban đầu
        bool originalInputState = canReceiveInput;
        
        // Disable input
        canReceiveInput = false;
        
        // Đợi 0.5s
        yield return new WaitForSeconds(hitControlDisableDuration);
        
        // Enable lại input (chỉ nếu không bị disable bởi lý do khác)
        if (!isDisable)
        {
            canReceiveInput = originalInputState;
        }
    }
    
    /// <summary>
    /// Spawn VFX effect khi va chạm với xe
    /// </summary>
    private void SpawnHitVFX(Vector3 collisionPoint)
    {
        if (hitVFXPrefab == null)
            return;
        
        // Xác định vị trí spawn VFX
        Vector3 spawnPosition = collisionPoint;
        if (hitVFXSpawnPoint != null)
        {
            spawnPosition = hitVFXSpawnPoint.position;
        }
        
        // Spawn VFX
        GameObject vfx = Instantiate(hitVFXPrefab, spawnPosition, Quaternion.identity);
        
        // Tự động destroy VFX sau một khoảng thời gian (nếu VFX không tự destroy)
        // Có thể điều chỉnh thời gian tùy theo VFX của bạn
        Destroy(vfx, 3f);
    }
    
    /// <summary>
    /// Spawn VFX effect khi nhặt Health Item tại VFX point
    /// </summary>
    public void SpawnHealthPickupVFX()
    {
        if (healthPickupVFXPrefab == null)
            return;
        
        // Xác định vị trí spawn VFX
        Vector3 spawnPosition = transform.position;
        Transform parentTransform = null;
        if (pickupVFXPoint != null)
        {
            spawnPosition = pickupVFXPoint.position;
            parentTransform = pickupVFXPoint;
        }
        
        // Spawn VFX và set làm con của pickupVFXPoint
        GameObject vfx = Instantiate(healthPickupVFXPrefab, spawnPosition, Quaternion.identity, parentTransform);
        
        // Tự động destroy VFX sau một khoảng thời gian (nếu VFX không tự destroy)
        Destroy(vfx, 0.5f);
    }
    
    /// <summary>
    /// Spawn VFX effect khi nhặt Speed Item tại VFX point
    /// </summary>
    public void SpawnSpeedPickupVFX()
    {
        if (speedPickupVFXPrefab == null)
            return;
        
        // Xác định vị trí spawn VFX
        Vector3 spawnPosition = transform.position;
        Transform parentTransform = null;
        if (pickupVFXPoint != null)
        {
            spawnPosition = pickupVFXPoint.position;
            parentTransform = pickupVFXPoint;
        }
        
        // Spawn VFX và set làm con của pickupVFXPoint
        GameObject vfx = Instantiate(speedPickupVFXPrefab, spawnPosition, Quaternion.identity, parentTransform);
        
        // Tự động destroy VFX sau một khoảng thời gian (nếu VFX không tự destroy)
        Destroy(vfx, 4f);
    }
    
    /// <summary>
    /// Hiển thị victory panel khi đến end gate
    /// </summary>
    private void ShowVictory()
    {
        // Kiểm tra xem đã collect đủ animal và đến endgate chưa
        if (QuestManager.Instance != null)
        {
            // Kiểm tra và hoàn thành quest nếu đã collect đủ
            QuestManager.Instance.CheckAndCompleteQuest();
        }
        else
        {
            Debug.LogWarning("QuestManager không tồn tại!");
        }
    }
    
    /// <summary>
    /// Set spawn point mới
    /// </summary>
    public void SetSpawnPoint(Transform newSpawnPoint)
    {
        spawnPoint = newSpawnPoint;
        if (spawnPoint != null)
        {
            initialSpawnPosition = spawnPoint.position;
        }
    }

    #endregion

    #region Public Methods

    public void SetDisable(bool disable)
    {
        isDisable = disable;
        
        if (characterController != null)
        {
            characterController.enabled = !disable;
        }
        
        if (disable)
        {
            SetIdleAnimation();
        }
    }

    /// <summary>
    /// Set cho phép nhận input hay không
    /// </summary>
    public void SetCanReceiveInput(bool canReceive)
    {
        canReceiveInput = canReceive;
    }

    public void SetIdleAnimation()
    {
        // Set movement to idle (speed = 0)
        playerAnimation?.SetMovement(false, 0f);
    }

    public GameObject GetModel()
    {
        return model;
    }

    /// <summary>
    /// Lấy ItemPoint transform
    /// </summary>
    public Transform GetItemPoint()
    {
        return itemPoint;
    }
    
    /// <summary>
    /// Kích hoạt speed boost cho player
    /// </summary>
    /// <param name="boostAmount">Tốc độ tăng thêm</param>
    /// <param name="duration">Thời gian boost (giây)</param>
    public void ActivateSpeedBoost(float boostAmount, float duration)
    {
        // Nếu đang có speed boost, reset lại thời gian
        if (isSpeedBoosted)
        {
            StopCoroutine("SpeedBoostCoroutine");
        }
        
        StartCoroutine(SpeedBoostCoroutine(boostAmount, duration));
    }
    
    /// <summary>
    /// Coroutine để tăng tốc độ trong thời gian nhất định
    /// </summary>
    private IEnumerator SpeedBoostCoroutine(float boostAmount, float duration)
    {
        isSpeedBoosted = true;
        moveSpeed = baseMoveSpeed + boostAmount;
        
        Debug.Log($"Speed Boost activated! Tốc độ: {moveSpeed} (tăng {boostAmount})");
        
        yield return new WaitForSeconds(duration);
        
        // Trở về tốc độ gốc
        moveSpeed = baseMoveSpeed;
        isSpeedBoosted = false;
        
        Debug.Log($"Speed Boost hết hạn! Tốc độ về: {baseMoveSpeed}");
    }

    #endregion
}

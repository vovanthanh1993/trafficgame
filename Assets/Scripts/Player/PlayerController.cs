using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    
    [Header("Slow Skill Settings")]
    [Tooltip("Thời gian cooldown của slow skill (giây)")]
    [SerializeField] private float slowSkillCooldown = 60f;
    [Tooltip("Thời gian delay sau khi slow skill kết thúc trước khi spawn lại xe/animal (giây)")]
    [SerializeField] private float slowSkillSpawnDelay = 2f;
    private float lastSlowSkillTime; // Thời gian lần cuối kích hoạt slow skill
    private float slowSkillEndTime = -1f; // Thời gian slow skill kết thúc (-1 = không active)
    
    [Header("Camera Settings")]
    [SerializeField] private Transform camTarget;

    [Header("Item Collection")]
    [Tooltip("Điểm để hiển thị item khi đã nhặt")]
    [SerializeField] private Transform itemPoint;
    
    [Tooltip("Khoảng cách offset giữa các items khi xếp chồng (theo trục Y)")]
    [SerializeField] private float itemStackOffset = 0.4f;
    
    // Danh sách items đang được mang theo (có thể nhặt nhiều items)
    private List<Item> carriedItems = new List<Item>();

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
        
        // Khởi tạo slow skill để sẵn sàng ngay từ đầu
        lastSlowSkillTime = Time.time - slowSkillCooldown - 1f;
        
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
                
                // Nếu đang mang items, rớt 2 item nhặt vào sau cùng (2 item cuối cùng)
                if (carriedItems.Count > 0)
                {
                    // Lấy số lượng item cần rớt (tối đa 2 item)
                    int itemsToDrop = Mathf.Min(2, carriedItems.Count);
                    
                    // Rớt 2 item cuối cùng (nhặt vào sau cùng) và update UI
                    for (int i = 0; i < itemsToDrop; i++)
                    {
                        if (carriedItems.Count > 0)
                        {
                            // Lấy item cuối cùng (nhặt vào sau cùng)
                            int lastIndex = carriedItems.Count - 1;
                            Item item = carriedItems[lastIndex];
                            if (item != null)
                            {
                                ItemType droppedItemType = item.ItemType;
                                item.ReturnToOriginalPosition();
                                
                                // Xóa item khỏi danh sách (xóa item cuối cùng)
                                carriedItems.RemoveAt(lastIndex);
                                
                                // Update UI quest khi drop item
                                if (QuestManager.Instance != null)
                                {
                                    QuestManager.Instance.OnItemDropped(droppedItemType);
                                }
                            }
                        }
                    }
                }
                
                // Disable điều khiển trong 0.5s sau khi bị hit
                StartCoroutine(DisableControlAfterHit());
                
                // Bay về spawn point và trừ mạng (khi va chạm với xe)
                ReturnToSpawnPoint();
            }
        }
        
        // Kiểm tra va chạm với animal (animal có isTrigger = true)
        AnimalController animal = other.GetComponent<AnimalController>();
        if (animal != null)
        {
            // Trigger animation hit và bay về spawn point khi va chạm với animal (có cooldown)
            if (Time.time - lastHitTime >= hitAnimationCooldown)
            {
                // Spawn VFX effect khi va chạm
                SpawnHitVFX(transform.position);
                
                if (playerAnimation != null)
                {
                    playerAnimation.SetHit();
                    lastHitTime = Time.time;
                }
                
                // Nếu đang mang items, rớt 2 item nhặt vào sau cùng (2 item cuối cùng)
                if (carriedItems.Count > 0)
                {
                    // Lấy số lượng item cần rớt (tối đa 2 item)
                    int itemsToDrop = Mathf.Min(2, carriedItems.Count);
                    
                    // Rớt 2 item cuối cùng (nhặt vào sau cùng) và update UI
                    for (int i = 0; i < itemsToDrop; i++)
                    {
                        if (carriedItems.Count > 0)
                        {
                            // Lấy item cuối cùng (nhặt vào sau cùng)
                            int lastIndex = carriedItems.Count - 1;
                            Item item = carriedItems[lastIndex];
                            if (item != null)
                            {
                                ItemType droppedItemType = item.ItemType;
                                item.ReturnToOriginalPosition();
                                
                                // Xóa item khỏi danh sách (xóa item cuối cùng)
                                carriedItems.RemoveAt(lastIndex);
                                
                                // Update UI quest khi drop item
                                if (QuestManager.Instance != null)
                                {
                                    QuestManager.Instance.OnItemDropped(droppedItemType);
                                }
                            }
                        }
                    }
                }
                
                // Disable điều khiển trong 0.5s sau khi bị hit
                StartCoroutine(DisableControlAfterHit());
                
                // Bay về spawn point nhưng KHÔNG trừ mạng (khi va chạm với animal)
                ReturnToSpawnPointWithoutLosingLife();
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
        if (item != null && !item.IsPickedUp && !item.IsCollected)
        {
            // Có thể nhặt nhiều items
            // Tính toán vị trí item point với offset dựa trên số lượng items đã có
            Transform itemPointTransform = GetItemPointForNewItem();
            
            // Lượm item (chưa tính điểm)
            item.PickupItem(itemPointTransform);
            
            // Thông báo cho PlayerController
            PickupItem(item);
        }
    }
    
    /// <summary>
    /// Lượm item (có thể nhặt nhiều items) - được gọi từ Item
    /// </summary>
    public void PickupItem(Item item)
    {
        if (item == null)
        {
            return;
        }
        
        // Thêm item vào danh sách
        if (!carriedItems.Contains(item))
        {
            carriedItems.Add(item);
            
            // Update UI quest ngay khi nhặt item
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnItemPickedUp(item.ItemType);
            }
            
            Debug.Log($"Đã nhặt item {item.ItemType}. Tổng số items: {carriedItems.Count}");
        }
    }
    
    /// <summary>
    /// Thả tất cả items tại checkpoint (giữ lại để tương thích, nhưng Checkpoint sẽ tự xử lý)
    /// </summary>
    public void DropItemAtCheckpoint(Transform checkpointPosition)
    {
        if (carriedItems.Count == 0)
        {
            Debug.Log("Không có item để thả!");
            return;
        }
        
        // Thả tất cả items tại checkpoint (dùng cùng một vị trí nếu được gọi trực tiếp)
        foreach (Item item in carriedItems.ToList())
        {
            if (item != null)
            {
                // Thả item tại checkpoint với callback để tính điểm sau khi animation hoàn thành
                // Lưu ý: Progress đã được tăng khi nhặt item, nên không cần tăng lại ở đây
                item.DropItemAtCheckpoint(checkpointPosition, () =>
                {
                    // Chỉ update UI để refresh (progress đã được tăng khi nhặt item)
                    if (QuestManager.Instance != null)
                    {
                        QuestManager.Instance.UpdateQuestUI();
                    }
                });
            }
        }
        
        // Clear danh sách items
        carriedItems.Clear();
    }
    
    /// <summary>
    /// Kiểm tra xem có đang mang item không
    /// </summary>
    public bool HasCarriedItem()
    {
        return carriedItems.Count > 0;
    }
    
    /// <summary>
    /// Lấy số lượng items đang mang
    /// </summary>
    public int GetCarriedItemCount()
    {
        return carriedItems.Count;
    }
    
    /// <summary>
    /// Lấy danh sách items đang mang theo
    /// </summary>
    public List<Item> GetCarriedItems()
    {
        return new List<Item>(carriedItems);
    }
    
    /// <summary>
    /// Clear tất cả items đang mang (không drop, chỉ xóa khỏi danh sách)
    /// </summary>
    public void ClearCarriedItems()
    {
        carriedItems.Clear();
    }
    
    /// <summary>
    /// Lấy item đang mang theo (giữ lại để tương thích, trả về item đầu tiên)
    /// </summary>
    public Item GetCarriedItem()
    {
        return carriedItems.Count > 0 ? carriedItems[0] : null;
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
        
        // Tìm spawn point gần nhất có x < player.x
        Vector3 targetPosition = initialSpawnPosition; // Fallback về vị trí ban đầu
        
        if (PlayerSpawnPointManager.Instance != null)
        {
            Transform nearestSpawnPoint = PlayerSpawnPointManager.Instance.FindNearestSpawnPointBehindPlayer(transform.position);
            if (nearestSpawnPoint != null)
            {
                targetPosition = nearestSpawnPoint.position;
                Debug.Log($"Player sẽ spawn tại spawn point: {nearestSpawnPoint.name}");
            }
            else
            {
                // Nếu không tìm thấy, dùng spawn point cũ hoặc initial position
                targetPosition = spawnPoint != null ? spawnPoint.position : initialSpawnPosition;
                Debug.LogWarning("Không tìm thấy spawn point phù hợp, dùng spawn point mặc định");
            }
        }
        else
        {
            // Nếu không có PlayerSpawnPointManager, dùng spawn point cũ
            targetPosition = spawnPoint != null ? spawnPoint.position : initialSpawnPosition;
        }
        
        // Teleport về spawn point
        transform.position = targetPosition;
        
        // Bật lại CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        Debug.Log($"Player đã quay về spawn point tại {targetPosition}!");
        
        // Reset flag sau một khoảng thời gian ngắn để cho phép xử lý lần tiếp theo
        StartCoroutine(ResetSpawnReturnFlag());
    }
    
    /// <summary>
    /// Quay về spawn point nhưng KHÔNG trừ mạng (dùng khi va chạm với animal)
    /// </summary>
    private void ReturnToSpawnPointWithoutLosingLife()
    {
        // Đánh dấu đang trong quá trình về spawn để tránh xử lý nhiều lần
        if (isReturningToSpawn)
        {
            return;
        }
        
        isReturningToSpawn = true;
        lastSpawnReturnTime = Time.time;
        
        AudioManager.Instance.PlayFallSound();
        
        // KHÔNG trừ mạng khi va chạm với animal
        
        // Tắt CharacterController tạm thời để teleport
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // Tìm spawn point gần nhất có x < player.x
        Vector3 targetPosition = initialSpawnPosition; // Fallback về vị trí ban đầu
        
        if (PlayerSpawnPointManager.Instance != null)
        {
            Transform nearestSpawnPoint = PlayerSpawnPointManager.Instance.FindNearestSpawnPointBehindPlayer(transform.position);
            if (nearestSpawnPoint != null)
            {
                targetPosition = nearestSpawnPoint.position;
                Debug.Log($"Player sẽ spawn tại spawn point: {nearestSpawnPoint.name} (không trừ mạng)");
            }
            else
            {
                // Nếu không tìm thấy, dùng spawn point cũ hoặc initial position
                targetPosition = spawnPoint != null ? spawnPoint.position : initialSpawnPosition;
                Debug.LogWarning("Không tìm thấy spawn point phù hợp, dùng spawn point mặc định");
            }
        }
        else
        {
            // Nếu không có PlayerSpawnPointManager, dùng spawn point cũ
            targetPosition = spawnPoint != null ? spawnPoint.position : initialSpawnPosition;
        }
        
        // Teleport về spawn point
        transform.position = targetPosition;
        
        // Bật lại CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        Debug.Log($"Player đã quay về spawn point tại {targetPosition} (không trừ mạng)!");
        
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
    /// Kiểm tra win khi đến checkpoint (progress đã được tính khi nhặt item)
    /// </summary>
    private void ShowVictory()
    {
        // Kiểm tra xem đã nhặt đủ items chưa (progress đã được tính khi nhặt)
        if (QuestManager.Instance != null)
        {
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
    /// Lấy ItemPoint transform với offset cho item mới (để xếp chồng nhiều items)
    /// </summary>
    public Transform GetItemPointForNewItem()
    {
        if (itemPoint == null)
        {
            return null;
        }
        
        // Tính toán offset dựa trên số lượng items hiện có
        int itemCount = carriedItems.Count;
        float offsetY = itemCount * itemStackOffset;
        
        Debug.Log($"GetItemPointForNewItem: itemCount = {itemCount}, itemStackOffset = {itemStackOffset}, offsetY = {offsetY}");
        Debug.Log($"GetItemPointForNewItem: itemPoint scale = {itemPoint.localScale}, position = {itemPoint.position}");
        
        // Tạo một GameObject tạm thời để làm item point với offset
        // GameObject này sẽ được cleanup khi item được drop hoặc return
        GameObject tempItemPoint = new GameObject($"ItemPoint_{itemCount}");
        tempItemPoint.transform.SetParent(itemPoint);
        
        // Nếu itemPoint có scale khác 1, cần điều chỉnh offset
        // localPosition sẽ bị scale bởi parent scale
        Vector3 parentScale = itemPoint.localScale;
        float adjustedOffsetY = offsetY / parentScale.y; // Điều chỉnh offset theo scale của parent
        
        tempItemPoint.transform.localPosition = new Vector3(0, adjustedOffsetY, 0);
        tempItemPoint.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"GetItemPointForNewItem: Created tempItemPoint at localPosition (0, {adjustedOffsetY}, 0), worldPosition = {tempItemPoint.transform.position}, parentScale.y = {parentScale.y}");
        
        return tempItemPoint.transform;
    }
    
    /// <summary>
    /// Kiểm tra xem slow skill có sẵn sàng sử dụng không (đã hết cooldown)
    /// </summary>
    public bool IsSlowSkillReady()
    {
        float timeSinceLastUse = Time.time - lastSlowSkillTime;
        return timeSinceLastUse >= slowSkillCooldown;
    }
    
    /// <summary>
    /// Lấy thời gian cooldown còn lại của slow skill (giây)
    /// </summary>
    public float GetSlowSkillCooldownRemaining()
    {
        float timeSinceLastUse = Time.time - lastSlowSkillTime;
        float remaining = slowSkillCooldown - timeSinceLastUse;
        return Mathf.Max(0f, remaining);
    }
    
    /// <summary>
    /// Lấy tổng thời gian cooldown của slow skill (giây)
    /// </summary>
    public float GetSlowSkillCooldownTotal()
    {
        return slowSkillCooldown;
    }
    
    /// <summary>
    /// Kiểm tra xem slow skill có đang active không (đang trong thời gian slow effect)
    /// </summary>
    public bool IsSlowSkillActive()
    {
        return slowSkillEndTime > 0 && Time.time < slowSkillEndTime;
    }
    
    /// <summary>
    /// Kiểm tra xem có đang trong thời gian không được spawn (slow skill active hoặc delay sau slow skill)
    /// </summary>
    public bool IsInSlowSkillSpawnBlock()
    {
        if (slowSkillEndTime <= 0)
            return false; // Slow skill chưa từng được kích hoạt
        
        // Kiểm tra xem có đang trong thời gian slow skill hoặc delay sau slow skill không
        float spawnBlockEndTime = slowSkillEndTime + slowSkillSpawnDelay;
        return Time.time < spawnBlockEndTime;
    }
    
    /// <summary>
    /// Kích hoạt skill làm chậm tốc độ xe và animal trong 3 giây
    /// </summary>
    /// <param name="slowPercent">Phần trăm giảm tốc độ (0.8 = giảm 80%, còn 20% tốc độ, mặc định)</param>
    /// <param name="duration">Thời gian slow effect (giây, mặc định 3 giây)</param>
    /// <returns>True nếu skill được kích hoạt thành công, False nếu đang trong cooldown</returns>
    public bool ActivateSlowSkill(float slowPercent = 0.8f, float duration = 3f)
    {
        // Kiểm tra cooldown
        if (!IsSlowSkillReady())
        {
            float remaining = GetSlowSkillCooldownRemaining();
            Debug.LogWarning($"PlayerController: Slow skill đang trong cooldown! Còn lại {remaining:F1} giây");
            return false;
        }
        
        // Cập nhật thời gian sử dụng skill
        lastSlowSkillTime = Time.time;
        slowSkillEndTime = Time.time + duration; // Track thời gian slow skill kết thúc
        
        Debug.Log($"PlayerController: Kích hoạt slow skill - Giảm {slowPercent * 100}% tốc độ trong {duration} giây");
        
        // Tìm tất cả CarController trong scene
        CarController[] allCars = FindObjectsOfType<CarController>();
        foreach (CarController car in allCars)
        {
            if (car != null)
            {
                car.ApplySlowEffect(slowPercent, duration);
            }
        }
        
        // Tìm tất cả AnimalController trong scene
        AnimalController[] allAnimals = FindObjectsOfType<AnimalController>();
        foreach (AnimalController animal in allAnimals)
        {
            if (animal != null)
            {
                animal.ApplySlowEffect(slowPercent, duration);
            }
        }
        
        Debug.Log($"PlayerController: Đã áp dụng slow effect cho {allCars.Length} xe và {allAnimals.Length} animal. Cooldown: {slowSkillCooldown} giây");
        
        return true;
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

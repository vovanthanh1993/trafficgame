using UnityEngine;
using System.Collections;

/// <summary>
/// Script cho item có thể collect
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Item Settings")]
    [Tooltip("Loại item này")]
    [SerializeField] private ItemType itemType = ItemType.Apple;
    
    [Tooltip("Effect khi collect (particle, sound, etc.)")]
    [SerializeField] private GameObject collectEffect;
    
    [Header("Pickup Animation")]
    [Tooltip("Thời gian animation thu nhỏ và bay lên (giây)")]
    [SerializeField] private float pickupAnimationDuration = 0.5f;
    
    [Tooltip("Scale cuối cùng khi thu nhỏ (0.5 = 50% kích thước ban đầu)")]
    [SerializeField] private float finalScale = 0.5f;
    
    [Tooltip("Độ cao bay lên trước khi đến ItemPoint")]
    [SerializeField] private float flyHeight = 2f;
    
    [Header("Drop Animation")]
    [Tooltip("Thời gian animation phóng to lại khi thả tại checkpoint (giây)")]
    [SerializeField] private float dropAnimationDuration = 0.3f;
    
    [Header("Animation Settings")]
    [Tooltip("Animator component của item (để trigger animation jump)")]
    [SerializeField] private Animator animator;
    
    [Tooltip("Tên trigger parameter cho animation jump (ví dụ: 'Jump', 'Landing')")]
    [SerializeField] private string jumpTriggerParameter = "Jump";
    
    [Tooltip("Thời gian giữa mỗi lần jump (giây)")]
    [SerializeField] private float jumpInterval = 2f;
    
    private bool isCollected = false;
    private bool isPickedUp = false; // Đã được lượm nhưng chưa thả tại checkpoint
    private Vector3 originalScale; // Lưu scale ban đầu
    private Vector3 originalPosition; // Lưu vị trí spawn ban đầu
    private Quaternion originalRotation; // Lưu rotation spawn ban đầu
    private bool isAnimating = false; // Đang trong quá trình animation
    private Coroutine jumpCoroutine; // Coroutine để jump lặp lại
    
    public ItemType ItemType => itemType;
    public bool IsPickedUp => isPickedUp;
    public bool IsCollected => isCollected;
    
    private void Start()
    {
        // Lưu scale ban đầu
        originalScale = transform.localScale;
        
        // Lưu vị trí và rotation spawn ban đầu
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Tự động tìm Animator nếu chưa được assign
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                // Tìm trong children
                animator = GetComponentInChildren<Animator>();
            }
        }
    }
    
    /// <summary>
    /// Set item type (dùng khi spawn động)
    /// </summary>
    public void SetItemType(ItemType type)
    {
        itemType = type;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Chỉ player mới có thể lượm
        if (collision.gameObject.CompareTag("Player") && !isPickedUp && !isCollected)
        {
            // Kiểm tra xem player đã có item chưa
            if (PlayerController.Instance != null && !PlayerController.Instance.HasCarriedItem())
            {
                // Lấy ItemPoint từ PlayerController
                Transform itemPoint = PlayerController.Instance.GetItemPoint();
                
                // Lượm item (chưa tính điểm)
                PickupItem(itemPoint);
                
                // Thông báo cho PlayerController
                PlayerController.Instance.PickupItem(this);
            }
        }
    }
    
    /// <summary>
    /// Lượm item (chưa tính điểm, chỉ khi thả tại checkpoint mới tính)
    /// </summary>
    /// <param name="itemPoint">Điểm để hiển thị item khi đã lượm</param>
    public void PickupItem(Transform itemPoint)
    {
        if (isPickedUp || isCollected || isAnimating) return;
        
        isPickedUp = true;
        isAnimating = true;
        
        // Play collect sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollectSound();
        }
        
        // Spawn effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Nếu có itemPoint, bắt đầu animation thu nhỏ và bay lên
        if (itemPoint != null)
        {
            // Tắt các component không cần thiết
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            
            // Tắt AI/NavMeshAgent nếu có
            UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null) navAgent.enabled = false;
            
            // Bắt đầu coroutine animation
            StartCoroutine(AnimatePickup(itemPoint));
        }
        else
        {
            // Nếu không có itemPoint, ẩn item
            gameObject.SetActive(false);
            isAnimating = false;
        }
    }
    
    /// <summary>
    /// Coroutine để animate việc thu nhỏ và bay lên ItemPoint
    /// </summary>
    private IEnumerator AnimatePickup(Transform itemPoint)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * finalScale;
        
        // Tính toán vị trí đích (ItemPoint trong world space)
        Vector3 targetPosition = itemPoint.position;
        
        // Tạo điểm giữa để bay lên cao trước
        Vector3 midPosition = Vector3.Lerp(startPosition, targetPosition, 0.5f);
        midPosition.y += flyHeight;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < pickupAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / pickupAnimationDuration;
            
            // Easing function (ease out)
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Tính toán vị trí theo đường cong (bay lên cao rồi xuống)
            Vector3 currentPosition;
            if (t < 0.5f)
            {
                // Nửa đầu: bay lên điểm giữa
                float localT = t * 2f;
                currentPosition = Vector3.Lerp(startPosition, midPosition, localT);
            }
            else
            {
                // Nửa sau: bay xuống ItemPoint
                float localT = (t - 0.5f) * 2f;
                currentPosition = Vector3.Lerp(midPosition, targetPosition, localT);
            }
            
            // Cập nhật vị trí và scale
            transform.position = currentPosition;
            transform.localScale = Vector3.Lerp(startScale, targetScale, easeT);
            
            yield return null;
        }
        
        // Đảm bảo đạt đúng vị trí và scale cuối cùng
        transform.position = targetPosition;
        transform.localScale = targetScale;
        
        // Set parent và đặt local position
        transform.SetParent(itemPoint);
        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector3.zero;
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Thả item tại checkpoint và tính điểm
    /// </summary>
    /// <param name="checkpointPosition">Vị trí checkpoint để thả item</param>
    /// <param name="onComplete">Callback được gọi sau khi animation hoàn thành</param>
    public void DropItemAtCheckpoint(Transform checkpointPosition, System.Action onComplete = null)
    {
        if (!isPickedUp) return;
        
        isCollected = true;
        isPickedUp = false;
        isAnimating = true;
        
        // Bật lại các component để item có thể hiển thị tại checkpoint
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Giữ kinematic để không rơi
        }
        
        // Bắt đầu animation bay từ ItemPoint đến checkpoint và phóng to lại
        if (checkpointPosition != null)
        {
            StartCoroutine(AnimateDrop(checkpointPosition, onComplete));
        }
        else
        {
            // Nếu không có checkpoint position, chỉ phóng to tại chỗ
            StartCoroutine(AnimateDrop(null, onComplete));
        }
    }
    
    /// <summary>
    /// Coroutine để animate việc bay từ ItemPoint đến checkpoint và phóng to lại về kích thước ban đầu
    /// </summary>
    private IEnumerator AnimateDrop(Transform checkpointPosition, System.Action onComplete = null)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale; // Hiện tại là 50%
        Vector3 targetScale = originalScale; // 100%
        
        Vector3 targetPosition;
        Quaternion targetRotation;
        
        if (checkpointPosition != null)
        {
            targetPosition = checkpointPosition.position;
            // Giữ nguyên rotation y = 180 khi bay về drop point
            targetRotation = Quaternion.Euler(0, 180, 0);
        }
        else
        {
            // Nếu không có checkpoint, chỉ phóng to tại chỗ và giữ rotation y = 180
            targetPosition = startPosition;
            targetRotation = Quaternion.Euler(0, 180, 0);
        }
        
        // Tạo điểm giữa để bay lên cao trước (chỉ khi có checkpoint)
        Vector3 midPosition = Vector3.Lerp(startPosition, targetPosition, 0.5f);
        if (checkpointPosition != null)
        {
            midPosition.y += flyHeight;
        }
        
        // Remove parent trước khi bắt đầu animation
        transform.SetParent(null);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < dropAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dropAnimationDuration;
            
            // Easing function (ease out)
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Tính toán vị trí theo đường cong (bay lên cao rồi xuống) nếu có checkpoint
            Vector3 currentPosition;
            if (checkpointPosition != null)
            {
                if (t < 0.5f)
                {
                    // Nửa đầu: bay lên điểm giữa
                    float localT = t * 2f;
                    currentPosition = Vector3.Lerp(startPosition, midPosition, localT);
                }
                else
                {
                    // Nửa sau: bay xuống checkpoint
                    float localT = (t - 0.5f) * 2f;
                    currentPosition = Vector3.Lerp(midPosition, targetPosition, localT);
                }
            }
            else
            {
                // Không có checkpoint, giữ nguyên vị trí
                currentPosition = startPosition;
            }
            
            // Cập nhật vị trí, rotation và scale
            transform.position = currentPosition;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, easeT);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easeT);
            
            yield return null;
        }
        
        // Đảm bảo đạt đúng vị trí, rotation và scale cuối cùng
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        transform.localScale = targetScale;
        
        // Trigger animation jump khi đến drop point và bắt đầu jump lặp lại
        TriggerJumpAnimation();
        StartJumpLoop();
        
        isAnimating = false;
        
        // Gọi callback khi animation hoàn thành
        if (onComplete != null)
        {
            onComplete();
        }
    }
    
    /// <summary>
    /// Trigger animation jump khi item đến drop point
    /// </summary>
    private void TriggerJumpAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(jumpTriggerParameter))
        {
            animator.SetTrigger(jumpTriggerParameter);
        }
    }
    
    /// <summary>
    /// Bắt đầu loop jump mỗi 2 giây
    /// </summary>
    private void StartJumpLoop()
    {
        // Dừng coroutine cũ nếu có
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
        }
        
        // Bắt đầu coroutine mới
        jumpCoroutine = StartCoroutine(JumpLoop());
    }
    
    /// <summary>
    /// Coroutine để jump lặp lại mỗi 2 giây
    /// </summary>
    private IEnumerator JumpLoop()
    {
        while (isCollected && !isPickedUp)
        {
            yield return new WaitForSeconds(jumpInterval);
            
            // Chỉ jump nếu item đã được thả (isCollected = true và không đang được mang)
            if (isCollected && !isPickedUp)
            {
                TriggerJumpAnimation();
            }
        }
        
        jumpCoroutine = null;
    }
    
    /// <summary>
    /// Dừng jump loop
    /// </summary>
    private void StopJumpLoop()
    {
        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }
    }
    
    /// <summary>
    /// Collect item này (có thể gọi từ bên ngoài, ví dụ từ PlayerController)
    /// DEPRECATED: Dùng PickupItem và DropItemAtCheckpoint thay thế
    /// </summary>
    /// <param name="itemPoint">Điểm để hiển thị item khi đã nhặt (nếu null sẽ ẩn item)</param>
    public void CollectItem(Transform itemPoint = null)
    {
        PickupItem(itemPoint);
    }
    
    /// <summary>
    /// Bay về vị trí spawn ban đầu (khi player bị hit)
    /// </summary>
    public void ReturnToOriginalPosition()
    {
        if (!isPickedUp) return; // Chỉ bay về nếu đang được mang
        
        isPickedUp = false;
        isAnimating = true;
        
        // Dừng tất cả coroutines đang chạy (đặc biệt là AnimatePickup)
        StopAllCoroutines();
        
        // Remove parent ngay lập tức để item không còn ở ItemPoint
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        
        // Đảm bảo item không còn ở vị trí ItemPoint bằng cách lấy world position
        // Nếu item đang là con của ItemPoint, localPosition sẽ là (0,0,0)
        // Cần lấy world position trước khi remove parent
        Vector3 currentWorldPosition = transform.position;
        
        // Bắt đầu animation bay về vị trí ban đầu
        StartCoroutine(AnimateReturnToOriginalPosition(currentWorldPosition));
    }
    
    /// <summary>
    /// Coroutine để animate việc bay về vị trí spawn ban đầu
    /// </summary>
    private IEnumerator AnimateReturnToOriginalPosition(Vector3 startPosition)
    {
        // Sử dụng startPosition được truyền vào để đảm bảo đúng vị trí bắt đầu
        Vector3 startScale = transform.localScale; // Hiện tại có thể là 50%
        Vector3 targetScale = originalScale; // 100%
        
        // Tạo điểm giữa để bay lên cao trước
        Vector3 midPosition = Vector3.Lerp(startPosition, originalPosition, 0.5f);
        midPosition.y += flyHeight;
        
        float elapsedTime = 0f;
        float returnAnimationDuration = pickupAnimationDuration; // Dùng cùng thời gian với pickup animation
        
        while (elapsedTime < returnAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / returnAnimationDuration;
            
            // Easing function (ease out)
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Tính toán vị trí theo đường cong (bay lên cao rồi xuống)
            Vector3 currentPosition;
            if (t < 0.5f)
            {
                // Nửa đầu: bay lên điểm giữa
                float localT = t * 2f;
                currentPosition = Vector3.Lerp(startPosition, midPosition, localT);
            }
            else
            {
                // Nửa sau: bay xuống vị trí ban đầu
                float localT = (t - 0.5f) * 2f;
                currentPosition = Vector3.Lerp(midPosition, originalPosition, localT);
            }
            
            // Cập nhật vị trí, rotation và scale
            transform.position = currentPosition;
            transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, easeT);
            transform.localScale = Vector3.Lerp(startScale, targetScale, easeT);
            
            yield return null;
        }
        
        // Đảm bảo đạt đúng vị trí, rotation và scale cuối cùng
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = targetScale;
        
        // Bật lại các component để item có thể được lượm lại
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        
        // Bật lại AI/NavMeshAgent nếu có
        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null) navAgent.enabled = true;
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Reset item để có thể collect lại (dùng khi restart level)
    /// </summary>
    public void ResetItem()
    {
        isCollected = false;
        isPickedUp = false;
        isAnimating = false;
        
        // Dừng jump loop
        StopJumpLoop();
        
        // Dừng tất cả coroutines
        StopAllCoroutines();
        
        // Khôi phục scale ban đầu
        transform.localScale = originalScale;
        
        // Khôi phục vị trí và rotation ban đầu
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        
        // Bật lại các component
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        
        // Bật lại AI/NavMeshAgent nếu có
        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null) navAgent.enabled = true;
        
        // Remove parent nếu đang là con của ItemPoint
        if (transform.parent != null && transform.parent.name == "ItemPoint")
        {
            transform.SetParent(null);
        }
        
        gameObject.SetActive(true);
    }
    
    private void OnDestroy()
    {
        // Dừng jump loop khi object bị destroy
        StopJumpLoop();
    }
}


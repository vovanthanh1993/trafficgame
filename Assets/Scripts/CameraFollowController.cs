using UnityEngine;

/// <summary>
/// Camera Controller đơn giản để follow player
/// Dựa trên Cinemachine Follow settings: Binding Mode World Space, Position Damping (1,1,1), Follow Offset (0,4,-4)
/// </summary>
public class CameraFollowController : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Player Transform để follow (nếu null sẽ tự động tìm bằng tag 'Player')")]
    public Transform target;

    [Tooltip("Binding Mode: World Space (true) hoặc Local Space (false)")]
    public bool useWorldSpace = true;

    [Tooltip("Follow ngay lập tức, không có độ trễ (smooth damping)")]
    public bool instantFollow = true;

    [Header("Position Damping")]
    [Tooltip("Damping cho X, Y, Z (chỉ dùng khi instantFollow = false, giá trị càng lớn, camera di chuyển càng mượt)")]
    public Vector3 positionDamping = new Vector3(1f, 1f, 1f);

    [Header("Follow Offset")]
    [Tooltip("Offset từ player (X=0, Y=4, Z=-4)")]
    public Vector3 followOffset = new Vector3(0f, 4f, -4f);

    [Header("X Distance Lock")]
    [Tooltip("Tự động tính và giữ khoảng cách x ban đầu giữa camera và player")]
    public bool lockInitialXDistance = true;

    private Vector3 velocity;
    private float initialXDistance; // Khoảng cách x ban đầu giữa camera và player
    private bool hasCalculatedInitialDistance = false;

    private void Start()
    {
        // Tự động tìm player nếu chưa gán
        FindAndSetPlayer();
        
        // Tính khoảng cách x ban đầu nếu có target
        CalculateInitialXDistance();
    }

    private void LateUpdate()
    {
        // Nếu chưa có target, tiếp tục tìm player (phòng trường hợp player spawn sau)
        if (target == null)
        {
            FindAndSetPlayer();
            return; // Chờ frame tiếp theo nếu vẫn chưa tìm thấy
        }

        // Tính khoảng cách x ban đầu nếu chưa tính (phòng trường hợp target được set sau Start)
        if (lockInitialXDistance && !hasCalculatedInitialDistance)
        {
            CalculateInitialXDistance();
        }

        // Tính toán vị trí mong muốn chỉ cho trục X
        Vector3 desiredPosition;
        
        if (useWorldSpace)
        {
            // World Space: offset được áp dụng trực tiếp trong world space
            desiredPosition = target.position + followOffset;
        }
        else
        {
            // Local Space: offset được xoay theo rotation của target
            desiredPosition = target.position + target.rotation * followOffset;
        }

        // Nếu lock khoảng cách x ban đầu, sử dụng khoảng cách đó thay vì offset
        if (lockInitialXDistance && hasCalculatedInitialDistance)
        {
            desiredPosition.x = target.position.x + initialXDistance;
        }

        // Lưu vị trí hiện tại của camera
        Vector3 currentPosition = transform.position;

        // Follow ngay lập tức hoặc smooth với damping
        if (instantFollow)
        {
            // Chỉ follow theo trục X, giữ nguyên Y và Z
            transform.position = new Vector3(desiredPosition.x, currentPosition.y, currentPosition.z);
        }
        else
        {
            // Smooth follow với damping chỉ cho trục X
            float smoothX = positionDamping.x > 0 ? 1f / positionDamping.x : 0.1f;

            // Smooth chỉ cho trục X, giữ nguyên Y và Z
            float newX = Mathf.SmoothDamp(currentPosition.x, desiredPosition.x, ref velocity.x, smoothX);

            transform.position = new Vector3(newX, currentPosition.y, currentPosition.z);
        }
    }

    /// <summary>
    /// Set target để follow
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            // Reset velocity khi đổi target
            velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Tự động tìm và set target bằng tag "Player"
    /// </summary>
    public void FindAndSetPlayer()
    {
        if (target != null) return; // Đã có target rồi, không cần tìm lại

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            SetTarget(player.transform);
            Debug.Log($"CameraFollowController: Đã tự động tìm Player '{player.name}'");
        }
    }

    /// <summary>
    /// Tính khoảng cách x ban đầu giữa camera và player
    /// </summary>
    private void CalculateInitialXDistance()
    {
        if (target != null && lockInitialXDistance)
        {
            initialXDistance = transform.position.x - target.position.x;
            hasCalculatedInitialDistance = true;
            Debug.Log($"CameraFollowController: Đã tính khoảng cách x ban đầu = {initialXDistance}");
        }
    }
}


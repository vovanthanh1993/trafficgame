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

    private Vector3 velocity;

    private void Start()
    {
        // Tự động tìm player nếu chưa gán
        FindAndSetPlayer();
    }

    private void LateUpdate()
    {
        // Nếu chưa có target, tiếp tục tìm player (phòng trường hợp player spawn sau)
        if (target == null)
        {
            FindAndSetPlayer();
            return; // Chờ frame tiếp theo nếu vẫn chưa tìm thấy
        }

        // Tính toán vị trí mong muốn
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

        // Follow ngay lập tức hoặc smooth với damping
        if (instantFollow)
        {
            // Follow ngay lập tức, không có độ trễ
            transform.position = desiredPosition;
        }
        else
        {
            // Smooth follow với damping
            Vector3 currentPosition = transform.position;
            
            // Sử dụng Vector3.SmoothDamp với damping riêng cho từng trục
            float smoothX = positionDamping.x > 0 ? 1f / positionDamping.x : 0.1f;
            float smoothY = positionDamping.y > 0 ? 1f / positionDamping.y : 0.1f;
            float smoothZ = positionDamping.z > 0 ? 1f / positionDamping.z : 0.1f;

            // Smooth từng trục riêng biệt
            float newX = Mathf.SmoothDamp(currentPosition.x, desiredPosition.x, ref velocity.x, smoothX);
            float newY = Mathf.SmoothDamp(currentPosition.y, desiredPosition.y, ref velocity.y, smoothY);
            float newZ = Mathf.SmoothDamp(currentPosition.z, desiredPosition.z, ref velocity.z, smoothZ);

            transform.position = new Vector3(newX, newY, newZ);
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
}


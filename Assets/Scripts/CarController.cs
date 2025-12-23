using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public Vector3 moveDirection = Vector3.forward; // Hướng di chuyển
    
    [Tooltip("Đi lùi (reverse) - nếu true, sẽ đảo ngược moveDirection")]
    public bool isReversing = false;
    
    [Header("Wheel Settings")]
    [Tooltip("Tag hoặc tên để nhận diện bánh xe (để trống sẽ lấy tất cả các con)")]
    public string wheelTag = ""; // Tag của bánh xe (để trống sẽ lấy tất cả children)
    
    [Tooltip("Bán kính bánh xe (để tính tốc độ quay chính xác, mặc định 0.3)")]
    public float wheelRadius = 0.3f;
    
    [Tooltip("Trục quay của bánh xe (X, Y, hoặc Z)")]
    public Vector3 wheelRotationAxis = Vector3.right; // Trục X để quay quanh trục ngang
    
    private Transform[] wheels; // Mảng các bánh xe (tự động lấy từ children)
    
    [Header("Limit Detection")]
    public string limitTag = "Limit"; // Tag của đối tượng limit
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Tự động lấy các bánh xe từ children
        InitializeWheels();
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
        // Di chuyển thẳng về phía trước
        MoveForward();
        
        // Xoay bánh xe
        RotateWheels();
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
    
    // Xoay bánh xe khi xe di chuyển
    private void RotateWheels()
    {
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
}

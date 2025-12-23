using UnityEngine;
using ControlFreak2;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    [Header("Joystick Settings")]
    [Tooltip("Touch Joystick để điều khiển (nếu null sẽ tự động tìm trong scene)")]
    [SerializeField] private TouchJoystick touchJoystick;
    
    private bool isInputEnabled = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Tự động tìm TouchJoystick nếu chưa được assign
        if (touchJoystick == null)
        {
            touchJoystick = FindObjectOfType<TouchJoystick>();
            if (touchJoystick != null)
            {
                Debug.Log("InputManager: Đã tìm thấy TouchJoystick tự động");
            }
        }
    }

    public void DisablePlayerInput()
    {
        isInputEnabled = false;
    }

    public void EnablePlayerInput()
    {
        isInputEnabled = true;
    }

    /// <summary>
    /// Lấy input vector từ TouchJoystick hoặc Keyboard
    /// </summary>
    public Vector2 InputMoveVector()
    {
        if (!isInputEnabled)
        {
            return Vector2.zero;
        }
        
        Vector2 inputVector = Vector2.zero;
        
        // Ưu tiên sử dụng TouchJoystick nếu có
        if (touchJoystick != null)
        {
            inputVector = touchJoystick.GetVector();
        }
        
        // Nếu không có joystick hoặc joystick không có input, fallback về keyboard
        if (inputVector.magnitude < 0.1f)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector2 keyboardInput = new Vector2(horizontal, vertical);
            
            // Chỉ dùng keyboard input nếu có giá trị
            if (keyboardInput.magnitude > 0.1f)
            {
                inputVector = keyboardInput;
            }
        }
        
        return inputVector;
    }
    
}
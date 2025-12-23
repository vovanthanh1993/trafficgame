using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;
    public string speedParameter = "Speed";
    public string dieTriggerParameter = "Die";
    public string hitTriggerParameter = "Hit";

    /// <summary>
    /// Được gọi từ PlayerController để cập nhật animation khi di chuyển
    /// </summary>
    /// <param name="isMoving">Nhân vật có đang di chuyển không</param>
    /// <param name="speed">Tốc độ di chuyển (0-1)</param>
    public void SetMovement(bool isMoving, float speed = 0f)
    {
        if (animator == null) return;
        
        if (!string.IsNullOrEmpty(speedParameter))
        {
            animator.SetFloat(speedParameter, speed);
        }
    }

    /// <summary>
    /// Được gọi từ PlayerHealth để trigger animation chết
    /// </summary>
    public void SetDie()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(dieTriggerParameter))
        {
            animator.SetTrigger(dieTriggerParameter);
        }
    }

    /// <summary>
    /// Được gọi từ PlayerController để trigger animation hit khi va chạm với xe
    /// </summary>
    public void SetHit()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(hitTriggerParameter))
        {
            animator.SetTrigger(hitTriggerParameter);
        }
    }
}

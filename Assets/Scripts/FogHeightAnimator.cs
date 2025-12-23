using System.Collections;
using UnityEngine;

/// <summary>
/// Script để animate Y position của FogHeight lặp lại: -2 lên -1, đợi 2s, xuống -2, đợi 5s
/// </summary>
public class FogHeightAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Vị trí Y thấp nhất")]
    [SerializeField] private float lowY = -2f;
    
    [Tooltip("Vị trí Y cao nhất")]
    [SerializeField] private float highY = -1f;
    
    [Tooltip("Thời gian di chuyển lên (giây)")]
    [SerializeField] private float moveUpDuration = 2f;
    
    [Tooltip("Thời gian di chuyển xuống (giây)")]
    [SerializeField] private float moveDownDuration = 2f;
    
    [Tooltip("Thời gian delay ở vị trí cao (y=-1) trước khi xuống (giây)")]
    [SerializeField] private float delayAtHighY = 10f;
    
    [Tooltip("Thời gian delay ở vị trí thấp (y=-2) trước khi lên (giây)")]
    [SerializeField] private float delayAtLowY = 1f;
    
    [Tooltip("Thời gian delay ban đầu trước khi bắt đầu animation (giây)")]
    [SerializeField] private float initialDelay = 5f;
    
    [Tooltip("Tự động bắt đầu animation khi Start()")]
    [SerializeField] private bool playOnStart = true;
    
    private Coroutine animationCoroutine;
    
    void Start()
    {
        // Đặt vị trí ban đầu
        Vector3 pos = transform.position;
        pos.y = lowY;
        transform.position = pos;
        
        // Bắt đầu animation nếu playOnStart = true
        if (playOnStart)
        {
            StartAnimationLoop();
        }
    }
    
    /// <summary>
    /// Bắt đầu animation loop
    /// </summary>
    public void StartAnimationLoop()
    {
        // Dừng animation cũ nếu đang chạy
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        // Bắt đầu animation loop mới
        animationCoroutine = StartCoroutine(AnimationLoop());
    }
    
    /// <summary>
    /// Dừng animation
    /// </summary>
    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine để lặp lại animation
    /// </summary>
    private IEnumerator AnimationLoop()
    {
        // Delay ban đầu 5 giây
        yield return new WaitForSeconds(initialDelay);
        
        // Lặp lại vô hạn
        while (true)
        {
            // Di chuyển từ -2 lên -1 trong 3 giây
            yield return StartCoroutine(MoveToY(highY, moveUpDuration));
            
            // Đợi 2 giây ở vị trí y=-1
            yield return new WaitForSeconds(delayAtHighY);
            
            // Di chuyển từ -1 xuống -2 trong 2 giây
            yield return StartCoroutine(MoveToY(lowY, moveDownDuration));
            
            // Đợi 5 giây ở vị trí y=-2
            yield return new WaitForSeconds(delayAtLowY);
        }
    }
    
    /// <summary>
    /// Coroutine để di chuyển đến vị trí Y cụ thể
    /// </summary>
    private IEnumerator MoveToY(float targetY, float duration)
    {
        float startY = transform.position.y;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // Sử dụng smooth interpolation (ease in-out)
            t = Mathf.SmoothStep(0f, 1f, t);
            
            // Interpolate Y position
            Vector3 currentPosition = transform.position;
            currentPosition.y = Mathf.Lerp(startY, targetY, t);
            transform.position = currentPosition;
            
            yield return null;
        }
        
        // Đảm bảo đạt đúng vị trí đích
        Vector3 finalPosition = transform.position;
        finalPosition.y = targetY;
        transform.position = finalPosition;
    }
    
    /// <summary>
    /// Reset về vị trí ban đầu
    /// </summary>
    public void ResetPosition()
    {
        StopAnimation();
        Vector3 pos = transform.position;
        pos.y = lowY;
        transform.position = pos;
    }
}


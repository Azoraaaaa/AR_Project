using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class BreathEffect : MonoBehaviour
{
    [Header("Alpha Settings")]
    [Range(0f, 1f)]
    public float minAlpha = 0.4f;

    [Range(0f, 1f)]
    public float maxAlpha = 1f;


    [Header("Scale Settings")]
    public float minScale = 0.95f;
    public float maxScale = 1.05f;


    [Header("Breath Speed")]
    public float duration = 2f;


    private CanvasGroup canvasGroup;
    private Vector3 originalScale;


    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }


    void Update()
    {
        // 0 - 1 - 0 循环
        float t = Mathf.PingPong(Time.time / (duration * 0.5f), 1f);

        // 更自然的缓动
        t = Mathf.SmoothStep(0f, 1f, t);


        // Alpha 呼吸
        canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);


        // Scale 呼吸
        float scale = Mathf.Lerp(minScale, maxScale, t);

        transform.localScale = originalScale * scale;
    }
}
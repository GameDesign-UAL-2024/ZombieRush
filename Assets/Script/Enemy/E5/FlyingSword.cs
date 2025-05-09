using System.Collections;
using UnityEngine;

public class FlyingSword : MonoBehaviour
{
    [Header("Homing Settings")]
    public float speed = 10f;                 // 移动速度
    public float turnSpeed = 40f;             // 最大转向速度（度/秒）
    public float delayBeforeHoming = 1f;      // 延迟开启追踪的秒数
    public float lifetime = 5f;               // 存在总时长

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;         // 淡入／淡出时长

    [SerializeField] AudioClip hit;
    private Transform target;
    private bool isHoming = false;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1, 1, 1, 0);

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            target = playerGO.transform;
        else
            Debug.LogWarning("FlyingSword: 找不到 Tag 为 Player 的物体！");

        StartCoroutine(FadeInRoutine());
        StartCoroutine(EnableHomingAfterDelay());
        StartCoroutine(FadeOutAndDestroyRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (spriteRenderer == null) yield break;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / fadeDuration);
            var c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;
            yield return null;
        }
        var col = spriteRenderer.color;
        col.a = 1f;
        spriteRenderer.color = col;
    }

    private IEnumerator EnableHomingAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeHoming);
        isHoming = true;
    }

    private IEnumerator FadeOutAndDestroyRoutine()
    {
        // 等待到剩 fadeDuration 秒时才开始淡出
        yield return new WaitForSeconds(Mathf.Max(0, lifetime - fadeDuration));

        // —— 停止追踪/移动 —— 
        isHoming = false;

        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                var c = spriteRenderer.color;
                c.a = a;
                spriteRenderer.color = c;
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    void Update()
    {
        if (!isHoming || target == null)
            return;

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        if (toTarget.sqrMagnitude < 0.001f)
            return;

        Vector2 desiredDir = toTarget.normalized;
        float currentAngle = transform.eulerAngles.z;
        float targetAngle  = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg - 90f;
        float maxDelta     = turnSpeed * Time.deltaTime;
        float newAngle     = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxDelta);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        transform.position += transform.up * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 命中后也停止移动
            isHoming = false;

            collision.GetComponent<PlayerController>()?.ReduceLife(2f);
            AudioSysManager.Instance.PlaySound(gameObject, hit, transform.position, 0.7f, true);

            StopAllCoroutines();
            StartCoroutine(HitFadeOutAndDestroy());
        }
    }

    private IEnumerator HitFadeOutAndDestroy()
    {
        if (spriteRenderer != null)
        {
            float startA = spriteRenderer.color.a;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(startA, 0f, elapsed / fadeDuration);
                var c = spriteRenderer.color;
                c.a = a;
                spriteRenderer.color = c;
                yield return null;
            }
        }
        Destroy(gameObject);
    }
}

using System.Collections;
using UnityEngine;

public class Loading : MonoBehaviour
{
    public float waitTime = 1f;           // 固定等待时间（秒）
    public float fadeDuration = 0.5f;     // 渐隐持续时间

    private CanvasGroup canvasGroup;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        Globals.Instance.Event.GameStart();
        StartCoroutine(FadeOutAfterDelay());
    }

    IEnumerator FadeOutAfterDelay()
    {
        // 固定等待
        yield return new WaitForSeconds(waitTime);

        // 开始淡出
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Globals.Instance.Event.current_state = Globals.Events.GameState.playing;
        // 销毁自己
        Destroy(gameObject);
    }
}

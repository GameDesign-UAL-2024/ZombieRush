using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class FinishingChair : MonoBehaviour
{
    [Tooltip("Fade to black duration in seconds")]
    public float fadeDuration = 1f;

    private bool isFading = false;

    // 缓存下来的 Canvas & Image
    private Canvas  fadeCanvas;
    private Image   fadeImage;

    void Awake()
    {
        // —— 1) 创建全屏 Canvas —— 
        GameObject canvasGO = new GameObject("RuntimeFadeCanvas");
        fadeCanvas = canvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 1000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // —— 2) 在 Canvas 下创建全屏黑 Image —— 
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 一开始隐藏
        canvasGO.SetActive(false);
    }

    void Update()
    {
        if (isFading) return;

        // 检查右键点击
        if (Input.GetMouseButtonDown(1))
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null && hit.gameObject == gameObject)
            {
                StartCoroutine(FadeAndLoad());
            }
        }
    }

    private IEnumerator FadeAndLoad()
    {
        isFading = true;
        fadeCanvas.gameObject.SetActive(true);

        float elapsed = 0f;
        // 从 alpha=0 插值到 1
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        fadeImage.color = Color.black;

        // 切场景
        SceneManager.LoadScene("MainMenu");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(CanvasGroup))]
public class MainMenuCanvas : MonoBehaviour
{
    [Header("Fade Settings")]
    public Camera mainCamera;            // 主摄像机
    public float fadeOutDistance = 15f;  // 超过这个距离，完全透明且不可交互
    public float fadeInDistance = 6f;    // 距离小于这个，完全可见且可交互

    private CanvasGroup canvasGroup;

    [Header("Localization")]
    public Locale chineseLocale;         // 在 Inspector 里拖入
    public Locale englishLocale;         // 在 Inspector 里拖入

    void Start()
    {
        Cursor.visible = true;

        // 获取或添加 CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        if (Cursor.visible != true)
        {
            Cursor.visible = true;
        }
        if (distance > fadeOutDistance)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (distance < fadeInDistance)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            float t = 1f - (distance - fadeInDistance) / (fadeOutDistance - fadeInDistance);
            canvasGroup.alpha = t;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 开始游戏（按钮调用）
    /// </summary>
    public void GameStart()
    {
        SceneManager.LoadScene("0_0");
    }

    /// <summary>
    /// 退出游戏（按钮调用）
    /// 编辑器下停止播放，打包后真正退出
    /// </summary>
    public void QuitGame()
    {
    #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    /// <summary>
    /// 切换语言（按钮调用）
    /// 首次或当前既非中英文时，默认切到中文
    /// </summary>
    public void ToggleLanguage()
    {
        var current = LocalizationSettings.SelectedLocale;

        // 首次或未配置到中/英时，直接切中文
        if (current != chineseLocale && current != englishLocale)
        {
            if (chineseLocale != null)
                LocalizationSettings.SelectedLocale = chineseLocale;
            return;
        }

        // 如果当前是中文，就切到英文；否则切到中文
        if (current == chineseLocale && englishLocale != null)
        {
            LocalizationSettings.SelectedLocale = englishLocale;
        }
        else if (current == englishLocale && chineseLocale != null)
        {
            LocalizationSettings.SelectedLocale = chineseLocale;
        }
    }
}

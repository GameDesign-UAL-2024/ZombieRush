using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuCanvas : MonoBehaviour
{
    public Camera mainCamera;            // 主摄像机
    public float fadeOutDistance = 15f;  // 超过这个距离，完全透明且不可交互
    public float fadeInDistance = 6f;    // 距离小于这个，完全可见且可交互

    private CanvasGroup canvasGroup;

    void Start()
    {
        // 获取或添加 CanvasGroup 组件
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);

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
            // 线性插值透明度（距离在 fadeInDistance 和 fadeOutDistance 之间时）
            float t = 1f - (distance - fadeInDistance) / (fadeOutDistance - fadeInDistance);
            canvasGroup.alpha = t;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void GameStart()
    {
        SceneManager.LoadScene("0_0");
    }
}
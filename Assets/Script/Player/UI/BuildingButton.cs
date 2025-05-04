using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 当前按钮对应的 id
    private int this_id;
    // 按钮相关属性
    private Dictionary<string, string> this_properties;
    // 按钮组件引用
    private Button button_comp;
    // 按钮图片引用
    public Image image;
    // 闪烁协程引用
    private Coroutine flashCoroutine;
    // PlayerUI 实例引用
    private PlayerUI UI_Instance;

    void Awake()
    {
        button_comp = GetComponent<Button>();
    }

    void Start()
    {
        UI_Instance = PlayerUI.Instance;
    }

    /// <summary>
    /// 初始化按钮并启动闪烁提示
    /// </summary>
    public void Initialize(string img_path, int id, Dictionary<string, string> properties, int Green_Need, int Black_Need, int Pink_Need, UnityAction<int> Listener)
    {
        this_id = id;
        this_properties = properties;

        // 点击时先停止闪烁，再执行放置逻辑
        button_comp.onClick.AddListener(() =>
        {
            StopFlash();
            Listener.Invoke(this_id);
        });

        SetImageSprite(img_path);

        // 确保 Image 初始 alpha 为 1
        if (image != null)
        {
            image.canvasRenderer.SetAlpha(1f);
        }

        StartFlash();  // 开始闪烁提示
    }

    private void SetImageSprite(string img_path)
    {
        Sprite newSprite = Resources.Load<Sprite>(img_path);
        if (newSprite != null)
        {
            image.sprite = newSprite;
        }
        else
        {
            image.color = Color.cyan;
            Debug.LogWarning($"无法加载路径为 '{img_path}' 的图片。请确保图片位于 Resources 文件夹，且路径正确。");
        }
    }

    /// <summary>
    /// 启动闪烁协程
    /// </summary>
    public void StartFlash()
    {
        if (flashCoroutine == null && image != null)
            flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    /// <summary>
    /// 停止闪烁，并恢复不透明状态
    /// </summary>
    public void StopFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        if (image != null)
        {
            image.CrossFadeAlpha(1f, 0f, true);
        }
    }

    /// <summary>
    /// 简化的闪烁效果：在 50%~100% alpha 之间交替
    /// </summary>
    private IEnumerator FlashCoroutine()
    {
        while (true)
        {
            // 渐隐到 50% alpha
            image.CrossFadeAlpha(0.5f, 0.5f, true);
            yield return new WaitForSecondsRealtime(0.5f);

            // 渐显到 100% alpha
            image.CrossFadeAlpha(1f, 0.5f, true);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UI_Instance?.ShowDescription(this_id, 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UI_Instance?.ClearDescription();
    }
}

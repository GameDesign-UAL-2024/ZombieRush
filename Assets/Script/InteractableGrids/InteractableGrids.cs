using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class InteractableGrids : MonoBehaviour
{
    GameObject player;
    public enum GridType { Trees, Bushes, Rocks };
    public float interacting_time;
    public GridType this_type;
    float start_time;
    Animator animator;
    GameObject bar_object;
    static string bar_prefab = "Prefabs/Bar";
    static string green_path = "Prefabs/GreenBlock";
    static string black_path = "Prefabs/BlackBlock";
    public int release_number;
    [SerializeField]
    int released = 0;

    bool is_interacting;
    bool on_sprite;
    bool in_area;
    bool interactionCompleted = false;

    private AsyncOperationHandle<GameObject>? barLoadHandle; // 存储加载句柄

    // 用于鼠标像素检测
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public float alphaThreshold = 0.1f;

    void Start()
    {
        is_interacting = false;
        on_sprite = false;
        player = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();

        // 获取 SpriteRenderer 并保存原始颜色
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        IsInArea();
        OnPlayerClick();

        if (animator != null)
        {
            animator.SetBool("Interacting", is_interacting);
        }

        if (bar_object != null && interacting_time != 0)
        {
            float progress = (GlobalTimer.Instance.GetCurrentTime() - start_time) / interacting_time;
            if (!in_area || !is_interacting)
            {
                Destroy(bar_object);
                bar_object = null;
            }
            else
            {
                bar_object.transform.GetComponent<Bar>().SetValue(progress);
            }

            if (progress >= 1 && !interactionCompleted)
            {
                interactionCompleted = true;  // 标记交互完成
                Destroy(bar_object);
                bar_object = null;

                // 生成资源
                if (this_type == GridType.Trees)
                {
                    for (int i = 0; i < release_number; i++)
                    {
                        Addressables.LoadAssetAsync<GameObject>(green_path).Completed += OnResourcesLoaded;
                    }
                }
                else if (this_type == GridType.Rocks)
                {
                    for (int i = 0; i < release_number; i++)
                    {
                        Addressables.LoadAssetAsync<GameObject>(black_path).Completed += OnResourcesLoaded;
                    }
                }

                // 延迟一段时间再移除当前对象，给生成的效果留出展示时间
                StartCoroutine(RemoveObjectWithDelay(0.1f));
            }
        }

        // —— 以下部分用于改变自身亮度，当鼠标悬停在 Sprite 非透明区域内时降低亮度 50% ——
        if (spriteRenderer != null)
        {
            if (IsMouseOverSpritePixel() && in_area)
            {
                spriteRenderer.color = new Color(originalColor.r * 0.80f,
                                                   originalColor.g * 0.80f,
                                                   originalColor.b * 0.80f,
                                                   originalColor.a);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private IEnumerator RemoveObjectWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameObject objectSpawner = GameObject.FindGameObjectWithTag("ObjectSpawner");
        if (objectSpawner != null)
        {
            objectSpawner.GetComponent<ObjectSpawner>().RemoveObject(gameObject);
        }
    }

    private void OnResourcesLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (this == null || gameObject == null) return; // 确保物体未被销毁

        GameObject resourceInstance = Instantiate(handle.Result, transform.position, Quaternion.identity);
        Rigidbody2D rb = resourceInstance.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.AddForce(UnityEngine.Random.insideUnitCircle.normalized * 10f, ForceMode2D.Impulse);
            StartCoroutine(DelayedReleaseIncrement());
        }
    }

    private IEnumerator DelayedReleaseIncrement()
    {
        yield return new WaitForSeconds(0.02f);
        released += 1;
    }

    private void OnBarLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (this == null || gameObject == null) return; // 防止访问已销毁的对象

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject bar = handle.Result;
            if (bar != null)
            {
                bar_object = Instantiate(bar, transform);
            }
        }
    }

    void OnPlayerClick()
    {
        if (Input.GetMouseButtonDown(1) && on_sprite && in_area && !is_interacting)
        {
            is_interacting = true;
            start_time = GlobalTimer.Instance.GetCurrentTime();

            if (release_number > 0)
            {
                release_number = (int)UnityEngine.Random.Range(1, release_number);
            }

            if (barLoadHandle.HasValue && barLoadHandle.Value.IsValid())
            {
                Addressables.Release(barLoadHandle.Value);
            }

            barLoadHandle = Addressables.LoadAssetAsync<GameObject>(bar_prefab);
            barLoadHandle.Value.Completed += OnBarLoaded;
        }
    }


    void IsInArea()
    {
        if (player != null)
        {
            if (Vector2.Distance(player.transform.position, transform.position) > 3f)
            {
                in_area = false;
                is_interacting = false;
            }
            else
            {
                in_area = true;
            }
        }
    }

    // 判断鼠标是否在 Sprite 的非透明像素区域内（无需 Collider）
    bool IsMouseOverSpritePixel()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return false;

        // 获取鼠标在世界坐标中的位置
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = transform.position.z;

        // 转换为 Sprite 的局部坐标
        Vector2 localPos = transform.InverseTransformPoint(worldPos);

        Sprite sprite = spriteRenderer.sprite;
        Rect textureRect = sprite.textureRect;
        Vector2 pivot = sprite.pivot;
        float pixelsPerUnit = sprite.pixelsPerUnit;

        // 将局部坐标转换为以像素为单位的坐标（参照 Sprite 的 pivot）
        Vector2 pixelPos = new Vector2(localPos.x * pixelsPerUnit + pivot.x,
                                       localPos.y * pixelsPerUnit + pivot.y);

        // 检查是否在 Sprite 纹理矩形内
        if (pixelPos.x < 0 || pixelPos.x > textureRect.width ||
            pixelPos.y < 0 || pixelPos.y > textureRect.height)
        {
            return false;
        }

        int texX = Mathf.FloorToInt(pixelPos.x + textureRect.x);
        int texY = Mathf.FloorToInt(pixelPos.y + textureRect.y);

        Color pixelColor = sprite.texture.GetPixel(texX, texY);
        return on_sprite = pixelColor.a > alphaThreshold;
    }
}

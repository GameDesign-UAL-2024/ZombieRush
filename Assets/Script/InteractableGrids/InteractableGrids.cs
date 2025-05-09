using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    static string Notice_Path_B = "Prefabs/Notice_Resource_Black";
    static string Notice_Path_G = "Prefabs/Notice_Resource_Green";
    string tree_broke_sound = "Audios/TreeBroke";
    string rock_broke_sound = "Audios/RockBroke";
    AudioClip rock;
    AudioClip tree;
    GameObject NoticeObject_prefab;
    GameObject NoticeObject;
    public int release_number;
    [SerializeField]
    GameObject resource_prefab;
    bool is_interacting;
    bool on_sprite;
    bool in_area;
    bool interactionCompleted = false;
    private AsyncOperationHandle<GameObject>? barLoadHandle;
    Globals global;

    // 用于鼠标像素检测
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public float alphaThreshold = 0.1f;

    // 新增：用于对 NoticeObject 子节点做距离衰减
    private List<SpriteRenderer> noticeSpriteRenderers;

    // 阈值配置
    private float noticeThreshold = 10f;
    private float attenuationStart = 6f;
    private float attenuationEnd = 20f;
    private float curveExponent = 0.5f;

    void Start()
    {
        is_interacting = false;
        on_sprite = false;
        global = Globals.Instance;
        player = GameObject.FindGameObjectWithTag("Player");
        animator = GetComponent<Animator>();
        resource_prefab = Addressables.LoadAssetAsync<GameObject>(this_type == GridType.Rocks ? black_path : green_path).WaitForCompletion();
        rock = Addressables.LoadAssetAsync<AudioClip>(rock_broke_sound).WaitForCompletion();
        tree = Addressables.LoadAssetAsync<AudioClip>(tree_broke_sound).WaitForCompletion();
        // 预载提示预制
        if (this_type == GridType.Rocks)
            NoticeObject_prefab = Addressables.LoadAssetAsync<GameObject>(Notice_Path_B).WaitForCompletion();
        else
            NoticeObject_prefab = Addressables.LoadAssetAsync<GameObject>(Notice_Path_G).WaitForCompletion();

        // 获取自身SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Update()
    {
        IsInArea();
        OnPlayerClick();

        // 根据类型与资源量判断是否需要提示
        var data = global.Data;
        bool needNotice = false;
        switch (this_type)
        {
            case GridType.Rocks:
                needNotice = data.player_current_resources[Globals.Datas.ResourcesType.Black] <= noticeThreshold;
                break;
            case GridType.Trees:
            case GridType.Bushes:
                needNotice = data.player_current_resources[Globals.Datas.ResourcesType.Green] <= noticeThreshold;
                break;
        }

        // 只有状态变化时才创建或销毁
        if (needNotice && NoticeObject == null)
        {
            NoticeObject = Instantiate(NoticeObject_prefab, transform, worldPositionStays: false);
            noticeSpriteRenderers = new List<SpriteRenderer>(NoticeObject.GetComponentsInChildren<SpriteRenderer>());
        }
        else if (!needNotice && NoticeObject != null)
        {
            Destroy(NoticeObject);
            NoticeObject = null;
            noticeSpriteRenderers = null;
        }

        // 动画状态
        if (animator != null)
            animator.SetBool("Interacting", is_interacting);

        // 交互进度条逻辑
        if (bar_object != null && interacting_time > 0f)
        {
            float progress = (GlobalTimer.Instance.GetCurrentTime() - start_time) / interacting_time;
            if (!in_area || !is_interacting)
            {
                Destroy(bar_object);
                bar_object = null;
            }
            else
            {
                bar_object.GetComponent<Bar>().SetValue(progress);
            }

            if (progress >= 1f && !interactionCompleted)
            {
                interactionCompleted = true;
                Destroy(bar_object);
                bar_object = null;
                AudioSysManager.Instance.PlaySound(player,this_type == GridType.Rocks? rock:tree,transform.position,1,false);
                if (resource_prefab == null)
                    return;
                for (int i = 0; i < release_number; i++)
                {
                    var instance = Instantiate(resource_prefab, transform.position, Quaternion.identity);
                    var rb = instance.GetComponent<Rigidbody2D>();
                    if (rb != null)
                        rb.AddForce(UnityEngine.Random.insideUnitCircle.normalized * 10f, ForceMode2D.Impulse);
                }
                var spawner = GameObject
                    .FindGameObjectWithTag("ObjectSpawner")
                    ?.GetComponent<ObjectSpawner>();
                if (spawner != null)
                    spawner.RemoveObject(this.gameObject);
                else
                    Destroy(gameObject);  // 兜底，确保不会遗留
            }
        }

        // 鼠标悬停亮度变化
        if (spriteRenderer != null)
        {
            if (IsMouseOverSpritePixel() && in_area)
                spriteRenderer.color = originalColor * 0.8f;
            else
                spriteRenderer.color = originalColor;
        }
    }

    void LateUpdate()
    {
        if (noticeSpriteRenderers == null || player == null)
            return;

        // 清理已销毁引用
        noticeSpriteRenderers.RemoveAll(sr => sr == null);
        if (noticeSpriteRenderers.Count == 0)
        {
            noticeSpriteRenderers = null;
            return;
        }

        // 计算距离衰减
        float dist = Vector3.Distance(player.transform.position, transform.position);
        float raw = Mathf.Clamp01((dist - attenuationStart) / (attenuationEnd - attenuationStart));
        float attenuation = 1f - Mathf.Pow(raw, curveExponent);

        // 应用于提示子元素
        foreach (var sr in noticeSpriteRenderers)
        {
            Color c = sr.color;
            c.a = c.a * attenuation;
            sr.color = c;
        }
    }

    private IEnumerator RemoveObjectWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        var spawner = GameObject.FindGameObjectWithTag("ObjectSpawner");
        spawner?.GetComponent<ObjectSpawner>()?.RemoveObject(gameObject);
    }


    private void OnBarLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (gameObject == null) return;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            bar_object = Instantiate(handle.Result, transform);
        }
    }

    void OnPlayerClick()
    {
        if (Input.GetMouseButtonDown(1) && on_sprite && in_area && !is_interacting)
        {
            is_interacting = true;
            start_time = GlobalTimer.Instance.GetCurrentTime();
            release_number = UnityEngine.Random.Range(1, release_number + 1);

            if (barLoadHandle.HasValue && barLoadHandle.Value.IsValid())
                Addressables.Release(barLoadHandle.Value);

            barLoadHandle = Addressables.LoadAssetAsync<GameObject>(bar_prefab);
            barLoadHandle.Value.Completed += OnBarLoaded;
        }
    }

    void OnDestroy()
    {
        if (NoticeObject != null)
            Destroy(NoticeObject);
    }

    void IsInArea()
    {
        if (player != null)
        {
            float d = Vector2.Distance(player.transform.position, transform.position);
            in_area = d <= 3f;
            if (!in_area) is_interacting = false;
        }
    }

    bool IsMouseOverSpritePixel()
    {
        if (spriteRenderer?.sprite == null) return false;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = transform.position.z;
        Vector2 localPos = transform.InverseTransformPoint(worldPos);

        var sprite = spriteRenderer.sprite;
        var texRect = sprite.textureRect;
        var pivot = sprite.pivot;
        float ppu = sprite.pixelsPerUnit;

        Vector2 pixelPos = new Vector2(
            localPos.x * ppu + pivot.x,
            localPos.y * ppu + pivot.y
        );

        if (pixelPos.x < 0 || pixelPos.x > texRect.width || pixelPos.y < 0 || pixelPos.y > texRect.height)
            return false;

        int x = Mathf.FloorToInt(pixelPos.x + texRect.x);
        int y = Mathf.FloorToInt(pixelPos.y + texRect.y);
        Color col = sprite.texture.GetPixel(x, y);
        return on_sprite = col.a > alphaThreshold;
    }
}

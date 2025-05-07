using UnityEngine;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ResourcePickup : MonoBehaviour
{
    [Header("资源设置")]
    [Tooltip("在 Inspector 里选择要拾取的资源类型")]
    public Globals.Datas.ResourcesType resourceType;

    [Tooltip("拾取时增加的数量")]
    public int amount = 5;

    [Header("物理设置")]
    [Tooltip("如果有初速度，可在外部 Rigidbody2D 上设置；这里用指数衰减模拟阻力")]
    public float dragFactor = 0.9f;
    string collect_sound = "Audios/Player/CollectSound";
    AudioClip collect_sound_clip;
    Rigidbody2D rb;

    void Awake()
    {
        collect_sound_clip = Addressables.LoadAssetAsync<AudioClip>(collect_sound).WaitForCompletion();
        rb = GetComponent<Rigidbody2D>();
        // 确保是 Trigger，否则需要在 Collider2D 里勾选 IsTrigger
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Globals.Instance.Data.AddResourceAmount(resourceType, amount);
            if (AudioSysManager.Instance != null)
            {
                AudioSysManager.Instance.PlaySound(gameObject,collect_sound_clip,transform.position,1f,false);
            }
            Destroy(gameObject,0.1f);
        }
    }

    void FixedUpdate()
    {
        if (rb != null && rb.velocity.sqrMagnitude > 0f)
        {
            rb.velocity *= dragFactor;
        }
    }
}

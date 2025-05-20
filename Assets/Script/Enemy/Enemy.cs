using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
public abstract class Enemy : MonoBehaviour
{
    public abstract float max_health{get; set;}
    public abstract float current_health{get; set;}
    public abstract float speed{get; set;}
    public abstract GameObject target{get; set;}
    public abstract void SetTarget(GameObject tar);
    public abstract bool TakeDamage(Vector3 source  , float amount , bool Instant_kill);
    public const string hitted_prefab_path = "Prefabs/Hitted";
    void OnDestroy()
    {
        GlobalEventBus.OnEnemyDead.Invoke();
    }
    /// <summary>
    /// 播放攻击音效，需要传入来源物体和位置
    /// </summary>
    public static void PlayAttackSound(GameObject source, Vector3 position)
    {
        const string sound_path = "Audios/E_Attack";
        AudioClip sound = Addressables
            .LoadAssetAsync<AudioClip>(sound_path)
            .WaitForCompletion();

        if (AudioSysManager.Instance != null)
        {
            AudioSysManager.Instance.PlaySound(
                source,    // 以前想用 this.gameObject
                sound,
                position,  // 以前想用 transform.position
                1f,
                true
            );
        }
    }
}



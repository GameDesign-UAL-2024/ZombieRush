using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HumanArms : MonoBehaviour
{
    Animator animator;
    [SerializeField]ArmAttackRange L;
    [SerializeField]ArmAttackRange R;
    [SerializeField] AudioClip attack_sound;
    [SerializeField] AudioClip hit_Sound;
    UnityAction<Enemy> OnHitEnemiesFunc;
    PlayerController player;
    void Awake()
    {
        animator = transform.GetComponent<Animator>();
        OnHitEnemiesFunc = OnHitEnemies;
        if (L != null && R != null)
        {
            L.AddListenerForThis(OnHitEnemiesFunc);
            R.AddListenerForThis(OnHitEnemiesFunc);
        }
    }
    public void PlayAttackSound()
    {
        AudioSysManager.Instance.PlaySound(player.gameObject,attack_sound,player.transform.position,0.8f,false);
    }
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }
    public void SetParameter(string parameter,bool state)
    {
        if (animator.GetBool(parameter) != state)
        {
            animator.SetBool(parameter , state);
        }
    }
    public void shake_effect()
    {
        if (CameraEffects.Instance != null)
        {
            CameraEffects.Instance.Shake(0.05f , 1f , 0.8f , 0.2f,0.5f);
        }
    }
    void OnHitEnemies(Enemy enemy)
    {
        if (player != null)
        {
            // 1. 先造成伤害
            AudioSysManager.Instance.PlaySound(enemy.gameObject,hit_Sound,enemy.transform.position,1,true);
            enemy.TakeDamage(
                player.transform.position,
                player.player_properties.damage * 1.85f,
                false
            );
            shake_effect();
            // 2. 获取敌人的 Rigidbody2D
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                // 3. 计算从自己指向敌人的方向（只取 X/Y）
                Vector2 dir = enemy.transform.position - transform.position;
                dir.Normalize();

                // 4. 应用冲量
                float knockbackForce = 10f; 
                enemyRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DaYunAttack : MonoBehaviour
{
    public float knockbackForce = 10f;
    [SerializeField] AudioClip StartSound;
    [SerializeField] AudioClip Hit;
    [SerializeField] AudioClip BGM;
    void Start()
    {
        AudioSysManager.Instance.PlaySound(gameObject,StartSound,transform.position,0.15f,true);
        AudioSysManager.Instance.PlayBGM(BGM,10f);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            collision.GetComponent<Enemy>().TakeDamage(transform.position,
                                                        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().player_properties.damage*3,
                                                        true);            
            // 2) 再击飞
            AudioSysManager.Instance.PlaySound(gameObject,Hit,collision.transform.position,0.25f,false);
            Rigidbody2D rb = collision.GetComponent<Enemy>().GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 从 this → enemy 的方向
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                
                // 一次性冲量式击飞
                CameraEffects.Instance.Shake(0.3f,1f,3,0.9f);
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
    void OnDestroy()
    {
        AudioSysManager.Instance.StopOverrideBGM();
    }
}

using System;
using UnityEngine;


public class BladeStormBullet : MonoBehaviour
{
    Vector2 target_direction;
    [SerializeField] AudioClip hit_sound;
    bool initialized;
    public event Action<BladeStormBullet> OnDestroyed;
    PlayerController player;
    void Awake()
    {
        initialized = false;
    }
    void Start()
    {
        player=GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }
    void FixedUpdate()
    {
        if (!initialized) return;
        transform.position += (Vector3)target_direction * 8f * Time.deltaTime;
                transform.localRotation *= Quaternion.Euler(0f, 0f, 1080 * Time.deltaTime);
    }

    public void Initialize(Vector2 target)
    {
        initialized = true;
        target_direction = target.normalized;
        // 自动销毁
        Destroy(gameObject, 3f);
    }

    void OnDestroy()
    {
        OnDestroyed?.Invoke(this);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            AudioSysManager.Instance.PlaySound(gameObject , hit_sound , transform.position , 1);
            var enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(transform.position, player.player_properties.damage * 2, false);
            }
        }
    }
}

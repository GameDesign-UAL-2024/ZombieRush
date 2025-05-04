using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(EnemyNav))]
public class Enemy1 : Enemy
{
    public enum EnemyState { Wait, Moving, Attack }
    private EnemyState _currentState;
    public EnemyState current_state { get => _currentState; private set => _currentState = value; }

    public override float max_health { get; set; } = 5f;
    public override float current_health { get; set; }
    public override float speed { get; set; } = 4f;
    public override GameObject target { get; set; }

    [Header("Attack Settings")]
    [SerializeField] float dashDuration = 2f;
    [SerializeField] float dashMultiplier = 0.8f;

    [Header("Rotation Settings")]
    [SerializeField] float rotationSpeed = 1f;

    [SerializeField] ParticleSystem attacking_effect;

    private EnemyNav navigation;
    private GlobalTimer g_timer;
    private GameObject player;
    private Animator animator;
    private Rigidbody2D RB;
    private SpriteRenderer sprite_renderer;
    private Vector3 originalScale;
    private float behaviour_time;
    private readonly float behaviour_gap = 3f;
    private bool could_hurt = true;
    private bool dying = false;

    void Start()
    {
        DeactiveAttackEffect();
        current_health = max_health;
        navigation = GetComponent<EnemyNav>();
        RB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite_renderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        g_timer = GlobalTimer.Instance;
        behaviour_time = g_timer.GetCurrentTime();
        current_state = EnemyState.Wait;

        player = GameObject.FindGameObjectWithTag("Player");
        target = player;
    }


    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, 0f, 199f);
        pos.y = Mathf.Clamp(pos.y, 0f, 199f);
        transform.position = pos;
    }


    void Update()
    {
        if (dying) return;

        float dist = Vector2.Distance(transform.position, player.transform.position);
        float now = g_timer.GetCurrentTime();

        if (current_state != EnemyState.Attack)
        {
            float sign = player.transform.position.x < transform.position.x ? -1f : 1f;
            transform.localScale = new Vector3(originalScale.x * sign, originalScale.y, originalScale.z);

            var shape = attacking_effect.shape;
            shape.rotation = new Vector3(0f, 0f, sign > 0 ? 90f : -90f);

            if (now - behaviour_time >= behaviour_gap && current_state == EnemyState.Wait)
                SetState(EnemyState.Moving);

            if (dist <= 10f && now - behaviour_time > behaviour_gap)
                SetState(EnemyState.Attack);
        }

        if (current_health <= 0 && !dying)
        {
            dying = true;
            could_hurt = false;
            SetState(EnemyState.Wait);
            animator.SetBool("Dead", true);
            navigation.SetNavActive(false);
        }
    }

    void FixedUpdate()
    {
        if (current_state != EnemyState.Attack)
            RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.fixedDeltaTime);
        if (current_state != EnemyState.Attack)
            RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.fixedDeltaTime);
    }

    public override bool TakeDamage(Vector3 source, float amount, bool Instant_kill)
    {
        if (!could_hurt) return false;
        current_health -= amount;
        animator.SetTrigger("Hurt");
        Vector2 backDir = -(source - transform.position).normalized;
        RB.velocity = backDir * 2f;
        navigation.SetNavActive(false);
        return true;
    }

    void SetState(EnemyState newState)
    {
        if (newState == current_state) return;

        animator.ResetTrigger("Attacking");
        animator.SetBool("Moving", false);
        navigation.SetNavActive(false);

        current_state = newState;
        switch (newState)
        {
            case EnemyState.Wait:
                behaviour_time = g_timer.GetCurrentTime();
                break;
            case EnemyState.Moving:
                animator.SetBool("Moving", true);
                navigation.SetTarget(player);
                navigation.SetNavActive(true);
                break;
            case EnemyState.Attack:
                animator.SetTrigger("Attacking");
                break;
        }
    }
    public void StartAttackDash()
    {
        StartCoroutine(AttackDash());
    }
    IEnumerator AttackDash()
    {
        navigation.SetNavActive(false);
        ActiveAttackEffect();

        float start = g_timer.GetCurrentTime();

        while (g_timer.GetCurrentTime() - start <= dashDuration)
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (transform.localScale.x < 0) angle += 180f;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * 0.25f * Time.deltaTime);

            transform.Translate((transform.localScale.x > 0 ? transform.right : -transform.right) * speed * dashMultiplier * Time.deltaTime, Space.World);
            yield return null;
        }

        DeactiveAttackEffect();
        transform.rotation = Quaternion.identity;
        SetState(EnemyState.Wait);
    }

    public override void SetTarget(GameObject tar) => target = tar;
    public void ActiveAttackEffect() => attacking_effect.Play();
    public void DeactiveAttackEffect() => attacking_effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    public void DestroyEnemy()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
            Globals.Datas.EnemyPool.Remove(this);
        Destroy(gameObject);
    }
}

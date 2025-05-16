using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Enemy8 : Enemy
{
    public override float max_health { get; set; } = 512;
    public override float speed { get; set; } = 4f;
    public override float current_health { get; set; }
    public override GameObject target { get; set; }
    float behaviour_time = 1f;
    float behaviour_cool = 0f;
    float hurt_exist;
    Animator animator;
    string water_drop_step = "Prefabs/WaterDrops";
    string E8B = "Prefabs/E8B";
    GameObject E8B_pref;
    GameObject water_drop_step_prefab;
    GameObject player;
    [SerializeField] AudioClip watermove;
    Coroutine current_move;
    Rigidbody2D RB;
    private readonly string[] boolParams = {
        "Behaviour",
        "Hit",
        "Jump_Attack",
        "Healing",
        "Attack_Shoot",
        "Moving"
    };
    public void ResetAllBoolParameters()
    {
        ChooseTarget();
        foreach (var param in boolParams)
        {
            animator.SetBool(param, false);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        hurt_exist = 0;
        current_health = max_health;
        animator = GetComponent<Animator>();
        RB = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        water_drop_step_prefab = Addressables.LoadAssetAsync<GameObject>(water_drop_step).WaitForCompletion();
        E8B_pref = Addressables.LoadAssetAsync<GameObject>(E8B).WaitForCompletion();
    }

    void Update()
    {
        if (Globals.Instance.Event.current_state != Globals.Events.GameState.playing)
        {
            return;
        }
        behaviour_cool += Time.deltaTime;
        if (behaviour_cool >= behaviour_time)
        {
            animator.SetBool("Behaviour",true);
        }   
        if (RB.velocity.magnitude > 0)
        {
            RB.velocity = Vector2.Lerp(RB.velocity,Vector2.zero,0.5f);
        }
    }
    void FixedUpdate()
    {
        if (animator.GetBool("Hurt"))
        {
            hurt_exist += Time.deltaTime;
            if (hurt_exist > 1f)
            {
                animator.SetBool("Hurt",false);
                hurt_exist = 0f;
            }
        }
        else
        {
            hurt_exist = 0f;
        }
    }
    public override void SetTarget(GameObject tar)
    {
        target = tar;
    }
    public void Healing()
    {
        
        current_health += 5;

        current_health = Math.Clamp(current_health,0,max_health);
    }
    public void Moving()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (current_move != null){StopCoroutine(current_move);}
        current_move = StartCoroutine(MoveCoruntine(player.transform.position,false));
    }
    public void JumpAttack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (current_move != null){StopCoroutine(current_move);}
        current_move = StartCoroutine(MoveCoruntine(player.transform.position,true));
    }
    public void ShootAttack()
    {
        StartCoroutine(ShootCoruntine());
    }
    private IEnumerator ShootCoruntine()
    {
        for (int i = 0; i <= 2 ; i++)
        {
            for (int n = 0; n <= 3 ; n++)
            {
                FireBullet();
                yield return new WaitForSeconds(0.05f);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
    private IEnumerator MoveCoruntine(Vector2 target_pos , bool is_attack)
    {
        while (Vector2.Distance(transform.position, target_pos) > 0.1f)
        {
            float speed = is_attack? 15f : 7f;
            float step  = speed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(
                transform.position,
                target_pos,
                step
            );
            yield return null;
        }
    }
    public void MakeDecision()
    {
        ResetAllBoolParameters();
        string[] states = {"Jump_Attack","Healing","Attack_Shoot","Moving"};
        int idx = UnityEngine.Random.Range(0, states.Length);
        // 打开选中的状态
        animator.SetBool(states[idx], true);
        behaviour_cool = 0f;
        animator.SetBool("Behaviour",false);
    }
    public void FireBullet()
    {
        GameObject go = Instantiate(E8B_pref, transform.position, Quaternion.identity);
        var bullet = go.GetComponent<E8_Bullet>();
        if (target != null)
        {
            Vector2 dir = (target.transform.position - transform.position).normalized;
            bullet.Initialize(this, dir, 0f, 8f, 10f, 0.5f);
        }
        
    }
    public override bool TakeDamage(Vector3 source, float amount, bool instantKill)
    {
        current_health -= amount;
         animator.SetBool("Hurt", true);
        if (current_health <= 0f)
        {
            animator.SetBool("Dead", true);
            animator.Play("Base Layer.Dead", 0, 0f); // 或者 SetBool("Dead", true)
        }
        return true;
    }
    private void ChooseTarget()
    {
        float playerDist = Vector2.Distance(transform.position, player.transform.position);
        float bestBuildDist = float.MaxValue;
        GameObject bestBuild = null;

        foreach (var kv in PlayerBuildingManager.current_buildings)
        {
            var bComp = kv.Value;
            if (bComp == null) continue;
            float d = Vector2.Distance(transform.position, bComp.transform.position);
            if (d < bestBuildDist)
            {
                bestBuildDist = d;
                bestBuild = bComp.gameObject;
            }
        }

        target = (bestBuild != null && bestBuildDist < playerDist)
            ? bestBuild 
            : player;
    }
    void PlayMovingSound()
    {
        if (AudioSysManager.Instance == null) return;


        // 2) 检测脚下是否是水
        if (ChunkGenerator.Instance != null && ChunkGenerator.Instance.IsTileOfType(transform.position, ChunkGenerator.Instance.waterTile))
        {
            // 4) 播放水上移动音效
            Instantiate(water_drop_step_prefab,transform.position,Quaternion.identity,transform);
            AudioSysManager.Instance.PlaySound(gameObject, watermove,transform.position,0.8f,false);
        }
    }
    public void AttackSound()
    {
        PlayAttackSound(gameObject, transform.position);
    }
    // if you want to hook actual removal from pool on death animation event:
    public void OnDeath()
    {
        if (Globals.Datas.EnemyPool.Contains(this))
        {
            Globals.Datas.EnemyPool.Remove(this);
            GlobalEventBus.OnEnemyDead.Invoke();
        }
        GameObject finishing = Addressables.LoadAssetAsync<GameObject>("Prefabs/FinishChair").WaitForCompletion();
        Instantiate(finishing , transform.position , Quaternion.identity);
        Destroy(gameObject);
    }
}

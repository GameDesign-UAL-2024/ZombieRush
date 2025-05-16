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
    Animator animator;
    string water_drop_step = "Prefabs/WaterDrops";
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
        foreach (var param in boolParams)
        {
            animator.SetBool(param, false);
        }
    }
    public
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public override void SetTarget(GameObject tar)
    {
        target = tar;
    }
    public void Healing()
    {
        current_health += 5;
    }
    public void Moving()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(MoveCoruntine(player.transform.position,false));
    }
    public void JumpAttack()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
    }
    private IEnumerator MoveCoruntine(Vector2 target_pos , bool is_attack)
    {
        while (Vector2.Distance(transform.position, target_pos) > 0.5f)
        {
            float speed = is_attack? 8f : 5f;
            float step  = speed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(
                transform.position,
                target_pos,
                step
            );
            yield return null;
        }
    }
    public override bool TakeDamage(Vector3 source, float amount, bool instantKill)
    {
        current_health -= amount;
        if (current_health <= 0f)
        {
            animator.SetBool("Dead", true);
            animator.Play("Base Layer.Dead", 0, 0f); // 或者 SetBool("Dead", true)
        }
        return true;
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

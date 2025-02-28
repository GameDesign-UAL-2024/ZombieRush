using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyNav))]
public class Enemy_01 : Enemy
{
    public override float max_health { get; set;} = 100f;
    public override float current_health { get; set;}
    public override float speed { get; set;} = 3.5f;
    bool could_hurt;
    public override GameObject target { get; set;}
    public override EnemyState current_state { get; set;}

    void Start()
    {
        current_health = max_health;
        current_state = EnemyState.Wait;
        could_hurt = false;
    }

    public override void SetTarget(GameObject tar)
    {
        target = tar;
    }
    public override bool TakeDamage(float amount, bool Instant_kill)
    {
        if (could_hurt){current_health -= amount; return true;}
        else {return false;}
    }
}

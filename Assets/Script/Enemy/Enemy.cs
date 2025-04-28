using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public abstract float max_health{get; set;}
    public abstract float current_health{get; set;}
    public abstract float speed{get; set;}
    public abstract GameObject target{get; set;}

    public enum EnemyState{ Moving , Wait , Attack }
    public abstract EnemyState current_state{get; set;}
    public abstract void SetTarget(GameObject tar);
    public abstract bool TakeDamage(Vector3 source  , float amount , bool Instant_kill);
    public const string hitted_prefab_path = "Prefabs/Hitted";
}



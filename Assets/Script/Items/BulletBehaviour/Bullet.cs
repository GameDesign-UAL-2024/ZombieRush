using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Bullet : Items
{
    public abstract void AddSpeed(float speed);
    public abstract void Initialize(Vector3 Source , Vector2 target , Rigidbody2D rb , float bullet_speed = 10, float damage = 1, float exist_time = 2);

    public override void OnShoot(){}
    public override void ActiveBulletEffects(){}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Bullet : Items
{
    public abstract void AddSpeed(float speed);
    public abstract void Initialize(Vector2 target , Rigidbody2D RB);

    public override void OnShoot(){}
    public override void ActiveBulletEffects(){}
}

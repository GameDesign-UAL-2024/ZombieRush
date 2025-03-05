using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BulletBehaviour_0 : Bullet
{ 
    public override int ID { get; set; } = 1;
    public override ItemTypes Type { get; set; } = ItemTypes.BulletBehaviour;
    Vector2 direction;
    float moveSpeed = 10;
    float damage;
    float acceleration = 2;
    float exist_time = 2;
    float timer;
    bool initialzed = false;
    Rigidbody2D RB;
    Vector3 source;
    void FixedUpdate()
    {
        if (initialzed && RB != null)
        {
            timer += Time.deltaTime;
            if (RB.velocity.magnitude < moveSpeed)
            {
                RB.velocity += acceleration * direction;
            }         
        }
        if (timer > exist_time)
        {
            Destroy(this.gameObject);
        }
    }
    public override void Initialize(Vector3 Source, Vector2 target , Rigidbody2D rb , float bullet_speed = 10, float bullet_damage = 1, float bullet_exist_time = 2)
    {
        RB = rb;
        source = Source;
        moveSpeed = bullet_speed;
        damage = bullet_damage;
        exist_time = bullet_exist_time;
        direction = target - new Vector2(rb.transform.position.x , rb.transform.position.y);
        direction = direction.normalized;
        initialzed = true;
    }
    public override void AddSpeed(float speed)
    {
        moveSpeed += speed;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Enemy")
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(source , damage , false);
            }
            Destroy(this.gameObject);
        }
    }
}

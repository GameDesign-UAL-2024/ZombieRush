using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class B1 : Buildings
{
    public override float current_health { get; set; }
    public override float max_health { get; set; } = 20f;
    public override BuildingType this_type { get; set;} = BuildingType.Attack;


    bool initialized;
    int max_attack_number = 3;
    float attack_range = 15f;
    List<B1_Bullet> targets;
    UnityEvent<Buildings> destroy_event;
    void Awake()
    {
        current_health = max_health;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void TakeDamage(float amount)
    {
        current_health -= amount;
    }

    void DestroyBullet(B1_Bullet bullet)
    {
        if (targets.Contains(bullet))
        {
            targets.Remove(bullet);
            Destroy(bullet.gameObject);
        }
    }

    public void BuildingDestroy()
    {
        destroy_event.Invoke(this);
    }
    public void Initialize(UnityAction<Buildings> on_building_destroy)
    {
        destroy_event.AddListener(on_building_destroy);
    }
}

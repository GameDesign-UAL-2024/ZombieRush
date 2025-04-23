using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Momentum : Items
{
    public override int ID {get; set;} = 8;
    public override ItemTypes Type {get; set;} = ItemTypes.Properties;
    public override ItemRanks Rank {get; set;} = ItemRanks.A;
    float rate = 0.2f;
    void Start()
    {
        PlayerController player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        if (player != null)
        {
            float bullet_speed = player.player_properties.bullet_speed;
            player.player_properties.damage += bullet_speed * rate;
            player.PlayPropertieUpAnimation();
            Destroy(this,0.1f);
        }
    }
}

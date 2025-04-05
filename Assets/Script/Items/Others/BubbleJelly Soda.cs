using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleJellySoda : Items
{
    public override int ID { get; set; } = 2;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;

    bool propertie_added = false;
    void Start()
    {
        PlayerController player = transform.GetComponent<PlayerController>();
        if (player != null)
        {
            player.player_properties.bullet_speed += 0.5f;
            propertie_added = true;
        }
    }
    void Update()
    {
        if (propertie_added)
        {
            Destroy(this);
        }
        else 
        {
            PlayerController player = transform.GetComponent<PlayerController>();
            if (player != null)
            {
                player.player_properties.bullet_speed += 0.5f;
                propertie_added = true;
            }            
        }
    }
}

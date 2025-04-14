using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeSupercharge : Items
{
    public override int ID { get; set; } = 5;
    public override ItemRanks Rank { get; set; } = ItemRanks.B;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;

    void Start()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.player_properties.bullet_exist_time += player.player_properties.bullet_exist_time*0.1f;
            Destroy(this);
        }
    }
}

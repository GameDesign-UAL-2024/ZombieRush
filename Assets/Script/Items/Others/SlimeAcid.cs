using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeAcid : Items
{
    public override int ID { get; set; } = 3;
    public override ItemRanks Rank { get; set; } = ItemRanks.B;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;

    void Start()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.player_properties.damage += 0.2f;
            player.player_properties.bullet_speed *= 0.1f;
            Destroy(this);
        }
    }
}

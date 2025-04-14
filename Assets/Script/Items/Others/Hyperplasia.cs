using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hyperplasia : Items
{
    public override int ID { get; set; } = 6;
    public override ItemRanks Rank { get; set; } = ItemRanks.B;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;

    void Start()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.player_properties.max_health += 2;
            player.player_properties.current_health += 2;
            Destroy(this);
        }
    }
}

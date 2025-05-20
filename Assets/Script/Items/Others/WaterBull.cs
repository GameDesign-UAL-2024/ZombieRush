using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBull : Items
{
    public override int ID { get; set; } = 12;
    public override ItemRanks Rank { get; set; } = ItemRanks.A;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;

    PlayerController player;
    static float damage_temp;
    bool is_upping;
    void Start()
    {
        player = GetComponent<PlayerController>();
        is_upping = false;
    }

    void Update()
    {
        if (ChunkGenerator.Instance != null && ChunkGenerator.Instance.IsTileOfType(transform.position, ChunkGenerator.Instance.waterTile))
        {
            if (!is_upping && damage_temp == 0)
            {
                damage_temp = player.player_properties.damage;
                player.player_properties.damage += 1;
                is_upping = true;
            }          
        }
        else if (is_upping)
        {
            player.player_properties.damage = damage_temp;
            damage_temp = 0;
            is_upping = false;
        }
    }
}

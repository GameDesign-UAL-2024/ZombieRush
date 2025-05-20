using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sha : Items
{
    public override int ID { get; set; } = 13;
    public override ItemRanks Rank { get; set; } = ItemRanks.S;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;

    PlayerController player;
    void Start()
    {
        player = GetComponent<PlayerController>();
        GlobalEventBus.OnEnemyDead.AddListener(OnEnemyKilled);
    }

    void OnEnemyKilled()
    {
        if (player != null)
        {
            player.player_properties.damage += 0.012f;
        }
    }
}

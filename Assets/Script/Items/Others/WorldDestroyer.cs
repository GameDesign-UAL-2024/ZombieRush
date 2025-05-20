using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDestroyer : Items
{
    public override int ID { get; set; } = 14;
    public override ItemRanks Rank { get; set; } = ItemRanks.S;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;
    PlayerController player;
    void Start()
    {
        player = GetComponent<PlayerController>();
        GlobalEventBus.OnGridInteracted.AddListener(OnInteractableGrid);       
    }
    void OnInteractableGrid(InteractableGrids grids)
    {
        if (player != null)
        {
            player.player_properties.damage += 0.01f;
            player.player_properties.atk_speed += 0.02f;
        }
    }
}

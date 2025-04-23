using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FourLeaf : Items
{
    public override int ID { get; set; } = 4;
    public override ItemRanks Rank { get; set; } = ItemRanks.B;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;
    // Start is called before the first frame update
    void Start()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.player_properties.luck += 0.1f;
            player.PlayPropertieUpAnimation();
            Destroy(this,0.1f);
        }
    }
}

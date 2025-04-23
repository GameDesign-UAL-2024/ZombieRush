using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Statues : MonoBehaviour
{
    Globals global;
    GameObject item;
    bool item_exist = false;
    [SerializeField] bool IsSatan;
    PlayerController player;
    Items.ItemRanks? rank;
    void Start()
    {
        global = Globals.Instance;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }
    void Update()
    {
        if (global.Event.in_battle)
        {
            Destroy(this.gameObject);
        }
        if (IsSatan && item_exist && item == null && player != null && rank != null)
        {
            if (rank == Items.ItemRanks.S)
            {
                player.player_properties.current_health -= player.player_properties.max_health * 0.3f;
            }
            else if ( rank == Items.ItemRanks.A )
            {
                player.player_properties.current_health -= player.player_properties.max_health * 0.2f;
            }
            else
            {
                player.player_properties.current_health -= player.player_properties.max_health * 0.15f;
            }
            Destroy(this.gameObject);
        }
    }
    public void write_item(GameObject the_object)
    {
        item = the_object;
        rank = ItemFactory.GetRankByID(item.GetComponent<ItemInWorld>().self_item_id);
        item_exist = true;
    }
}

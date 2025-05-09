using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // 需要这个命名空间
public class Statues : MonoBehaviour
{
    Globals global;
    GameObject item1, item2, item3;
    Items.ItemRanks rank1, rank2, rank3;
    bool itemsInitialized = false;
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
            Destroy(gameObject);
            return;
        }

        if (!itemsInitialized || player == null)
            return;
        if (IsSatan)
        {
            // —— 1. item1 被拾走时（第一次从非 null 变成 null）扣血并“吃掉”这次状态 —— 
            if (item1 == null)
            {
                // 扣血逻辑
                float ratio1 = rank1 switch
                {
                    Items.ItemRanks.S => 0.6f,
                    Items.ItemRanks.A => 0.3f,
                    Items.ItemRanks.B => 0.15f,
                    _ => 0f
                };
                player.player_properties.current_health -= player.player_properties.max_health * ratio1;

                // 将 item1 标记为“已处理”——指向自身，使它不再为 null
                item1 = this.gameObject;
            }

            // —— 2. item2 同理 —— 
            if (item2 == null)
            {
                float ratio2 = rank2 switch
                {
                    Items.ItemRanks.S => 0.6f,
                    Items.ItemRanks.A => 0.3f,
                    Items.ItemRanks.B => 0.15f,
                    _ => 0f
                };
                player.player_properties.current_health -= player.player_properties.max_health * ratio2;
                item2 = this.gameObject;
            }

            // —— 3. item3 同理 —— 
            if (item3 == null)
            {
                float ratio3 = rank3 switch
                {
                    Items.ItemRanks.S => 0.6f,
                    Items.ItemRanks.A => 0.3f,
                    Items.ItemRanks.B => 0.15f,
                    _ => 0f
                };
                player.player_properties.current_health -= player.player_properties.max_health * ratio3;
                item3 = this.gameObject;
            }
        }
        else
        {
            // —— 1. item1 被拾走时 —— 
            if (item1 == null)
            {
                float ratio1 = rank1 switch
                {
                    Items.ItemRanks.S => 0.05f,
                    Items.ItemRanks.A => 0.10f,
                    Items.ItemRanks.B => 0.15f,
                    _ => 0f
                };
                player.player_properties.current_health += player.player_properties.max_health * ratio1;

                // 把自己当“已处理”标记
                item1 = this.gameObject;
                Destroy(item2,0.1f);
                Destroy(item3,0.1f);
                Destroy(this.gameObject,0.2f);
            }

            // —— 2. item2 同理 —— 
            if (item2 == null)
            {
                float ratio2 = rank2 switch
                {
                    Items.ItemRanks.S => 0.05f,
                    Items.ItemRanks.A => 0.10f,
                    Items.ItemRanks.B => 0.15f,
                    _ => 0f
                };
                player.player_properties.current_health += player.player_properties.max_health * ratio2;
                item2 = this.gameObject;
                Destroy(item1,0.1f);
                Destroy(item3,0.1f);
                Destroy(this.gameObject,0.2f);
            }

            // —— 3. item3 同理 —— 
            if (item3 == null)
            {
                float ratio3 = rank3 switch
                {
                    Items.ItemRanks.S => 0.05f,
                    Items.ItemRanks.A => 0.10f,
                    Items.ItemRanks.B => 0.15f,
                    _ => 0f
                };
                player.player_properties.current_health += player.player_properties.max_health * ratio3;
                item3 = this.gameObject;
                Destroy(item2,0.1f);
                Destroy(item1,0.1f);
                Destroy(this.gameObject,0.2f);
            }
        }

    }


    public void write_items(GameObject the_object_1 , GameObject the_object_2 , GameObject the_object_3)
    {
        item1 = the_object_1;
        item2 = the_object_2;
        item3 = the_object_3;
        if (item1 != null)
        {
            Items.ItemRanks? rank = ItemFactory.GetRankByID(item1.GetComponent<ItemInWorld>().self_item_id);
            if (rank != null)
            {
                rank1 = rank.Value;
            }
        }
        if (item2 != null)
        {
            Items.ItemRanks? rank = ItemFactory.GetRankByID(item2.GetComponent<ItemInWorld>().self_item_id);
            if (rank != null)
            {
                rank2 = rank.Value;
            }
        }
        if (item3 != null)
        {
            Items.ItemRanks? rank = ItemFactory.GetRankByID(item3.GetComponent<ItemInWorld>().self_item_id);
            if (rank != null)
            {
                rank3 = rank.Value;
            }
        }
        itemsInitialized = true;
    }
}

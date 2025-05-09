using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DaYun : Items
{
    string dayun_image = "Prefabs/Trunk";
    PlayerUI ui_instance;
    GameObject dayun;
    GameObject dayun_instance;
    float cool_down_time = 10f;
    float current_cool_time = 0f;
    public override int ID {get; set;} = 11;
    public override ItemRanks Rank{get; set;} = ItemRanks.A;
    PlayerController player;
    float speed_temp;
    public override ItemTypes Type{get; set;} = ItemTypes.Proactive;
    
    void Start()
    {
        dayun = Addressables.LoadAssetAsync<GameObject>(dayun_image).WaitForCompletion();
        player = GetComponent<PlayerController>();
        ui_instance = PlayerUI.Instance;
        if (ui_instance.ProactiveItemImage != null)
        {
            string img_path = "Sprites/Items/" + ID.ToString();
            Sprite item_image = Resources.Load<Sprite>(img_path);
            if (item_image == null)
                return;
            ui_instance.ProactiveItemImage.sprite = item_image;
        }
    }
    private void Update()
    {
        // 累加冷却计时
        if (current_cool_time < cool_down_time)
        {
            current_cool_time += Time.deltaTime;
            if (ui_instance.CoolDownText != null)
            {
                ui_instance.CoolDownText.text = Mathf.CeilToInt(cool_down_time - current_cool_time).ToString();
            }
        }
        else
        {
            if (ui_instance.CoolDownText != null)
            {
                ui_instance.CoolDownText.text = "";
            }               
        }
        if (dayun_instance != null)
        {
            if (GetComponent<SpriteRenderer>().flipX)
            {
                dayun_instance.transform.localScale = new Vector3(-1,1,1);
            }
            else
            {
                dayun_instance.transform.localScale = new Vector3(1,1,1);
            }
            dayun_instance.transform.position = transform.position + new Vector3(0,0,-0.1f);
        }
        // 按下空格，且冷却时间已到，才可发射
        if (Input.GetKeyDown(KeyCode.Space) && dayun != null && current_cool_time >= cool_down_time && dayun_instance == null)
        {
            speed_temp = player.moveSpeed;
            player.moveSpeed = speed_temp*1.5f;
            dayun_instance = Instantiate(dayun,transform.position,Quaternion.identity);
            ActiveDayun();
            current_cool_time = 0f;  // 重置冷却计时
        }
    }

    void ActiveDayun()
    {
        StartCoroutine(DayunInstanceLogic());
    }

    IEnumerator DayunInstanceLogic()
    {
        yield return new WaitForSeconds(4f);

        if (dayun_instance != null)
        {
            player.moveSpeed = speed_temp;
            Destroy(dayun_instance);
        }
    }

}

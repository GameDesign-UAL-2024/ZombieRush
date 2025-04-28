using System;
using UnityEngine.AddressableAssets;
using UnityEngine;

public class HumanArmsController : Items
{
    public override int ID {get; set;} = 10;
    public override ItemRanks Rank {get; set;} = ItemRanks.A;
    public override ItemTypes Type {get; set;} = ItemTypes.Additional_Attack;
    GameObject player;
    const string Anim_Parameter_Moving = "Moving";
    const string Anim_Parameter_Attacking = "Attacking";
    const string ArmsPrefab_Adress = "Prefabs/HumanArms";
    static HumanArms arms_instance;
    Globals global;

    // Start is called before the first frame update
    void Start()
    {
        global = Globals.Instance;
        GameObject arms_obj = Addressables.LoadAssetAsync<GameObject>(ArmsPrefab_Adress).WaitForCompletion();
        if (gameObject.tag == "Player")
        {
            arms_instance = Instantiate(arms_obj , transform.position , Quaternion.identity).GetComponent<HumanArms>();
        }
        
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (global.Event.current_state != Globals.Events.GameState.playing)
            return;

        // 1. 同步位置
        Vector3 playerPos = player.transform.position;
        arms_instance.transform.position = playerPos;

        // 2. 计算鼠标在世界坐标中的位置（忽略 Z）
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld3D = Camera.main.ScreenToWorldPoint(mouseScreen);
        Vector2 mouseWorld2D = new Vector2(mouseWorld3D.x, mouseWorld3D.y);

        // 3. 计算从臂膀到鼠标的方向向量
        Vector2 dir = (mouseWorld2D - new Vector2(playerPos.x, playerPos.y)).normalized;

        // 4. 根据向量计算角度（Rad2Deg 得到的是以 X 轴为 0°，假设你的臂膀 sprite 默认朝“上”，所以再 -90°）
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        // 5. 只在 Z 轴上旋转
        arms_instance.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 6. 攻击参数
        if (Input.GetMouseButton(0))
            arms_instance.SetParameter(Anim_Parameter_Attacking, true);
        else
            arms_instance.SetParameter(Anim_Parameter_Attacking, false);
    }
    void OnDestroy()
    {
        // 如果手臂还在场上就一并销毁
        if (arms_instance != null)
            Destroy(arms_instance.gameObject);
    }
}

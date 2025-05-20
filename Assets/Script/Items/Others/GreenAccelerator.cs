using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenAccelerator : Items
{
    public override int ID { get; set; } = 15;
    public override ItemRanks Rank { get; set; } = ItemRanks.S;
    public override ItemTypes Type { get; set; } = ItemTypes.Properties;
    PlayerController player;
    float speed_temp;
    void Start()
    {
        player = GetComponent<PlayerController>();
        GlobalEventBus.OnResourceCollect.AddListener(OnResourceCollected);
    }
    void OnResourceCollected(ResourcePickup resource)
    {
        if (player != null && resource.resourceType == Globals.Datas.ResourcesType.Green)
        {
            if (speed_temp == 0)
            {
                speed_temp = player.moveSpeed;
            }
            StartCoroutine(Accelerate(player));
        }
    }

    private IEnumerator Accelerate(PlayerController playerController)
    {
        playerController.moveSpeed += 1;
        yield return new WaitForSeconds(1f);
        playerController.moveSpeed -= 1;
    }
}

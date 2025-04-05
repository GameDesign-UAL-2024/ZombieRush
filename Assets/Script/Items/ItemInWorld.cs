using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInWorld : MonoBehaviour
{
    int self_item;
    PlayerItemManager PIM;
    // Start is called before the first frame update
    void Start()
    {
        PIM = PlayerItemManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

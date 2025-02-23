using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlackResource : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Globals.Instance.Data.AddResourceAmount(Globals.Datas.ResourcesType.Black , 5);
            Destroy(gameObject);
        }    
    }
}

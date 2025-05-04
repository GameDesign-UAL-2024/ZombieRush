using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BlackResource : MonoBehaviour
{
    Rigidbody2D RB;

    void Start()
    {
        RB = transform.GetComponent<Rigidbody2D>();
        
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Globals.Instance.Data.AddResourceAmount(Globals.Datas.ResourcesType.Black , 5);
            Destroy(gameObject);
        }    
    }
    void FixedUpdate()
    {
        if (RB != null)
        {
            if (RB.velocity.magnitude > 0)
            {
                RB.velocity *= 0.9f;
            }
        }
    }
}

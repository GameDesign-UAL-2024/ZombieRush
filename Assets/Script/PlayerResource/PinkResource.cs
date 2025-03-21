using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinkResource : MonoBehaviour
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
            Globals.Instance.Data.AddResourceAmount(Globals.Datas.ResourcesType.Pink , 5);
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

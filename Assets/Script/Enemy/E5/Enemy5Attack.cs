using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy5Attack : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerController>().ReduceLife(3f);
        }
    }
}

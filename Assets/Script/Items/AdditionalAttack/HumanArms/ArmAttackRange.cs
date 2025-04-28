using System;
using UnityEngine;
using UnityEngine.Events;

public class ArmAttackRange : MonoBehaviour
{
    public UnityEvent<Enemy> on_enmie_hit = new UnityEvent<Enemy>();
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Enemy")
        {
            on_enmie_hit.Invoke(collision.GetComponent<Enemy>());
        }
    }
 
    public void AddListenerForThis(UnityAction<Enemy> func)
    {
        on_enmie_hit.AddListener(func);
    }
}

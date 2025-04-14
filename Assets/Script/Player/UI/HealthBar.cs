using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] GameObject bar_img;
    [SerializeField] GameObject bar_white_img;
    PlayerController player;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player != null)
        {
            float max_health = player.player_properties.max_health;
            float current_health = player.player_properties.current_health;

            float x_scale = current_health / max_health;
            bar_img.transform.localScale = new Vector3(x_scale , 1 , 1);
            if (bar_white_img.transform.localScale.x > x_scale)
            {
                bar_white_img.transform.localScale = Vector3.Lerp(bar_white_img.transform.localScale , new Vector3(x_scale , 1 , 1) , 0.05f);
            }
            else
            {
                bar_white_img.transform.localScale = new Vector3(x_scale , 1 , 1);
            }
        }
    }
}

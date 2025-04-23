using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LittleMap : MonoBehaviour
{
    GameObject player;
    Globals global;
    List<Enemy> enemies;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        enemies = Globals.Datas.EnemyPool;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

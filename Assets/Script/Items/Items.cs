using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Items : MonoBehaviour
{
    public abstract int ID { get; set; }
    public enum ItemTypes{ Properties , BulletEffect , ShootBehaviour_Bullet , ShootBehaviour_Lazer , BulletBehaviour , Additional_Attack , Proactive , None}
    public abstract ItemTypes Type { get; set; }

}

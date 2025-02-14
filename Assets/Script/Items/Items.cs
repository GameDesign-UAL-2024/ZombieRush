using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Items : MonoBehaviour
{
    public abstract int ID { get; set; }
    public enum ItemTypes{ Properties , BulletEffect , ShootBehaviour , BulletBehaviour , None}
    public abstract ItemTypes Type { get; set; }

    public abstract void OnShoot();
    public abstract void ActiveBulletEffects();
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Buildings : MonoBehaviour
{
    public enum BuildingType{ Barriar , Attack , Trap , Camp , Resource_Generate }
    public abstract BuildingType this_type {get; set;}
    public abstract float max_health {get; set;}
    public abstract float current_health {get; set;}

    
    public abstract void TakeDamage(float amount);
    
}

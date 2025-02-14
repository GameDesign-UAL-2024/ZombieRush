using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractableGrids : MonoBehaviour
{
    public abstract void OnPlayerClick();
    public abstract void OnPlayerHold();
}

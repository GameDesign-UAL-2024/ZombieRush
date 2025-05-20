using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalTimer : MonoBehaviour
{
    public static GlobalTimer Instance {get; private set;}
    float current_time;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        if (Globals.Instance != null)
        {
            if (Globals.Instance.Event.current_state == Globals.Events.GameState.playing)
            {
                current_time += Time.deltaTime;
            }
        }
    }
    public float GetCurrentTime()
    {
        return current_time;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bar : MonoBehaviour
{
    public GameObject value;
    private float _percentage;
    float percentage{ get => _percentage; set => _percentage = Math.Clamp(value, 0f, 1f); }
    // Start is called before the first frame update
    void Start()
    {
        percentage = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (value != null)
        {
            Vector3 current_scale = value.transform.localScale;
            value.transform.localScale = new Vector3(percentage , current_scale.y , current_scale.z);
        }
    }
    public void SetValue(float target_value)
    {
        percentage = target_value;
    }
}

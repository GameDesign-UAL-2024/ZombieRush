using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    
    public static PlayerUI Instance {get; private set;}
    void Awake()
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
    public TextMeshProUGUI green_value;
    public TextMeshProUGUI black_value;
    // public TextMeshPro pink_value;

    Globals globals;
    void Start()
    {
        globals = Globals.Instance;
    }

    public void UpdateValues(Globals.Datas.ResourcesType the_type , int number)
    {
        if ( the_type == Globals.Datas.ResourcesType.Green )
        {
            green_value.text = number.ToString();
        }
        else if ( the_type == Globals.Datas.ResourcesType.Black )
        {
            black_value.text = number.ToString();
        }
        else
        {

        }
    }
}

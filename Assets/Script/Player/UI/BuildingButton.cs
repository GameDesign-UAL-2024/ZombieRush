using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BuildingButton : MonoBehaviour
{

    private bool initialized;
    Dictionary<string , string> this_properties;
    UnityEngine.UI.Button button_comp;
    public Image image;
    int green_value;
    int black_value;
    int pink_value;

    string dectribe_text;
    // Start is called before the first frame update
    void Awake()
    {
        button_comp = transform.GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(string img_path, Dictionary<string , string> properties , int Green_Need , int  Black_Need , int Pink_Need , UnityAction Listener)
    {
        green_value = Green_Need;
        black_value = Black_Need;
        pink_value = Pink_Need;
        this_properties = properties;
        button_comp.onClick.AddListener(Listener);
        SetImageSprite(img_path);
    }


    private void SetImageSprite(string img_path)
    {
        // 从 Resources 文件夹加载 Sprite
        Sprite newSprite = Resources.Load<Sprite>(img_path);

        if (newSprite != null)
        {
            image.sprite = newSprite;
        }
        else
        {
            image.color = Color.cyan;
            Debug.Log($"无法加载路径为 '{img_path}' 的图片。请确保该图片位于 Resources 文件夹内，且路径正确。");
        }
    }
}

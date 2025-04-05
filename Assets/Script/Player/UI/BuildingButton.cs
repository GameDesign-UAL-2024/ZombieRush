using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class BuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // 本地化按钮是否初始化
    private bool initialized;
    // 当前按钮对应的 id
    int this_id;
    // 按钮相关属性
    Dictionary<string, string> this_properties;
    // 按钮组件引用
    UnityEngine.UI.Button button_comp;
    // 按钮图片引用
    public Image image;
    // 资源需求数值
    int green_value;
    int black_value;
    int pink_value;
    // PlayerUI 实例引用
    PlayerUI UI_Instance;
    // 描述文本
    string dectribe_text;

    void Awake()
    {
        button_comp = transform.GetComponent<Button>();
    }

    void Start()
    {
        UI_Instance = PlayerUI.Instance;
    }

    public void Initialize(string img_path, int id, Dictionary<string, string> properties, int Green_Need, int Black_Need, int Pink_Need, UnityAction<int> Listener)
    {
        green_value = Green_Need;
        black_value = Black_Need;
        pink_value = Pink_Need;
        this_id = id;
        this_properties = properties;
        button_comp.onClick.AddListener(() => Listener(this_id));
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (UI_Instance != null)
        {
            UI_Instance.ShowDescription(this_id, 1);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UI_Instance != null)
        {
            UI_Instance.ClearDescription();
        }
    }
}

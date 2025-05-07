using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public static CustomCursor  Instance;
    public enum MouseNoticeType {Left , Right};
    [SerializeField] SpriteRenderer MouseNotice;
    [SerializeField] Sprite LeftNotice;
    [SerializeField] Sprite RightNotice;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        else 
        {
            Instance = this;
        }
    }
    void Update()
    {
        // 将鼠标位置转换为世界坐标
        Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = cursorPosition;
    }

    public void UseMouseNotice(MouseNoticeType type)
    {
        if(type == MouseNoticeType.Left)
        {
            MouseNotice.sprite = LeftNotice;
        }
        else
        {
            MouseNotice.sprite = RightNotice;
        }
    }

    public void ClearMouseNotice()
    {
        MouseNotice.sprite = null;
    }
}

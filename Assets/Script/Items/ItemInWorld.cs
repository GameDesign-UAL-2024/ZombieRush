
using UnityEngine;

public class ItemInWorld : MonoBehaviour
{
    public int self_item_id {get; private set;}
    PlayerItemManager PIM;
    PlayerUI UI_INSTANCE;
    Animator animator;
    Globals global;
    [SerializeField]SpriteRenderer self_image;
    [SerializeField]Sprite S_img;
    [SerializeField]Sprite A_img;
    [SerializeField]Sprite B_img;
    SpriteRenderer bottom_renderer; // 底座的Renderer
    Sprite item_image;
    Collider2D col2D;
    bool Initialized = false;
    // Start is called before the first frame update
    void Awake()
    {
        PIM = PlayerItemManager.Instance;
        UI_INSTANCE = PlayerUI.Instance;
        animator = transform.GetComponent<Animator>();
        bottom_renderer = transform.GetComponent<SpriteRenderer>();
        col2D = GetComponent<Collider2D>();
        if (self_image != null)
            self_image.enabled = false;
    }
    void Start()
    {
        global = Globals.Instance;
    }
    public void Initialize(int item_id , Items.ItemRanks rank)
    {
        self_item_id = item_id;
        string img_path = "Sprites/Items/" + self_item_id.ToString();
        item_image = Resources.Load<Sprite>(img_path);
        if (rank == Items.ItemRanks.S)
            bottom_renderer.sprite = S_img;
        else if (rank == Items.ItemRanks.A)
            bottom_renderer.sprite = A_img;
        else
            bottom_renderer.sprite = B_img;    

        if (item_image == null)
        {
            Debug.Log($"img not found:{self_item_id}");
        }
        else
        {
            self_image.sprite = item_image;
        }
        self_image.enabled = true;
        Initialized = true;
    }

    void Update()
    {
        if (!Initialized) return;

        // 如果进入战斗，自动销毁
        if (global.Event.in_battle)
        {
            UI_INSTANCE.ClearDescription();
            Destroy(gameObject);
            return;
        }

        // 把鼠标坐标转成世界坐标 + 2D
        Vector3 worldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 worldPos2D = new Vector2(worldPos3D.x, worldPos3D.y);

        // 发一条长度为 0 的射线，检测鼠标点下落在哪个 Collider2D 上
        RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);
        if (hit.collider == col2D)
        {
            // 鼠标在物体上
            UI_INSTANCE.ShowDescription(self_item_id, 0);

            // 右键点击
            if (Input.GetMouseButtonDown(1))
            {
                PIM.AddItem(self_item_id);
                UI_INSTANCE.ClearDescription();
                Destroy(gameObject);
            }
        }
        else
        {
            // 鼠标不在物体上就清掉
            UI_INSTANCE.ClearDescription();
        }
    }
    void Dissappear()
    {
        Destroy(this.gameObject);
    }
    bool IsMouseOverSpritePixel(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
        {
            return false;
        }
        
        // 获取鼠标在世界坐标中的位置
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // 使用 renderer 的 z 坐标，确保转换准确
        worldPos.z = renderer.transform.position.z;
        
        // 转换为 Sprite 的局部坐标（使用 renderer.transform 而不是当前组件的 transform）
        Vector2 localPos = renderer.transform.InverseTransformPoint(worldPos);

        Sprite sprite = renderer.sprite;
        Rect textureRect = sprite.textureRect;
        Vector2 pivot = sprite.pivot;
        float pixelsPerUnit = sprite.pixelsPerUnit;

        // 将局部坐标转换为以像素为单位的坐标（参照 Sprite 的 pivot）
        Vector2 pixelPos = new Vector2(localPos.x * pixelsPerUnit + pivot.x,
                                    localPos.y * pixelsPerUnit + pivot.y);

        // 检查是否在 Sprite 纹理矩形内
        if (pixelPos.x < 0 || pixelPos.x > textureRect.width ||
            pixelPos.y < 0 || pixelPos.y > textureRect.height)
        {
            return false;
        }

        int texX = Mathf.FloorToInt(pixelPos.x + textureRect.x);
        int texY = Mathf.FloorToInt(pixelPos.y + textureRect.y);

        UnityEngine.Color pixelColor = sprite.texture.GetPixel(texX, texY);
        return pixelColor.a > 0.01f;
    }
}

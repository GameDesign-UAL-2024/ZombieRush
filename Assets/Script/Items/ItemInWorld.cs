
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
    bool wasHovering = false;  
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

        if (global.Event.in_battle)
        {
            UI_INSTANCE.ClearDescription();
            Destroy(gameObject);
            return;
        }

        // 把鼠标坐标转成世界坐标 + 2D
        Vector3 worldPos3D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 worldPos2D = new Vector2(worldPos3D.x, worldPos3D.y);

        // 点检测
        RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);
        bool isHovering = (hit.collider == col2D);

        if (isHovering)
        {
            // 只要悬停就持续显示
            UI_INSTANCE.ShowDescription(self_item_id, 0);
            if (CustomCursor.Instance != null)
            {
                CustomCursor.Instance.UseMouseNotice(CustomCursor.MouseNoticeType.Right);
            }
            if (Input.GetMouseButtonDown(1))
            {
                PIM.AddItem(self_item_id);
                UI_INSTANCE.ClearDescription();
                Destroy(gameObject);
                return;
            }
        }
        else if (wasHovering)
        {
            // 只有从“悬停”到“未悬停”这一帧，才清一次
            if (CustomCursor.Instance != null)
            {
                CustomCursor.Instance.ClearMouseNotice();
            }
            UI_INSTANCE.ClearDescription();
        }
    
        // 更新状态给下一帧用
        wasHovering = isHovering;
    }
    void OnDestroy()
    {
        if (CustomCursor.Instance != null)
        {
            CustomCursor.Instance.ClearMouseNotice();
        }
    }
}

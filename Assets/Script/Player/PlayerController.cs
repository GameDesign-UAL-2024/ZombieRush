using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f; // 移动速度
    private Vector2 moveInput;
    ObjectSpawner objectSpawner;
    Animator anim;
    SpriteRenderer sprite;
    Rigidbody2D RB;
    public Properties player_properties;
    public class Properties
    {
        public float bullet_speed;
        public float damage;
        public int max_health;
        public float bullet_exist_time;
        public int luck;
        
        public Properties()
        {
            bullet_speed = 5f;
            damage = 1f;
            max_health = 6;
            bullet_exist_time = 3f;
            luck = 1;
        }
    } 

    void Start()
    {
        if (SceneManager.GetActiveScene().name == "0_0")
        {
            Globals.Instance.Event.GameStart();
        }
        player_properties = new Properties();
        anim = transform.GetComponent<Animator>();
        sprite = transform.GetComponent<SpriteRenderer>();
        RB = transform.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;
        if (moveInput.magnitude > 0.2f)
        {
            anim.SetBool("Moving", true);
        }
        else
        {
            anim.SetBool("Moving", false);
        }
        if (moveInput.x > 0.1f) { sprite.flipX = false; }
        else if (moveInput.x < -0.1f) { sprite.flipX = true; }

        // 只在鼠标点击且不在UI上时触发攻击
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            anim.SetTrigger("Attack");
        }

        // 计算新位置并更新
        Vector3 newPosition = transform.position + (Vector3)moveInput * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, 0, 199);
        newPosition.y = Mathf.Clamp(newPosition.y, 0, 199);
        transform.position = newPosition;

        if (objectSpawner != null)
        {
            objectSpawner.UpdateSpawnedObjects(transform.position);
        }
    }

    public void SetObjectSpawner(ObjectSpawner com)
    {
        objectSpawner = com;
    }
    void FixedUpdate()
    {
        RB.velocity = Vector2.Lerp(RB.velocity, Vector2.zero, 2f * Time.fixedDeltaTime);
    }
    private bool IsPointerOverUI()
    {
        // 创建一个指针事件数据，使用当前鼠标位置
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        // 射线检测所有UI元素
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        // 如果检测到的UI元素数量大于0，则返回true
        return results.Count > 0;
    }
}

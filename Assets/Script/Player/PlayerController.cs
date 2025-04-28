using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
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
    CameraEffects camera_effect;
    static string properties_up_animation = "Prefabs/PropertiesUp";
    GameObject properties_up_prefab;
    [SerializeField] Transform properties_up_location;
    Globals global;
    public class Properties
    {
        public float bullet_speed;
        public float damage;
        public float max_health;
        public float current_health;
        public float bullet_exist_time;
        public float luck;
        
        public Properties()
        {
            bullet_speed = 5f;
            damage = 1f;
            max_health = 6;
            current_health = max_health;
            bullet_exist_time = 3f;
            luck = 1;
        }
    } 
    public bool ReduceLife(float value)
    {
        if (player_properties == null){return false;}
        player_properties.current_health -= value;
        if (player_properties.current_health < 0){ player_properties.current_health = 0; }
        if (camera_effect != null)
        { 
            if ((value/player_properties.max_health) < 0.1)
            {
                camera_effect.Shake(0.3f , 1f , 1.5f , 3); 
            }
            else if ((value/player_properties.max_health) > 0.3)
            {
                camera_effect.Shake(0.5f , 1f , 1.5f , 5.5f); 
            }
            else
            {
                camera_effect.Shake(0.4f , 1f , 1.5f , 4); 
            }            
        }
        return true;
    }
    void Start()
    {
        if (SceneManager.GetActiveScene().name == "0_0")
        {
            //Globals.Instance.Event.GameStart();
        }
        player_properties = new Properties();
        anim = transform.GetComponent<Animator>();
        sprite = transform.GetComponent<SpriteRenderer>();
        global = Globals.Instance;
        RB = transform.GetComponent<Rigidbody2D>();
        camera_effect = CameraEffects.Instance;
        properties_up_prefab = Addressables.LoadAssetAsync<GameObject>(properties_up_animation).WaitForCompletion();
    }
    
    void Update()
    {
        if (global.Event.current_state != Globals.Events.GameState.playing)
        {
            return;
        }
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
    public void PlayPropertieUpAnimation()
    {
        if (properties_up_location != null && properties_up_prefab != null)
        {
            Instantiate(properties_up_prefab,properties_up_location.position,Quaternion.identity);
        }
    }
}

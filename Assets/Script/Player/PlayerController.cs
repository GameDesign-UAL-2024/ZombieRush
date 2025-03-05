using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private Vector2Int currentChunkPos;
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
        
        public Properties()
        {
            bullet_speed = 5f;
            damage = 1f;
            max_health = 6;
            bullet_exist_time = 3f;
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
        if (moveInput.magnitude > 0.2){anim.SetBool("Moving",true);}
        else{anim.SetBool("Moving",false);}
        if (moveInput.x > 0.1f) {sprite.flipX = false;}
        else if (moveInput.x < -0.1f) {sprite.flipX = true;}

        if (Input.GetMouseButtonDown(0)){anim.SetTrigger("Attack");}
        // 计算新位�?
        Vector3 newPosition = transform.position + (Vector3)moveInput * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, 0, 199);
        newPosition.y = Mathf.Clamp(newPosition.y, 0, 199);
        // 更新位置
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
}

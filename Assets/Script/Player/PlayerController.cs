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
    void Start()
    {
        if (SceneManager.GetActiveScene().name == "0_0")
        {
            Globals.Instance.Event.GameStart();
        }
        anim = transform.GetComponent<Animator>();
        sprite = transform.GetComponent<SpriteRenderer>();
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
        // 计算新位置
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
}

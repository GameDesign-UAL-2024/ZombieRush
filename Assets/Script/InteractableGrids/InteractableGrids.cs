using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class InteractableGrids : MonoBehaviour
{
    GameObject player;
    public enum GridType{ Trees , Bushes , Rocks};
    public float interacting_time; 
    public GridType this_type;
    float start_time;
    Animator animator;
    GameObject bar_object;
    static string bar_prefab = "Prefabs/Bar";

    bool is_interacting;
    bool on_sprite;
    bool in_area;
    void Start()
    {
        is_interacting = false;
        on_sprite = false;
        player = GameObject.FindGameObjectWithTag("Player");
        animator = transform.GetComponent<Animator>();
    }
    void Update()
    {
        OnMouseInSprite();
        IsInArea();
        OnPlayerClick();
        if (animator != null)
        {
            animator.SetBool("Interacting",is_interacting);
        }
        if (bar_object != null && interacting_time != 0)
        {
            float progress = (GlobalTimer.Instance.GetCurrentTime() - start_time) / interacting_time;
            if (! in_area || ! is_interacting)
            {
                Destroy(bar_object);
                bar_object = null;
            }
            else
            {
                bar_object.transform.GetComponent<Bar>().SetValue(progress);
            }

            if (progress >= 1)
            {
                Destroy(bar_object);
                GameObject object_spawn = GameObject.FindGameObjectWithTag("ObjectSpawner");
                Debug.Log($"找到:"+ object_spawn.GetInstanceID());
                if (object_spawn != null)
                {
                    object_spawn.GetComponent<ObjectSpawner>().RemoveObject(gameObject);
                }
            }
        }
    }

    private void OnBarLoaded(AsyncOperationHandle<GameObject> handle)
    {
        GameObject bar = handle.Result;
        bar_object = Instantiate(handle.Result, transform);
    }

    void OnPlayerClick()
    {
        if (Input.GetMouseButtonDown(1) && on_sprite && in_area && is_interacting == false)
        {
            is_interacting = true;
            start_time = GlobalTimer.Instance.GetCurrentTime();
            Addressables.LoadAssetAsync<GameObject>(bar_prefab).Completed += OnBarLoaded;
        }
    }
    void OnMouseInSprite()
    {
        Vector3 mouse_position = Input.mousePosition;
        mouse_position = Camera.main.ScreenToWorldPoint(mouse_position);
        mouse_position.z = transform.position.z;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            on_sprite = spriteRenderer.bounds.Contains(mouse_position);
        }
    }

    void IsInArea()
    {
        if (player != null)
        {
            if (Vector2.Distance(player.transform.position,transform.position) > 5)
            {
                in_area = false;
                is_interacting = false;
            }
            else
            {
                in_area = true;
            }
        }
    }
}

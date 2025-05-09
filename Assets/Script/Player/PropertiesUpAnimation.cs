using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropertiesUpAnimation : MonoBehaviour
{
    [SerializeField] AudioClip up;
    public void OnDestroy()
    {
        Destroy(gameObject);
    }
    public void PlayThisAudio()
    {
        AudioSysManager.Instance.PlaySound(GameObject.FindGameObjectWithTag("Player"),up,transform.position,1,false);
    }
}

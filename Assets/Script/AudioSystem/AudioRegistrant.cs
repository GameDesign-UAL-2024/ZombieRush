using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioRegistrant : MonoBehaviour 
{
    void OnDestroy()
    {
        if (AudioSysManager.Instance != null)
            AudioSysManager.Instance.UnregisterOwner(gameObject);
    }
}
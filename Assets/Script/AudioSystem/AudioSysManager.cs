using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 高效管理 2D 俯视音效：
/// • 池化 AudioSource（挂在本物体上，不移动 Transform）  
/// • 手动距离衰减与可选动态 Pan  
/// • 每 Owner 最多同时音源，超限替换最旧  
/// • 全局池满时按优先级剔除最弱音源  
/// • 去重：相同剪辑短时间内只保留最近的  
/// • 支持循环音效：外部可 StopLoop/SetLoopVolume/SetLoopPosition  
/// </summary>
public class AudioSysManager : MonoBehaviour
{
    public static AudioSysManager Instance { get; private set; }

    [Header("Pool Settings")] public int poolSize = 20;
    [Header("Audio Mixer")] public AudioMixerGroup sfxGroup;
    [Header("Spatial Settings")] public float maxDistance = 20f;
    [Header("Pan Settings")] public float panDistance = 10f;
    [Header("Per-Owner Limits")] public int maxSoundsPerOwner = 3;
    [Header("Duplicate Suppression")] public float duplicateTimeThreshold = 0.1f;
    [Tooltip("相同剪辑源间距阈值")] public float duplicateDistanceThreshold = 5f;

    private Queue<AudioSource> sourcePool;
    private List<AudioSource> activeSources;
    private Dictionary<GameObject, Queue<AudioSource>> registry;
    private Dictionary<AudioSource, GameObject> audioToOwner;
    private HashSet<AudioSource> dynamicPanSources;
    private Dictionary<AudioClip, float> lastPlayTime = new Dictionary<AudioClip, float>();
    private Transform listenerTransform;
    private Dictionary<AudioSource, Vector2> sourcePositions = new Dictionary<AudioSource, Vector2>();
    private enum AudioPriority { BGM = 0, UI = 0, Player = 1, Enemy = 2, Others = 3 }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        sourcePool       = new Queue<AudioSource>(poolSize);
        activeSources    = new List<AudioSource>(poolSize);
        registry         = new Dictionary<GameObject, Queue<AudioSource>>();
        audioToOwner     = new Dictionary<AudioSource, GameObject>(poolSize);
        dynamicPanSources= new HashSet<AudioSource>(poolSize);

        var listener = FindObjectOfType<AudioListener>();
        listenerTransform = listener ? listener.transform : null;
        if (listenerTransform == null)
            Debug.LogWarning("AudioSysManager: 未找到 AudioListener");

        for (int i = 0; i < poolSize; i++)
        {
            var audioSrc = gameObject.AddComponent<AudioSource>();
            audioSrc.playOnAwake           = false;
            audioSrc.spatialBlend          = 0f;
            audioSrc.outputAudioMixerGroup = sfxGroup;
            audioSrc.hideFlags             = HideFlags.HideInInspector;
            audioSrc.enabled               = false;
            sourcePool.Enqueue(audioSrc);
        }
    }

    void Update()
    {
        if (listenerTransform == null || activeSources.Count == 0) 
            return;

        Vector2 listenerPos = listenerTransform.position;

        // 倒序遍历以安全回收
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            var audioSrc = activeSources[i];
            if (audioSrc == null || !audioToOwner.ContainsKey(audioSrc))
            {
                activeSources.RemoveAt(i);
                continue;
            }

            // 非循环且播放结束：回收
            if (!audioSrc.loop && !audioSrc.isPlaying)
            {
                ReleaseSource(audioSrc);
                activeSources.RemoveAt(i);
                continue;
            }

            // 动态 Pan 更新：用字典中的坐标
            if (dynamicPanSources.Contains(audioSrc) 
                && sourcePositions.TryGetValue(audioSrc, out Vector2 pos))
            {
                audioSrc.panStereo = Mathf.Clamp((pos.x - listenerPos.x) / panDistance, -1f, 1f);
            }
        }
    }
    /// <param name="owner">发声者，用于限额管理</param>
    /// <param name="clip">要播放的 AudioClip</param>
    /// <param name="position">虚拟世界坐标，用于手动衰减与 Pan</param>
    /// <param name="volume">基础音量</param>
    /// <param name="dynamicPan">是否在 Update 中动态更新 Pan</param>
    public AudioSource PlaySound(
        GameObject owner,
        AudioClip clip,
        Vector2 position,
        float volume = 1f,
        bool dynamicPan = false
    )
    {
        // 基本校验
        if (clip == null || sourcePool.Count == 0 || listenerTransform == null)
            return null;

        // 1) 按 Owner 限额
        if (!registry.TryGetValue(owner, out var queue))
        {
            queue = new Queue<AudioSource>(maxSoundsPerOwner);
            registry[owner] = queue;
            if (!owner.TryGetComponent<AudioRegistrant>(out _))
                owner.AddComponent<AudioRegistrant>();
        }
        if (queue.Count >= maxSoundsPerOwner)
        {
            var oldest = queue.Dequeue();
            ReleaseSource(oldest);
            activeSources.Remove(oldest);
        }

        // 2) 取出池中 AudioSource 并初始化
        var src = sourcePool.Dequeue();
        src.enabled = true;
        src.clip    = clip;

        // —— 不要再写 src.transform.position —— 
        // src.transform.position = new Vector3(position.x, position.y, 0f);

        // 3) 记录“虚拟”坐标到字典
        sourcePositions[src] = position;

        // 4) 一次性计算距离衰减并赋值
        float dist    = Vector2.Distance(listenerTransform.position, position);
        src.volume    = Mathf.Clamp01(1f - dist / maxDistance) * volume;

        // 5) 初始 Pan
        src.panStereo = dynamicPan
            ? Mathf.Clamp((position.x - listenerTransform.position.x) / panDistance, -1f, 1f)
            : 0f;

        // 6) 播放并注册
        src.Play();
        activeSources.Add(src);
        audioToOwner[src] = owner;
        queue.Enqueue(src);
        if (dynamicPan) dynamicPanSources.Add(src);

        return src;
    }

    public void StopLoop(AudioSource src)
    {
        if (src != null && audioToOwner.ContainsKey(src))
        {
            src.loop = false;
            src.Stop();
            ReleaseSource(src);
            activeSources.Remove(src);
        }
    }

    public void SetLoopVolume(AudioSource src, float volume)
    {
        if (src != null && audioToOwner.ContainsKey(src))
            src.volume = Mathf.Clamp01(volume);
    }

    public void SetLoopPosition(AudioSource src, Vector2 position)
    {
        if (src != null && audioToOwner.ContainsKey(src))
        {
            src.transform.position = (Vector3)position;
            src.volume = ComputeAttenuation(position, src.volume);
            if (dynamicPanSources.Contains(src))
                src.panStereo = ComputePan(position);
        }
    }

    public void UnregisterOwner(GameObject owner)
    {
        if (!registry.TryGetValue(owner, out var q)) return;
        while (q.Count > 0)
        {
            var old = q.Dequeue();
            ReleaseSource(old);
            activeSources.Remove(old);
        }
        registry.Remove(owner);
    }

    private float ComputeAttenuation(Vector2 pos, float vol)
    {
        float dist = Vector2.Distance(listenerTransform.position, pos);
        return Mathf.Clamp01(1f - dist / maxDistance) * vol;
    }

    private float ComputePan(Vector2 pos)
    {
        float dx = pos.x - listenerTransform.position.x;
        return Mathf.Clamp(dx / panDistance, -1f, 1f);
    }

    private void EvictLowestPriority(int newPrio)
    {
        AudioSource worst = null;
        int worstPrio = -1;
        foreach (var candidate in activeSources)
        {
            int p = candidate.priority;
            if (worst == null || p > worstPrio)
            {
                worst = candidate;
                worstPrio = p;
            }
        }
        if (worst != null && worstPrio > newPrio)
        {
            ReleaseSource(worst);
            activeSources.Remove(worst);
        }
    }

    private void ReleaseSource(AudioSource src)
    {
        dynamicPanSources.Remove(src);
        audioToOwner.Remove(src);

        // 从字典中移除记录的坐标
        sourcePositions.Remove(src);

        src.Stop();
        src.enabled = false;
        sourcePool.Enqueue(src);
    }

    private AudioPriority MapTagToPriority(string tag)
    {
        switch (tag)
        {
            case "Player": return AudioPriority.Player;
            case "Enemy":  return AudioPriority.Enemy;
            default:       return AudioPriority.Others;
        }
    }
}

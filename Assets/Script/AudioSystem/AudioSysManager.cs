using System;
using System.Collections;
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
/// • BGM 播放与过渡逻辑：平滑替换并恢复
/// </summary>
public class AudioSysManager : MonoBehaviour
{
    public static AudioSysManager Instance { get; private set; }

    [Header("Audio Mixers")]
    [Tooltip("音效混音组")]
    public AudioMixerGroup sfxGroup;
    [Tooltip("BGM 混音组")]
    public AudioMixerGroup bgmGroup;

    [Header("BGM Settings")]
    [Tooltip("默认循环播放的 BGM 音频剪辑")]
    public AudioClip default_BGM;
    [Tooltip("默认 BGM 音量 (0-1)")]
    [Range(0f, 1f)] public float defaultBGMVolume = 0.15f;
    [Tooltip("淡入淡出时长 (秒)")]
    public float bgmFadeDuration = 1f;

    private AudioSource defaultBGMSource;
    private AudioSource overrideBGMSource;
    private Coroutine bgmCoroutine;

    [Header("Pool Settings")] public int poolSize = 30;
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
    private Dictionary<AudioClip, float> lastPlayTime;
    private Transform listenerTransform;
    private Dictionary<AudioSource, Vector2> sourcePositions;
    private enum AudioPriority { BGM = 0, UI = 0, Player = 1, Enemy = 2, Others = 3 }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        sourcePool = new Queue<AudioSource>(poolSize);
        activeSources = new List<AudioSource>(poolSize);
        registry = new Dictionary<GameObject, Queue<AudioSource>>();
        audioToOwner = new Dictionary<AudioSource, GameObject>(poolSize);
        dynamicPanSources = new HashSet<AudioSource>(poolSize);
        lastPlayTime = new Dictionary<AudioClip, float>();
        sourcePositions = new Dictionary<AudioSource, Vector2>();

        var listener = FindObjectOfType<AudioListener>();
        listenerTransform = listener ? listener.transform : null;
        if (listenerTransform == null)
            Debug.LogWarning("AudioSysManager: 未找到 AudioListener，无法正常播放空间音效");

        if (sfxGroup == null)
            Debug.LogWarning("AudioSysManager: sfxGroup 未设置，将使用默认 Mixer");

        // 初始化音效池
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = sfxGroup;
            src.hideFlags = HideFlags.HideInInspector;
            src.enabled = false;
            sourcePool.Enqueue(src);
        }
    }

    void Start()
    {
        // 默认 BGM
        defaultBGMSource = gameObject.AddComponent<AudioSource>();
        defaultBGMSource.playOnAwake = false;
        defaultBGMSource.clip = default_BGM;
        defaultBGMSource.loop = true;
        defaultBGMSource.outputAudioMixerGroup = bgmGroup;
        defaultBGMSource.volume = defaultBGMVolume;
        defaultBGMSource.spatialBlend = 0f;
        defaultBGMSource.Play();

        // Override BGM
        overrideBGMSource = gameObject.AddComponent<AudioSource>();
        overrideBGMSource.playOnAwake = false;
        overrideBGMSource.loop = false;
        overrideBGMSource.outputAudioMixerGroup = bgmGroup;
        overrideBGMSource.volume = 0f;
        overrideBGMSource.spatialBlend = 0f;
    }
    void Update()
    {
        // 如果没有监听器或当前没有活动音源，跳过
        if (listenerTransform == null || activeSources.Count == 0)
            return;

        // 缓存 Listener 的位置
        Vector2 listenerPos = listenerTransform.position;

        // 倒序遍历，安全地回收与更新
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            var src = activeSources[i];

            // 1) 如果 AudioSource 已丢失或已不属于我们管理的，就移除引用
            if (src == null || !audioToOwner.ContainsKey(src))
            {
                activeSources.RemoveAt(i);
                continue;
            }

            // 2) 非循环且已经播放完毕：回收到池中
            if (!src.loop && !src.isPlaying)
            {
                ReleaseSource(src);
                activeSources.RemoveAt(i);
                continue;
            }

            // 3) 如果启用了动态 Pan，则根据虚拟坐标更新左右声道平衡
            if (dynamicPanSources.Contains(src)
                && sourcePositions.TryGetValue(src, out Vector2 pos))
            {
                float pan = (pos.x - listenerPos.x) / panDistance;
                src.panStereo = Mathf.Clamp(pan, -1f, 1f);
            }
        }
    }

    /// <summary>
    /// 播放 Override BGM，sec 秒后或片段结束后恢复默认 BGM
    /// </summary>
    public void PlayBGM(AudioClip clip, float sec)
    {
        if (clip == null) return;
        if (bgmCoroutine != null) StopCoroutine(bgmCoroutine);
        bgmCoroutine = StartCoroutine(BGMRoutine(clip, sec));
    }

    /// <summary>
    /// 立即中断 Override，平滑恢复默认 BGM
    /// </summary>
    public void StopOverrideBGM()
    {
        if (bgmCoroutine != null)
        {
            StopCoroutine(bgmCoroutine);
            bgmCoroutine = StartCoroutine(StopOverrideRoutine());
        }
    }

    private IEnumerator BGMRoutine(AudioClip clip, float sec)
    {
        yield return StartCoroutine(CrossFadeToOverride(clip));
        float timer = 0f;
        while (timer < sec && overrideBGMSource.isPlaying)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        yield return StartCoroutine(CrossFadeToDefault());
        bgmCoroutine = null;
    }

    private IEnumerator CrossFadeToOverride(AudioClip clip)
    {
        overrideBGMSource.clip = clip;
        overrideBGMSource.time = 0f;
        overrideBGMSource.Play();
        float t = 0f;
        while (t < bgmFadeDuration)
        {
            t += Time.deltaTime;
            defaultBGMSource.volume = Mathf.Lerp(defaultBGMVolume, 0f, t / bgmFadeDuration);
            overrideBGMSource.volume = Mathf.Lerp(0f, defaultBGMVolume, t / bgmFadeDuration);
            yield return null;
        }
        defaultBGMSource.Pause();
    }

    private IEnumerator CrossFadeToDefault()
    {
        defaultBGMSource.UnPause();
        float t = 0f;
        while (t < bgmFadeDuration)
        {
            t += Time.deltaTime;
            overrideBGMSource.volume = Mathf.Lerp(defaultBGMVolume, 0f, t / bgmFadeDuration);
            defaultBGMSource.volume = Mathf.Lerp(0f, defaultBGMVolume, t / bgmFadeDuration);
            yield return null;
        }
        overrideBGMSource.Stop();
        overrideBGMSource.clip = null;
        overrideBGMSource.volume = 0f;
    }

    private IEnumerator StopOverrideRoutine()
    {
        float t = 0f;
        while (t < bgmFadeDuration)
        {
            t += Time.deltaTime;
            overrideBGMSource.volume = Mathf.Lerp(defaultBGMVolume, 0f, t / bgmFadeDuration);
            defaultBGMSource.volume = Mathf.Lerp(0f, defaultBGMVolume, t / bgmFadeDuration);
            yield return null;
        }
        overrideBGMSource.Stop();
        overrideBGMSource.clip = null;
        overrideBGMSource.volume = 0f;
        defaultBGMSource.UnPause();
        defaultBGMSource.volume = defaultBGMVolume;
        bgmCoroutine = null;
    }

    /// <summary>
    /// 播放一次性音效
    /// </summary>
    public AudioSource PlaySound(
        GameObject owner,
        AudioClip clip,
        Vector2 position,
        float volume = 1f,
        bool dynamicPan = false)
    {
        if (clip == null || sourcePool.Count == 0 || listenerTransform == null)
        {
            return null;
        }

        if (!registry.TryGetValue(owner, out var queue))
        {
            queue = new Queue<AudioSource>(maxSoundsPerOwner);
            registry[owner] = queue;
        }
        if (queue.Count >= maxSoundsPerOwner)
        {
            var old = queue.Dequeue();
            ReleaseSource(old);
            activeSources.Remove(old);
        }

        var src = sourcePool.Dequeue();
        src.enabled = true;
        src.loop = false;
        src.clip = clip;
        src.outputAudioMixerGroup = sfxGroup;
        src.spatialBlend = 0f;

        // 计算距离衰减与 Pan
        src.volume = ComputeAttenuation(position, volume);
        src.panStereo = dynamicPan ? ComputePan(position) : 0f;
        if (dynamicPan) dynamicPanSources.Add(src);
        sourcePositions[src] = position;

        //Debug.Log($"PlaySound: {clip.name} at {position}, vol={src.volume}");
        src.Play();

        activeSources.Add(src);
        audioToOwner[src] = owner;
        queue.Enqueue(src);
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
            sourcePositions[src] = position;
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

    private void ReleaseSource(AudioSource src)
    {
        dynamicPanSources.Remove(src);
        audioToOwner.Remove(src);
        sourcePositions.Remove(src);
        src.Stop();
        src.enabled = false;
        sourcePool.Enqueue(src);
    }

    private float ComputeAttenuation(Vector2 pos, float baseVol)
    {
        float dist = Vector2.Distance(listenerTransform.position, pos);
        return Mathf.Clamp01(1f - dist / maxDistance) * baseVol;
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
            int p = (int)MapTagToPriority(audioToOwner[candidate].tag);
            if (worst == null || p > worstPrio)
            {
                worst = candidate; worstPrio = p;
            }
        }
        if (worst != null && worstPrio > newPrio)
        {
            ReleaseSource(worst);
            activeSources.Remove(worst);
        }
    }

    private AudioPriority MapTagToPriority(string tag)
    {
        switch (tag)
        {
            case "Player": return AudioPriority.Player;
            case "Enemy": return AudioPriority.Enemy;
            default: return AudioPriority.Others;
        }
    }
}

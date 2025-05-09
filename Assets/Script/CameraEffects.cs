using System;
using System.Collections;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    // 单例
    public static CameraEffects Instance { get; private set; }
    
    private CinemachineVirtualCamera vCam;
    private CinemachineBasicMultiChannelPerlin noiseComponent;
    private float originalAmplitude;
    private float originalFrequency;
    private float baseSize; // 存储初始的镜头大小（缩放值）
    
    // 限制同时存在的 Shake 协程数量
    private int activeShakeCount = 0;
    // 记录全局的 Time.timeScale 与 Time.fixedDeltaTime（第一次调用时记录）
    private float globalOriginalTimeScale;
    private float globalOriginalFixedDeltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 如需跨场景保留该对象，可取消下面注释：
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        vCam = GetComponent<CinemachineVirtualCamera>();
        if (vCam != null)
        {
            // 保存虚拟摄像机原始镜头参数
            baseSize = vCam.m_Lens.OrthographicSize;
            // 获取 Cinemachine 噪声组件（前提是在 Inspector 中已配置 Noise Profile）
            noiseComponent = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noiseComponent != null)
            {
                originalAmplitude = noiseComponent.m_AmplitudeGain;
                originalFrequency = noiseComponent.m_FrequencyGain;
            }
        }
    }

    /// <summary>
    /// 外部调用的抖动函数
    /// </summary>
    /// <param name="shakeDuration">总抖动时长，默认 0.8 秒</param>
    /// <param name="shakeAmplitude">抖动振幅，默认 2（可根据需求调整）</param>
    /// <param name="shakeFrequency">抖动频率，默认 2</param>
    /// <param name="zoomAmount">缩小视野的数值，默认 5</param>
    public void Shake(float shakeDuration = 0.8f, float shakeAmplitude = 2f, float shakeFrequency = 2f, float zoomAmount = 5f , float timeAmount = 1f)
    {
        // 限制同时存在的 Shake 协程数量最多为 2
        if (activeShakeCount >= 2)
        {
            return;
        }

        activeShakeCount++;

        // 如果这是第一次调用，则记录全局时间参数，并应用初始抖动效果
        if (activeShakeCount == 1)
        {
            globalOriginalTimeScale = Time.timeScale;
            globalOriginalFixedDeltaTime = Time.fixedDeltaTime;

            // 调整镜头缩放，始终以 baseSize 为恢复基准，
            // 使用 math.lerp 保证平滑过渡（这里 lerp 参数可根据实际需求调整）
            vCam.m_Lens.OrthographicSize = math.lerp(vCam.m_Lens.OrthographicSize, baseSize - zoomAmount, 0.05f);

            // 应用噪声参数抖动效果
            if (noiseComponent != null)
            {
                noiseComponent.m_AmplitudeGain = shakeAmplitude;
                noiseComponent.m_FrequencyGain = shakeFrequency;
            }

            
            Time.timeScale = timeAmount;
        }

        StartCoroutine(ShakeCoroutine(shakeDuration, zoomAmount));
    }

    private IEnumerator ShakeCoroutine(float shakeDuration, float zoomAmount)
    {
        // 等待整个抖动周期，使用 WaitForSecondsRealtime 以防止时间缩放对等待时间的影响
        yield return new WaitForSecondsRealtime(shakeDuration);

        activeShakeCount--;

        // 仅当所有 Shake 协程结束后，恢复相机及全局状态
        if (activeShakeCount <= 0)
        {
            // 恢复噪声参数
            if (noiseComponent != null)
            {
                noiseComponent.m_AmplitudeGain = originalAmplitude;
                noiseComponent.m_FrequencyGain = originalFrequency;
            }
            // 恢复原始镜头缩放（即使多次缩放也始终回到最初状态）
            vCam.m_Lens.OrthographicSize = baseSize;
            // 恢复全局时间
            Time.timeScale = globalOriginalTimeScale;
            Time.fixedDeltaTime = globalOriginalFixedDeltaTime;
        }
    }
}

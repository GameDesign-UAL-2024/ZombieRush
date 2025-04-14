using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    // 单例
    public static CameraEffects Instance { get; private set; }
    
    private CinemachineVirtualCamera vCam;
    private CinemachineBasicMultiChannelPerlin noiseComponent;
    private float originalAmplitude;
    private float originalFrequency;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 如果需要跨场景保留，可以解注下面这一行
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        vCam = GetComponent<CinemachineVirtualCamera>();
        if (vCam != null)
        {
            // 获取 Cinemachine 的噪声组件
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
    /// <param name="shakeAmplitude">抖动振幅，默认 2（数值可根据需求调整）</param>
    /// <param name="shakeFrequency">抖动频率，默认 2</param>
    /// <param name="zoomAmount">缩小视野的数值，目前可以自行调整虚拟摄像机的 FOV 或是其他参数</param>
    public void Shake(float shakeDuration = 0.8f, float shakeAmplitude = 2f, float shakeFrequency = 2f, float zoomAmount = 5f)
    {
        StartCoroutine(ShakeCoroutine(shakeDuration, shakeAmplitude, shakeFrequency, zoomAmount));
    }

    private IEnumerator ShakeCoroutine(float shakeDuration, float shakeAmplitude, float shakeFrequency, float zoomAmount)
    {
        if (noiseComponent == null)
        {
            Debug.LogWarning("未找到 Cinemachine Basic Multi-Channel Perlin 组件，无法应用抖动效果。");
            yield break;
        }

        // 如果需要缩小视野，你可以考虑调整虚拟摄像机的镜头参数，例如 FOV
        float originalFOV = vCam.m_Lens.FieldOfView;
        vCam.m_Lens.FieldOfView = originalFOV - zoomAmount;

        // 应用抖动效果
        noiseComponent.m_AmplitudeGain = shakeAmplitude;
        noiseComponent.m_FrequencyGain = shakeFrequency;

        // 如果需要实现短暂的钝帧效果，可以调整 Time.timeScale
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.5f;  // 这里可以用一个默认减速因子，或者额外加入参数
       
        // 持续一半的抖动时间应用减速效果
        yield return new WaitForSecondsRealtime(shakeDuration / 2f);
        Time.timeScale = originalTimeScale;

        // 持续剩下的抖动时间
        yield return new WaitForSecondsRealtime(shakeDuration / 2f);

        // 恢复初始设置
        noiseComponent.m_AmplitudeGain = originalAmplitude;
        noiseComponent.m_FrequencyGain = originalFrequency;
        vCam.m_Lens.FieldOfView = originalFOV;
    }
}

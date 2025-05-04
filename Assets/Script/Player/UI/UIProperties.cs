using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 实时更新玩家属性 UI，并在属性变化时做提示动画：
/// 提升时绿色闪烁并放大；下降时红色闪烁并剧烈抖动。
/// 支持中断与重入，始终恢复到初始状态。
/// </summary>
public class UIProperties : MonoBehaviour
{
    [Header("属性文本引用")]
    public TextMeshProUGUI atk;
    public TextMeshProUGUI shot_speed;
    public TextMeshProUGUI atk_speed;
    public TextMeshProUGUI range;
    public TextMeshProUGUI luck;

    private PlayerController player;

    // 缓存上次属性值
    private float prevAtk, prevShotSpeed, prevAtkSpeed, prevRange, prevLuck;

    // 原始状态存储
    private struct LabelState { public Color color; public Vector3 scale; public Vector2 pos; }
    private Dictionary<TextMeshProUGUI, LabelState> originalStates = new Dictionary<TextMeshProUGUI, LabelState>();

    // 当前运行中的协程
    private Dictionary<TextMeshProUGUI, Coroutine> activeCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        // 记录原始状态，并初始化文本
        var labels = new TextMeshProUGUI[] { atk, shot_speed, atk_speed, range, luck };
        if (player != null)
        {
            var p = player.player_properties;
            prevAtk = p.damage;
            prevShotSpeed = p.bullet_speed;
            prevAtkSpeed = p.atk_speed;
            prevRange = p.bullet_exist_time;
            prevLuck = p.luck;
        }
        for (int i = 0; i < labels.Length; i++)
        {
            var lbl = labels[i];
            // 记录初始
            originalStates[lbl] = new LabelState {
                color = lbl.color,
                scale = lbl.rectTransform.localScale,
                pos   = lbl.rectTransform.anchoredPosition
            };
            // 初始化文本显示
            if (player != null)
            {
                float v = i == 0 ? prevAtk : i == 1 ? prevShotSpeed : i == 2 ? prevAtkSpeed : i == 3 ? prevRange : prevLuck;
                lbl.text = v.ToString("0.##");
            }
        }
    }

    void Update()
    {
        if (player == null) return;
        var p = player.player_properties;

        CheckAndAnimate(atk, p.damage, ref prevAtk);
        CheckAndAnimate(shot_speed, p.bullet_speed, ref prevShotSpeed);
        CheckAndAnimate(atk_speed, p.atk_speed, ref prevAtkSpeed);
        CheckAndAnimate(range, p.bullet_exist_time, ref prevRange);
        CheckAndAnimate(luck, p.luck, ref prevLuck);
    }

    private void CheckAndAnimate(TextMeshProUGUI label, float currentValue, ref float prevValue)
    {
        if (Mathf.Approximately(currentValue, prevValue))
            return;

        // 更新文本
        label.text = currentValue.ToString("0.##");

        // 中断已有动画并复位
        if (activeCoroutines.TryGetValue(label, out Coroutine running))
        {
            StopCoroutine(running);
            ResetLabel(label);
            activeCoroutines.Remove(label);
        }

        // 触发新动画
        if (currentValue > prevValue)
            activeCoroutines[label] = StartCoroutine(FlashScaleCoroutine(label, Color.green));
        else
            activeCoroutines[label] = StartCoroutine(ShakeFlashCoroutine(label, Color.red));

        prevValue = currentValue;
    }

    private void ResetLabel(TextMeshProUGUI label)
    {
        if (originalStates.TryGetValue(label, out LabelState state))
        {
            label.color = state.color;
            var rt = label.rectTransform;
            rt.localScale = state.scale;
            rt.anchoredPosition = state.pos;
        }
    }

    private IEnumerator FlashScaleCoroutine(TextMeshProUGUI label, Color flashColor)
    {
        // 获取原始状态
        LabelState state = originalStates[label];
        var rect = label.rectTransform;

        float duration = 0.5f;
        float elapsed = 0f;
        float maxScaleIncrease = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.PingPong(t * 2f, 1f);
            label.color = Color.Lerp(state.color, flashColor, alpha);
            float scale = 1f + maxScaleIncrease * Mathf.Sin(t * Mathf.PI);
            rect.localScale = state.scale * scale;
            yield return null;
        }

        // 结束，复位
        ResetLabel(label);
        activeCoroutines.Remove(label);
    }

    private IEnumerator ShakeFlashCoroutine(TextMeshProUGUI label, Color flashColor)
    {
        LabelState state = originalStates[label];
        var rect = label.rectTransform;

        float duration = 0.5f;
        float elapsed = 0f;
        float shakeAmount = 10f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.PingPong(t * 2f, 1f);
            label.color = Color.Lerp(state.color, flashColor, alpha);
            Vector2 offset = Random.insideUnitCircle * shakeAmount;
            rect.anchoredPosition = state.pos + offset;
            yield return null;
        }

        ResetLabel(label);
        activeCoroutines.Remove(label);
    }
}

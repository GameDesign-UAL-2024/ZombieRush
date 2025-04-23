using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public class TimerTextUpdater : MonoBehaviour
{
    // 获取 TextMeshProUGUI 组件（确保该组件在同一物体上）
    private TextMeshProUGUI tmpText;
    Globals global;
    [SerializeField] LocalizeStringEvent text_notice;
    void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        global = Globals.Instance;
        if(tmpText == null)
        {
            Debug.LogError("未找到 TextMeshProUGUI 组件，请确保脚本挂载在拥有该组件的物体上。");
        }
    }

    void Update()
    {
        // 从 GlobalTimer 获取当前时间（假设返回的是秒数）
        float currentTime = GlobalTimer.Instance.GetCurrentTime();
        int totalSeconds = Mathf.FloorToInt(currentTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        // 当时间不足 60 秒时，不显示分钟部分
        if (minutes > 0)
        {
            // 格式示例： "1 m 30 s"
            tmpText.text = minutes.ToString() + " m " + seconds.ToString() + " s";
        }
        else
        {
            // 仅显示秒，例如 "45 s"
            tmpText.text = seconds.ToString() + " s";
        }
        if (text_notice == null || global == null){return;}
        if (! global.Event.in_battle)
        {
            text_notice.SetEntry("NextWaveNotice");
            if(text_notice.StringReference.ContainsKey("remain_time"))
            {
                text_notice.StringReference["remain_time"] = new IntVariable{Value = global.GetSecondsUntilNextWave()};
            }
            text_notice.RefreshString();
        }
        else
        {
            text_notice.SetEntry("Battleing");
        }
    }
}

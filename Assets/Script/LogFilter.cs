using UnityEngine;

[DefaultExecutionOrder(-1000)] // 尽早初始化
public class LogFilter : MonoBehaviour
{
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 如果是你要忽略的那条报错，就不往控制台输出
        if (type == LogType.Error && logString.Contains("AABB"))
            return;

        // 其它日志照常输出
        Debug.unityLogger.Log(type, logString);
    }
}

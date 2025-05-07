using UnityEngine;
using UnityEngine.Events;

public static class GlobalEventBus
{
    // 定义全局的击中敌人事件
    public static readonly HitEnemyEvent OnHitEnemy = new HitEnemyEvent();
}

// 你的 HitEnemyEvent 定义
[System.Serializable]
public class HitEnemyEvent : UnityEvent<GameObject> {}

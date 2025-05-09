using UnityEngine;
using UnityEngine.Events;

public static class GlobalEventBus
{
    // 定义全局的击中敌人事件
    public static readonly HitEnemyEvent OnHitEnemy = new HitEnemyEvent();
    public static readonly ResourceCollectEvent OnResourceCollect = new ResourceCollectEvent();
    public static readonly GridInteractedEvent OnGridInteracted= new GridInteractedEvent();
    public static readonly EnemyDeadEvent OnEnemyDead = new EnemyDeadEvent();
}

// 你的 HitEnemyEvent 定义
[System.Serializable]
public class HitEnemyEvent : UnityEvent<GameObject> {}
public class EnemyDeadEvent : UnityEvent {}
public class ResourceCollectEvent : UnityEvent<ResourcePickup> {}
public class GridInteractedEvent : UnityEvent<InteractableGrids> {}

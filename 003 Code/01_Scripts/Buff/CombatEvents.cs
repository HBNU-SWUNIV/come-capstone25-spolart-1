using System;
using UnityEngine;

// 전투 중 발생하는 이벤트를 위한 전역 클래스

public static class CombatEvents
{
    // 몬스터가 사망했을 때 호출 : (killer, victim)
    public static event Action<GameObject, GameObject> OnEnemyKilled;

    public static void RaiseEnemyKilled(GameObject killder, GameObject victim)
        => OnEnemyKilled?.Invoke(victim, killder);

    // 던전에 입장했을 때 호출
    public static event Action OnDungeonEntered;
    public static void RaiseDungeonEntered() => OnDungeonEntered?.Invoke();

    // 던전을 떠날 때 호출
    public static event Action OnDungeonExited;
    public static void RaiseDungenExited() => OnDungeonExited?.Invoke();
}

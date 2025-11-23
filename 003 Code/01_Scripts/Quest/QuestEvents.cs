using System;
using UnityEngine;

/// <summary>
/// 몬스터 처치/ 광물 채광 등을 들을 수 있는 이벤트 훅 스크립트
/// </summary>

public static class QuestEvents
{
    // 몬스터 처치 보고 (monsterId, count)
    public static event Action<string, int> OnMonsterKilled;

    // 광물 획득 보고 (oreId, count)
    public static event Action<string, int> OnOreAcquired;

    // 튜토리얼 트리거 동작 보고
    public static event Action<string> OnFlagRaised;

    public static void ReportMonsterKill(string monsterId, int count = 1)
        => OnMonsterKilled?.Invoke(monsterId, Math.Max(1, count));

    public static void ReportOreAcquired(string oreId, int count = 1)
        => OnOreAcquired?.Invoke(oreId, Math.Max(1, count));

    public static void RaiseFlag(string flagId) => OnFlagRaised?.Invoke(flagId);
}

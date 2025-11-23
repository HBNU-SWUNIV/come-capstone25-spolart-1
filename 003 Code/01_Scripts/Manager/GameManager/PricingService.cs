using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PricingService", menuName = "Game/PricingService")]
public class PricingService : ScriptableObject
{
    [Header("던전 진행 보정치")]
    [Tooltip("층수당 보너스 ex) 0.02 = 2%")]
    [SerializeField] private float floorBonusPerFloor = 0f;

    [Tooltip("기본 판매가에 곱할 배율 (수수료 등)")]
    [SerializeField] private float globalMultiplier = 1f;

    public int EvaluateTotal(IReadOnlyList<RunInventory.Entry> stacks, int floorCleared = 0)
    {
        // 층 보너스 = 기본값 1 + ( 클리어한 층 수 * 층수당 보너스 배율)
        float floorBonus = 1f + Mathf.Max(0f, floorCleared) * Mathf.Max(0f, floorBonusPerFloor);
        float total = 0f;

        foreach (var e in stacks)
        {
            if (e.ore == null || e.count <= 0)
            {
                continue;
            }

            int unit = Mathf.Max(0, e.ore.Price); // 광물 가격
            total += e.count * unit * globalMultiplier * floorBonus; // 개수 * 광물 가격 * 배율 * 층별 보너스
        }
        // 결과값은 정수로 변환해 반환
        return Mathf.RoundToInt(total);
    }
}

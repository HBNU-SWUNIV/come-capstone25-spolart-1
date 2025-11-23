using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeTable", menuName = "Game/Upgrade Table")]
public class UpgradeTable : ScriptableObject
{
    [Serializable]
    public class RarityRule
    {
        public EquipmentRarity rarity;

        [Header("강화 한도 및 비용")]
        [Min(0)] public int maxLevel = 10;
        [Min(0)] public int baseCost = 100;
        [Min(1)] public float costMultiplier = 1.5f;

        [Header("공격력 증가")]
        public int attackFlatPerLevel = 0;

        [Header("강화 확률")]
        [Range(0f, 1f)]
        public float[] successRates;
    }

    [SerializeField] private RarityRule[] rules;

    public RarityRule GetRule(EquipmentRarity rarity)
    {
        foreach (var rule in rules)
        {
            if (rule.rarity == rarity)
            {
                return rule;
            }
        }
        return null;
    }

    public int MaxLevel(EquipmentData eq) => GetRule(eq.Rarity)?.maxLevel ?? 0;

    public int CostForNextLevel(EquipmentData eq, int currentLevel)
    {
        var r = GetRule(eq.Rarity);
        if (r == null) return int.MaxValue;
        double step = r.baseCost * System.Math.Pow(r.costMultiplier, currentLevel);
        return Mathf.RoundToInt((float)step);
    }

    public int AttackBonus(EquipmentData eq, int level)
    {
        var r = GetRule(eq.Rarity);
        if (r == null || level <= 0) return 0;

        if (r.attackFlatPerLevel != 0)
        {
            int sum = 0;
            for (int i = 0; i <= level; i++)
            {
                sum += r.attackFlatPerLevel;
            }
            return sum;
        }

        return r.attackFlatPerLevel * level;
    }

    public float GetSuccessRate(EquipmentData eq, int level)
    {
        var r = GetRule(eq.Rarity);
        if (r == null || r.successRates == null || r.successRates.Length <= level) return 1f;
        return Mathf.Clamp01(r.successRates[level]);
    }
}

using UnityEngine;

/* 플레이어가 들고 있는 장비 상태 */

public class EquipmentItem
{
    public EquipmentData data;
    public int currentDurability;

    public EquipmentItem(EquipmentData data)
    {
        this.data = data;
        this.currentDurability = data.MaxDurability;
    }

    public EquipmentItem(EquipmentData data, int currentDurability)
    {
        this.data = data;
        this.currentDurability = Mathf.Clamp(currentDurability, 0, data.MaxDurability);
    }

    public string Name => data.EquipmentName;
    public Sprite Icon => data.Icon;
    public EquipmentType Type => data.Type;
    public EquipmentRarity Rarity => data.Rarity;
    public int AttackDamage => data.BaseAttackDamage;
    public float Speed => data.Speed;
    public float CriticalChance => data.CriticalChance;
    
    // 내구도 관련
    public int CurrentDurability => currentDurability;
    public int MaxDurability => data.MaxDurability;
    public bool IsBroken => currentDurability <= 0; // 내구도가 0 이하면 고장

    // 내구도 감소
    public void ReduceDurability(int amount = 1)
    {
        currentDurability = Mathf.Max(0, currentDurability - amount);
    }

    // 내구도를 최대로 수리
    public void Repair()
    {
        currentDurability = MaxDurability;
    }
}

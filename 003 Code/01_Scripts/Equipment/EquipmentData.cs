using UnityEngine;

public enum EquipmentType { Mining, Weapon }
public enum EquipmentRarity { Common, Uncommon, Rare, Unique, Epic, Legendary, Mythic }

[CreateAssetMenu(fileName = "EquipmentData", menuName = "Game/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("식별자")]
    [SerializeField] private string equipmentId;
    public string Id => string.IsNullOrEmpty(equipmentId) ? name : equipmentId;

    [Header("기본 정보")]
    [SerializeField] private string equipmentName;
    public string EquipmentName => equipmentName;

    [SerializeField] private Sprite icon;
    public Sprite Icon => icon;

    [SerializeField] private EquipmentType type;
    public EquipmentType Type => type;

    [SerializeField] private EquipmentRarity rarity;
    public EquipmentRarity Rarity => rarity;

    [SerializeField] private int equipmentUpgrade;
    public int EquipmentUpgrade => equipmentUpgrade;

    [Header("능력치")]
    [SerializeField] private int baseAttackDamage;
    public int BaseAttackDamage => baseAttackDamage;

    [SerializeField] float speed;
    public float Speed => speed;

    [SerializeField, Range(0f, 1f)] private float criticalChance;
    public float CriticalChance => criticalChance;

    [Header("내구도")]
    [SerializeField] private int maxDurability;
    public int MaxDurability => maxDurability;

    // 강화 레벨 적용 및 반영
    public void ApplyLoadedUpgrade(int level)
    {
        equipmentUpgrade = Mathf.Max(0, level);
    }

    // 계산된 공격력 반영
    public int CalculatedAttackDamage
    {
        get
        {
            if (EnhancementService.Instance == null) return baseAttackDamage;
            int bonus = EnhancementService.Instance.AttackBonusFor(this, equipmentUpgrade);
            return baseAttackDamage + bonus;
        }
    }

}

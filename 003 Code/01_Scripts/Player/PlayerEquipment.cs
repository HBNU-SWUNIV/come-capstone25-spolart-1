using System;
using JetBrains.Annotations;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [Header("현재 착용 중인 장비")]
    [SerializeField] private EquipmentItem currentWeapon;
    [SerializeField] private EquipmentItem currentMiningTool;

    [Header("기본 능력치")]
    [SerializeField] private int baseAttackPower = 1;
    [SerializeField] private float baseAttackCooldown = 0.5f;
    [SerializeField] private float baseAttackCriticalChance = 0f;

    [SerializeField] private int baseMiningPower = 1;
    [SerializeField] private float baseMiningCooldown = 0.5f;
    [SerializeField] private float baseMiningCriticalChance = 0f;

    public event Action OnEquipmentChanged;

    public EquipmentItem CurrentWeapon => currentWeapon;
    public EquipmentItem CurrentMiningTool => currentMiningTool;

    private void Awake()
    {
        // 내 장비가 바뀌면 항상 DataManager에 저장하도록 자동 훅
        OnEquipmentChanged += AutoSaveToDataManager;
    }

    private void Start()
    {
        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.ApplyEquipmentTo(this);
        }
    }

    private void OnDestroy()
    {
        OnEquipmentChanged -= AutoSaveToDataManager;
    }

    public void Equip(EquipmentData data)
    {
        if (data == null)
        {
            return;
        }
        var item = new EquipmentItem(data); // 매개변수로 전달받은 장비 데이터를 통해 EquipmentItem 생성

        // 해당 데이터의 타입에 따라 현재 무기/채광도구 설정
        if (data.Type == EquipmentType.Weapon)
        {
            currentWeapon = item;
            Debug.Log("current item: " + data.Type);
        }
        else
        {
            currentMiningTool = item;
            Debug.Log("current item: " + data.Type);
        }

        OnEquipmentChanged?.Invoke();

        // 장비 장착 시 바로 저장
        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.SaveEquipmentsFrom(this);
        }
    }

    public void Unequip(EquipmentType type)
    {
        if (type == EquipmentType.Weapon)
        {
            currentWeapon = null;
        }
        else
        {
            currentMiningTool = null;
        }

        OnEquipmentChanged?.Invoke();

        // 장비 해제 시 바로 저장
        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.SaveEquipmentsFrom(this);
        }
    }

    // 무기 사용 시 내구도 소모
    public void UseWeapon()
    {
        if (currentWeapon != null && !currentWeapon.IsBroken)
        {
            currentWeapon.ReduceDurability(1);
            OnEquipmentChanged?.Invoke();
        }
    }

    public void UseMiningTool()
    {
        if (currentMiningTool != null && !currentMiningTool.IsBroken)
        {
            currentMiningTool.ReduceDurability(1);
            OnEquipmentChanged?.Invoke();
        }
    }

    // === 공격 관련 ===
    public int GetAttackPower()
    {
        // 내구도가 0일때
        int weaponDamage = (currentWeapon != null && !currentWeapon.IsBroken)
            ? currentWeapon.AttackDamage
            : 0;
        return weaponDamage + baseAttackPower;
    }

    public float GetAttackCooldown()
    {
        float speed = currentWeapon?.Speed ?? 1f;
        return baseAttackCooldown / MathF.Max(0.01f, speed);
    }

    public float GetAttackCriticalChance()
    {
        // 내구도가 0 이하라면 치명타 확률 0%
        float chance = (currentWeapon != null && !currentWeapon.IsBroken)
            ? currentWeapon.CriticalChance
            : 0f;
        return Mathf.Clamp01(chance + baseAttackCriticalChance);
    }

    // --- 채광 관련

    public int GetMiningPower()
    {
        // 내구도가 0일때
        int toolDamage = (currentMiningTool != null && !currentMiningTool.IsBroken)
            ? currentMiningTool.AttackDamage
            : 0;
        return toolDamage + baseMiningPower;
    }

    public float GetMiningCooldown()
    {
        float speed = currentMiningTool?.Speed ?? 1f;
        return baseMiningCooldown / MathF.Max(0.01f, speed);
    }
    public float GetMiningCriticalChance()
    {
        // 내구도가 0일때
        float t = (currentMiningTool != null && !currentMiningTool.IsBroken)
            ? currentMiningTool.CriticalChance
            : 0f;
        float b = baseMiningCriticalChance;

        if (t > 1f) t *= 0.01f;
        if (b > 1f) b *= 0.01f;

        return Mathf.Clamp01(t + b);
    }

    public void LoadEquipped(
        EquipmentData weaponData, int weaponDurability,
        EquipmentData miningData, int miningDurability)
    {

        // 전달받은 데이터로 내구도를 반영해 장비 생성
        currentWeapon = weaponData != null ? new EquipmentItem(weaponData, weaponDurability) : null;
        currentMiningTool = miningData != null ? new EquipmentItem(miningData, miningDurability) : null;

        OnEquipmentChanged?.Invoke();
    }

    // 수리 비용 계산
    public bool GetRepairCost(EquipmentType type, out int cost)
    {
        cost = 0;
        EquipmentItem itemToRepair = (type == EquipmentType.Weapon) ? currentWeapon : currentMiningTool;

        if (itemToRepair == null) return false; // 장착한 아이템 없음

        // 최대 내구도에서 감소된 현재 내구도
        int durabilityMissing = itemToRepair.MaxDurability - itemToRepair.CurrentDurability;
        if (durabilityMissing == 0) return false;

        cost = CalculateRepairCost(itemToRepair, durabilityMissing);
        return true;
    }

    // 수리 시도
    public bool RepairItem(EquipmentType type, out int cost)
    {
        cost = 0;
        EquipmentItem itemToRepair = (type == EquipmentType.Weapon) ? currentWeapon : currentMiningTool;

        if (itemToRepair == null) return false; // 장착한 아이템 없음

        int durabilityMissing = itemToRepair.MaxDurability - itemToRepair.CurrentDurability;
        if (durabilityMissing == 0) return false; // 이미 내구도 최대

        cost = CalculateRepairCost(itemToRepair, durabilityMissing);

        if (!EconomyServiceLocator.TryGetMoney(out int currentMoney) || currentMoney < cost)
        {
            return false; // 돈 부족
        }

        var eco = EconomyService.Instance;
        if (eco != null)
        {
            eco.TrySpendMoney(cost); // 골드 차감 시도
            itemToRepair.Repair();    // 아이템 수리
            OnEquipmentChanged?.Invoke(); // 장비 상태 변경 알림
            return true;
        }

        return false; // EconomyService 없음
    }

    private int CalculateRepairCost(EquipmentItem item, int durabilityMissing)
    {
        int rarityCostPerPoint;

        switch (item.Rarity)
        {
            case EquipmentRarity.Common:
                rarityCostPerPoint = 1;
                break;
            case EquipmentRarity.Uncommon:
                rarityCostPerPoint = 2;
                break;
            case EquipmentRarity.Rare:
                rarityCostPerPoint = 3;
                break;
            case EquipmentRarity.Unique:
                rarityCostPerPoint = 5;
                break;
            case EquipmentRarity.Epic:
                rarityCostPerPoint = 7;
                break;
            case EquipmentRarity.Legendary:
                rarityCostPerPoint = 10;
                break;
            case EquipmentRarity.Mythic:
                rarityCostPerPoint = 20;
                break;
            default:
                rarityCostPerPoint = 1;
                break;
        }

        return durabilityMissing * rarityCostPerPoint;
    }

    private void AutoSaveToDataManager()
    {
        var dm = DataManager.Instance;
        if (dm != null)
        {
            dm.SaveEquipmentsFrom(this);
        }
    }
}

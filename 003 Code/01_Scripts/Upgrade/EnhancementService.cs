using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class EnhancementService : MonoBehaviour
{
    public static EnhancementService Instance { get; private set; }

    [Header("강화 규칙")]
    [SerializeField] private UpgradeTable upgradeTable;
    [SerializeField] private string saveFileName = "equipment_upgrade.json";

    private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

    [Serializable] private class SaveItem { public string id; public int lv; }
    [Serializable] private class SaveRoot { public List<SaveItem> items = new List<SaveItem>(); }

    private SaveRoot _root = new SaveRoot();
    private string SavePath => Path.Combine(Application.dataPath, saveFileName);

    public event Action<EquipmentData, int> OnEnhanced;

    // 시설 ID 상수
    private const string EquipmentCostDiscountId = "BS001";
    private const string EquipmentMaxLvIncreaseId = "BS002";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFromDisk();
    }

    public int GetLevel(EquipmentData eq) => eq == null ? 0 : (_levels.TryGetValue(eq.Id, out var lv) ? lv : 0);

    public int AttackBonusFor(EquipmentData eq, int level) => upgradeTable != null ? upgradeTable.AttackBonus(eq, level) : 0;

    // --------------------------------------------------------------------------
    // BS001 할인 로직
    // --------------------------------------------------------------------------

    public int NextCost(EquipmentData eq)
    {
        int baseCost = upgradeTable != null ? upgradeTable.CostForNextLevel(eq, GetLevel(eq)) : int.MaxValue;
        if (baseCost == int.MaxValue) return int.MaxValue;

        int discountLevel = DataManager.Instance != null
            ? DataManager.Instance.GetFacilityLevel(EquipmentCostDiscountId)
            : 0;
        float discountRate = Mathf.Clamp(discountLevel * 5 / 100f, 0f, 0.45f);

        float finalCostFloat = baseCost * (1.0f - discountRate);
        return Mathf.FloorToInt(finalCostFloat);
    }

    // --------------------------------------------------------------------------
    // BS002 MaxLv 제한 로직
    // --------------------------------------------------------------------------

    private int GetEquipmentMaxLevelLimit()
    {
        int bs002Level = DataManager.Instance != null
            ? DataManager.Instance.GetFacilityLevel(EquipmentMaxLvIncreaseId)
            : 0;
        return bs002Level switch
        {
            0 => 0,
            1 => 4,
            2 => 8,
            3 => 12,
            _ => 12
        };
    }

    public int GetMaxLevel(EquipmentData eq)
    {
        int equipmentBaseMaxLv = upgradeTable != null ? upgradeTable.MaxLevel(eq) : 0;
        int bs002Limit = GetEquipmentMaxLevelLimit();
        return Mathf.Min(equipmentBaseMaxLv, bs002Limit);
    }

    // --------------------------------------------------------------------------

    public bool TryEnhance(EquipmentData eq, out int newLevel, out string reason)
    {
        newLevel = GetLevel(eq);
        if (eq == null || upgradeTable == null)
        {
            reason = "설정 없음";
            return false;
        }

        int maxLv = GetMaxLevel(eq);
        if (newLevel >= maxLv)
        {
            // BS002 제한에 걸렸는지, 아니면 장비 자체의 MAX 레벨에 걸렸는지 판단
            reason = (maxLv == GetEquipmentMaxLevelLimit()) ? "BS002 제한" : "최대 강화";
            return false;
        }

        int cost = NextCost(eq);
        if (EconomyService.Instance == null || !EconomyService.Instance.TrySpendMoney(cost))
        {
            reason = "골드 부족";
            return false;
        }

        // 확률 체크
        float rate = upgradeTable.GetSuccessRate(eq, newLevel);
        Debug.Log(newLevel + "레벨 성공확률: " + rate);
        if (UnityEngine.Random.value > rate)
        {
            Debug.Log("강화 실패");
            reason = "강화 실패";
            return false;
        }

        // 성공 시
        newLevel++;
        _levels[eq.Id] = newLevel;

        // SO에 강화 정보 적용
        eq.ApplyLoadedUpgrade(newLevel);

        // 저장 및 이벤트
        FlushToRootAndSave();
        OnEnhanced?.Invoke(eq, newLevel);
        reason = null;
        return true;
    }

    // 초기화 시 모든 SO 레벨 데이터 초기화
    public void ApplyAllTo(params EquipmentData[] allEquipments)
    {
        if (allEquipments == null) return;
        foreach (var eq in allEquipments)
        {
            if (eq == null) continue;
            int lv = GetLevel(eq);
            eq.ApplyLoadedUpgrade(lv);
        }
    }

    // 저장 및 로드
    private void LoadFromDisk()
    {
        _levels.Clear();
        try
        {
            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath);
                _root = JsonUtility.FromJson<SaveRoot>(json) ?? new SaveRoot();
            }
            else _root = new SaveRoot();

            foreach (var it in _root.items)
                if (!string.IsNullOrEmpty(it.id))
                    _levels[it.id] = Mathf.Max(0, it.lv);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EnhancementService] Load fail: {e.Message}");
            _root = new SaveRoot();
        }
    }

    private void FlushToRootAndSave()
    {
        _root.items.Clear();
        foreach (var kv in _levels)
            _root.items.Add(new SaveItem { id = kv.Key, lv = kv.Value });

        try
        {
            var json = JsonUtility.ToJson(_root, true);
            File.WriteAllText(SavePath, json);
#if UNITY_EDITOR
            Debug.Log($"[EnhancementService] Saved: {SavePath}");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[EnhancementService] Save fail: {e.Message}");
        }
    }

    private void OnApplicationQuit() => FlushToRootAndSave();
}
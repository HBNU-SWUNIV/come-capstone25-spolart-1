using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

[Serializable]
public class SkillLevelKV
{
    public string id;
    public int level;
}

[Serializable]
public class FacilityLevelKV
{
    public string id; // FacilityId
    public int level;
}

[Serializable]
public class EquipmentStateKV
{
    public string id;
    public int durability;
}

[System.Serializable]
public class QuestSave
{
    public string questId;
    public int progress;
    public bool completed;   // 클리어 조건 달성(수주자에게 보고 필요)
}

[Serializable]
public class SaveData
{
    public int money = 0;

    // 해금된 장비 ID 목록
    public List<string> unlockedEquipments = new();

    // 장비별 현재 내구도 정보
    public List<EquipmentStateKV> equipmentStates = new();

    // 해금된 스킬 ID 목록
    public List<string> unlockedSkills = new();

    // 스킬 레벨(직렬화 가능 형태)
    public List<SkillLevelKV> skillLevels = new();

    // 시설 레벨
    public List<FacilityLevelKV> facilityLevels = new();

    // 장착 슬롯(스킬 ID)
    public string slot1Id;
    public string slot2Id;

    public List<string> runBuffBasket = new();

    // 퀘스트
    public bool tutorialCompleted = false;
    public int tutorialStep = 0;
    public List<QuestSave> activeQuest = new(); // 현재 수주 중인 퀘스트 (튜토리얼/반복 포함)

    // 장비
    public string equippedWeaponId;
    public int equippedWeaponDurability;
    public string equippedMiningToolId;
    public int equippedMiningToolDurability;
}

[DefaultExecutionOrder(-100)]
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("카탈로그 (모든 장비 및 스킬SO 등록)")]
    [SerializeField] private EquipmentData[] equipmentCatalog;
    [SerializeField] private SkillData[] skillCatalog;
    [SerializeField] private QuestData[] questCatalog;

    // id → EquipmentData/SkillData
    private readonly Dictionary<string, EquipmentData> _equipIndex = new();
    private readonly Dictionary<string, SkillData> _skillIndex = new();

    // id → skill level (런타임 캐시; 파일 저장 전/후 List로 변환)
    private readonly Dictionary<string, int> _levelMap = new();

    // 시설 레벨 (런타임 캐시)
    private readonly Dictionary<string, int> _facilityLevelMap = new();

    // 장비 내구도 캐시
    private readonly Dictionary<string, int> _equipmentDurabilityMap = new();

    private string _path;
    private SaveData cache = new SaveData();

    private bool _hasLoaded = false;
    private bool _isLoading = false;

    // 시설 레벨 1 시작 시설 ID 상수 정의
    private const string FacilityMaxLvIncreaseId = "GD002";
    private const string MineCartFacilityId = "GD003";
    private const string BuffUnlockFacilityId = "TP002";
    private const string BuffSlotFacilityId = "TP003";
    private const string EquipmentMaxLvIncreaseId = "BS002";
    private const string RarityUnlockFacilityId = "BS003";

    public event Action OnFacilityLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 추후 배포 시 Application.persistentDataPath로 수정
        // 편의를 위해 dataPath 사용
        _path = Path.Combine(Application.persistentDataPath, "database.json");

        BuildIndex();
        JsonLoad();
        ValidateCatalog();
    }

    #region === 유틸 ===
    private void BuildIndex()
    {
        _skillIndex.Clear();
        if (skillCatalog != null)
            foreach (var s in skillCatalog)
                if (s && !string.IsNullOrEmpty(s.skillId)) _skillIndex[s.skillId] = s;

        _equipIndex.Clear();
        if (equipmentCatalog != null)
            foreach (var e in equipmentCatalog)
                if (e && !string.IsNullOrEmpty(e.Id)) _equipIndex[e.Id] = e;
    }

    private void ValidateCatalog()
    {
        // 중복 skillId 탐지 + 아이콘 누락 경고
        var seen = new HashSet<string>();
        foreach (var s in skillCatalog)
        {
            if (!s) continue;
            if (string.IsNullOrEmpty(s.skillId))
                Debug.LogWarning($"[DataManager] SkillData '{s.name}' has empty skillId.");

            if (!seen.Add(s.skillId))
                Debug.LogError($"[DataManager] Duplicate skillId detected: '{s.skillId}'.");

            if (s.skillIcon == null)
                Debug.LogWarning($"[DataManager] Skill '{s.skillId}' has no icon assigned.");
        }
    }
    #endregion

    #region JSON Load / Save
    public void JsonLoad()
    {
        // 이미 로딩했거나, 로딩 중이면 다시 들어가지 않음
        if (_hasLoaded || _isLoading) return;

        _isLoading = true;
        try
        {
            // 1) 파일이 없으면 새로 생성
            if (!File.Exists(_path))
            {
                Debug.Log("[DataManager] database.json을 찾지 못함. 새로 생성");
                cache = new SaveData();
                cache.money = 0;

                // 시설 기본 레벨 세팅
                InitializeLevelOneFacilities();

                // List 형식 맞춰두기
                PushLevelMapToList();
                PushFacilityLevelMapToList();

                // 시설 레벨 기준 장비 해금(저장은 나중에)
                RefreshEquipmentUnlockByFacilities(autoSave: false);

                _hasLoaded = true;
                _isLoading = false;    // 저장 전에 로딩 플래그 해제

                // 실제 파일 생성
                JsonSave();
                return;
            }

            // 2) 파일이 있으면 읽어서 복구
            string loadJson = File.ReadAllText(_path);
            cache = JsonUtility.FromJson<SaveData>(loadJson) ?? new SaveData();

            // List → Dictionary 재구성
            PullLevelListToMap();
            PullFacilityLevelListToMap();
            PullEquipmentStatesToMap();

            // 시설 레벨 기준으로 해금 장비 동기화 (자동 저장은 끔)
            RefreshEquipmentUnlockByFacilities(autoSave: false);

            // 돈 복구
            var eco = EconomyServiceInstance();
            if (eco != null)
            {
                eco.SetMoney(cache.money);
                Debug.Log($"[DataManager] database.json 복구 성공! 현재 소지 금액: {eco.Money}");
            }

            _hasLoaded = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] JsonLoad 중 예외: {e}");

            // 문제가 생기면 새 파일로 초기화
            cache = new SaveData();
            cache.money = 0;

            InitializeLevelOneFacilities();
            PushLevelMapToList();
            PushFacilityLevelMapToList();

            RefreshEquipmentUnlockByFacilities(autoSave: false);

            _hasLoaded = true;
            _isLoading = false;   // 저장 전에 플래그 해제
            JsonSave();
            return;
        }
        finally
        {
            _isLoading = false;
        }
    }



    public void JsonSave()
    {
        if (_isLoading)
        {
            return;
        }

        if (!_hasLoaded)
        {
            Debug.LogWarning("[DataManager] JsonSave 호출 시 아직 JsonLoad가 안 되어 있어, 먼저 JsonLoad를 실행합니다.");
            JsonLoad();
        }

        // Dictionary → List 반영
        PushLevelMapToList();
        PushFacilityLevelMapToList();
        PushEquipmentMapToList();

        var eco = EconomyServiceInstance();
        if (eco != null)
        {
            // 캐시에 런타임 중 보유금액 저장
            cache.money = eco.Money;
        }

        try
        {
            string json = JsonUtility.ToJson(cache, true);
            File.WriteAllText(_path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] JsonSave 실패: {e.Message}");
        }
    }

    public void SaveNow() => JsonSave();

    private EconomyService EconomyServiceInstance()
    {
        return EconomyService.Instance ?? FindObjectOfType<EconomyService>();
    }

    private void PullLevelListToMap()
    {
        _levelMap.Clear();
        if (cache.skillLevels == null) return;

        foreach (var kv in cache.skillLevels)
        {
            if (kv == null || string.IsNullOrEmpty(kv.id)) continue;
            _levelMap[kv.id] = kv.level;
        }
    }

    private void PushLevelMapToList()
    {
        if (cache.skillLevels == null) cache.skillLevels = new List<SkillLevelKV>();
        cache.skillLevels.Clear();

        foreach (var pair in _levelMap)
        {
            cache.skillLevels.Add(new SkillLevelKV { id = pair.Key, level = pair.Value });
        }
    }

    #endregion

    #region 장비 해금/장착/조회

    public bool IsEquipmentUnlocked(EquipmentData data)
    {
        return data != null && cache.unlockedEquipments.Contains(data.Id);
    }

    public bool UnlockEquipment(EquipmentData data)
    {
        if (data == null) return false;
        if (IsEquipmentUnlocked(data)) return false;

        cache.unlockedEquipments.Add(data.Id);

        // 처음 해금될 때 현재 내구도를 풀로 세팅
        _equipmentDurabilityMap[data.Id] = data.MaxDurability;

        JsonSave();
        return true;
    }

    public EquipmentData GetEquipmentById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _equipIndex.TryGetValue(id, out var d) ? d : null;
    }

    public void ApplyEquipmentTo(PlayerEquipment pe)
    {
        if (!pe) return;

        var weaponData = GetEquipmentById(cache.equippedWeaponId);
        var miningData = GetEquipmentById(cache.equippedMiningToolId);

        pe.LoadEquipped(
            weaponData, 
            cache.equippedWeaponDurability, 
            miningData, 
            cache.equippedMiningToolDurability
        );
    }

    public void SaveEquipmentsFrom(PlayerEquipment pe)
    {
        if (!pe) return;

        var weapon = pe.CurrentWeapon;
        var tool = pe.CurrentMiningTool;

        cache.equippedWeaponId = weapon?.data?.Id;
        cache.equippedWeaponDurability = weapon?.CurrentDurability ?? 0;

        cache.equippedMiningToolId = tool?.data?.Id;
        cache.equippedMiningToolDurability = tool?.CurrentDurability ?? 0;

        if (weapon != null && weapon.data != null)
            _equipmentDurabilityMap[weapon.data.Id] = weapon.CurrentDurability;

        if (tool != null && tool.data != null)
            _equipmentDurabilityMap[tool.data.Id] = tool.CurrentDurability;

        var qm = QuestManager.Instance;
        if (qm != null)
        {
            cache.activeQuest = qm.Active.Values.ToList();
            cache.tutorialStep = GetTutorialStep();
            // cache.tutorialCompleted = qm.IsTutorialCompleted;
        }

        JsonSave();
    }

    // 시설 레벨에 따른 장비 희귀도 해금 한계를 리턴
    private int GetMaxUnlockedRarityIndex()
    {
        int bs003Level = GetFacilityLevel(RarityUnlockFacilityId);

        int maxRarityIndex = bs003Level switch
        {
            1 => 0, // Common만
            2 => 1,
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 5,
            7 => 6,
            8 => 7,
            _ => 7
        };

        return Mathf.Clamp(maxRarityIndex, 0, (int)EquipmentRarity.Mythic);
    }

    // 현재 시설 레벨을 기준으로 해금 가능한 장비를 모두 database에 반영
    public void RefreshEquipmentUnlockByFacilities(bool autoSave = true)
    {
        if (equipmentCatalog == null) return;
        if (cache.unlockedEquipments == null)
            cache.unlockedEquipments = new List<string>();

        int maxRarityIndex = GetMaxUnlockedRarityIndex();
        bool changed = false;

        foreach (var eq in equipmentCatalog)
        {
            if (eq == null) continue;
            if ((int)eq.Rarity > maxRarityIndex) continue;

            bool newlyUnlocked = false;

            if (!cache.unlockedEquipments.Contains(eq.Id))
            {
                cache.unlockedEquipments.Add(eq.Id);
                newlyUnlocked = true;
            }

            // 내구도 맵에 없으면 기본값으로 추가
            if (!_equipmentDurabilityMap.ContainsKey(eq.Id))
            {
                _equipmentDurabilityMap[eq.Id] = eq.MaxDurability;
                newlyUnlocked = true;
            }

            if (newlyUnlocked) changed = true;
        }

        if (changed && autoSave && !_isLoading)
            JsonSave();
    }


    #endregion

    #region 장비 내구도 맵
    private void PullEquipmentStatesToMap()
    {
        _equipmentDurabilityMap.Clear();
        if (cache.equipmentStates == null) return;

        foreach (var kv in cache.equipmentStates)
        {
            if (kv == null || string.IsNullOrEmpty(kv.id)) continue;
            _equipmentDurabilityMap[kv.id] = kv.durability;
        }
    }

    private void PushEquipmentMapToList()
    {
        if (cache.equipmentStates == null) cache.equipmentStates = new List<EquipmentStateKV>();
        cache.equipmentStates.Clear();

        foreach (var pair in _equipmentDurabilityMap)
        {
            cache.equipmentStates.Add(new EquipmentStateKV
            {
                id = pair.Key,
                durability = pair.Value,
            });
        }
    }

    public int GetEquipmentDurability(EquipmentData data)
    {
        if (data == null) return 0;

        // 저장된 값이 있으면 값 사용, 없으면 최대 내구도로 간주
        if (_equipmentDurabilityMap.TryGetValue(data.Id, out var dur)) 
        {
            return dur;
        }
        return data.MaxDurability;
    }

    public void SetEqiupmentDurability(EquipmentData data, int durability)
    {
        if (data == null) return;

        int clamped = Mathf.Clamp(durability, 0, data.MaxDurability);
        _equipmentDurabilityMap[data.Id] = clamped;

        if (!_isLoading)
        {
            JsonSave();
        }
    }
    #endregion

    #region 스킬 해금/장착/조회

    public bool IsSkillUnlocked(SkillData skillData)
    {
        return skillData != null && cache.unlockedSkills.Contains(skillData.skillId);
    }

    public bool UnlockSkill(SkillData skillData)
    {
        if (skillData == null) return false;
        if (IsSkillUnlocked(skillData)) return false;

        cache.unlockedSkills.Add(skillData.skillId);

        // 기본 레벨 보장
        if (!_levelMap.ContainsKey(skillData.skillId))
            _levelMap[skillData.skillId] = 1;

        JsonSave();
        return true;
    }

    public void LockSkill(SkillData skillData)
    {
        if (skillData == null) return;

        if (cache.unlockedSkills.Remove(skillData.skillId))
        {
            // 장착 해제
            if (cache.slot1Id == skillData.skillId) cache.slot1Id = null;
            if (cache.slot2Id == skillData.skillId) cache.slot2Id = null;

            // 레벨 삭제(원하면 유지 가능)
            _levelMap.Remove(skillData.skillId);

            JsonSave();
        }
    }

    public void ApplySkillsTo(PlayerSkillSystem pss)
    {
        if (!pss) return;
        pss.slot1 = GetSkillById(cache.slot1Id);
        pss.slot2 = GetSkillById(cache.slot2Id);
        // 필요 시 HUD/이벤트 알림 호출
    }

    public void SaveSkillsFrom(PlayerSkillSystem pss)
    {
        if (!pss) return;
        cache.slot1Id = pss.slot1 ? pss.slot1.skillId : null;
        cache.slot2Id = pss.slot2 ? pss.slot2.skillId : null;
        JsonSave();
    }

    public SkillData GetSkillById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_skillIndex.TryGetValue(id, out var data)) return data;

        Debug.LogWarning($"[DataManager] Unknown SkillId '{id}'. " +
                         $"Check: SaveData(slot ids/unlocked list) ↔ skillCatalog(skillId).");
        return null;
    }

    #endregion

    #region 스킬 레벨

    public int GetSkillLevel(SkillData data)
    {
        if (data == null) return 0;
        if (!IsSkillUnlocked(data)) return 0;

        if (_levelMap.TryGetValue(data.skillId, out var lv))
            return Mathf.Clamp(lv, 1, data.maxLevel);

        // 해금되어 있는데 레벨 엔트리가 없으면 1로 고정
        _levelMap[data.skillId] = 1;
        JsonSave();
        return 1;
    }

    public void SetSkillLevel(SkillData data, int level)
    {
        if (data == null) return;

        int clamped = Mathf.Clamp(level, 1, data.maxLevel);
        _levelMap[data.skillId] = clamped;
        JsonSave();
    }

    /// <summary>
    /// 스킬 해금 조건(선행 스킬 레벨 등) 검사
    /// </summary>
    public bool CanUnlock(SkillData data)
    {
        if (data == null) return false;
        if (data.unlockConditions == null || data.unlockConditions.Length == 0) return true;

        foreach (var condition in data.unlockConditions)
        {
            var required = GetSkillById(condition.requiredSkillId);
            if (required == null) return false;

            int lv = GetSkillLevel(required);
            if (lv < condition.requiredLevel) return false;
        }
        return true;
    }

    #endregion

    #region 버프 저장
    public void SaveRunBuffBasket(IReadOnlyList<string> ids)
    {
        if (cache.runBuffBasket == null) cache.runBuffBasket = new List<string>();
        cache.runBuffBasket.Clear();
        if (ids != null) cache.runBuffBasket.AddRange(ids);
        JsonSave();
    }

    public List<string> LoadRunBuffBasket()
    {
        return cache.runBuffBasket != null
            ? new List<string>(cache.runBuffBasket)
            : new List<string>();
    }

    #endregion

    #region 퀘스트 관리

    public bool IsTutorialCompleted() => cache.tutorialCompleted;
    public int GetTutorialStep() => cache.tutorialStep;

    public void SetTutorialStep(int step, bool completed = false)
    {
        cache.tutorialStep = Mathf.Max(0, step);
        if (completed)
        {
            cache.tutorialCompleted = true;
        }
        JsonSave();
    }

    public List<QuestSave> GetActiveQuests()
    {
        JsonLoad();

        return cache.activeQuest ??= new List<QuestSave>();
    }

    public void SetActiveQuests(List<QuestSave> list)
    {
        cache.activeQuest = list ?? new List<QuestSave>();
        JsonSave();
    }

    public void UpsertQuestProgress(string questId, int progress, bool completed)
    {
        if (cache.activeQuest == null)
        {
            cache.activeQuest = new List<QuestSave>();
        }

        var q = cache.activeQuest.Find(x => x.questId == questId);
        if (q == null)
        {
            q = new QuestSave { questId = questId, progress = progress, completed = completed };
            cache.activeQuest.Add(q);
        }
        else
        {
            q.progress = progress;
            q.completed = completed;
        }
        JsonSave();
    }

    public void RemoveQuest(string questId)
    {
        cache.activeQuest?.RemoveAll(x => x.questId == questId);
        JsonSave();
    }

    #endregion

    #region 시설 강화 관리
    // 레벨 1부터 시작해야 하는 특정 시설 초기화
    private void InitializeLevelOneFacilities()
    {
        _facilityLevelMap[FacilityMaxLvIncreaseId] = 1;
        _facilityLevelMap[BuffSlotFacilityId] = 1;
        _facilityLevelMap[EquipmentMaxLvIncreaseId] = 1;
        _facilityLevelMap[RarityUnlockFacilityId] = 1;
        _facilityLevelMap[BuffUnlockFacilityId] = 1;
        _facilityLevelMap[MineCartFacilityId] = 1;

        Debug.Log($"[DataManager] 레벨 1 시작 시설 초기화 완료: GD002, TP003, BS002, BS003, TP002");
    }

    private void PushFacilityLevelMapToList()
    {
        if (cache.facilityLevels == null) cache.facilityLevels = new List<FacilityLevelKV>();
        cache.facilityLevels.Clear();

        foreach (var pair in _facilityLevelMap)
        {
            cache.facilityLevels.Add(new FacilityLevelKV { id = pair.Key, level = pair.Value });
        }
    }

    private void PullFacilityLevelListToMap()
    {
        _facilityLevelMap.Clear();
        if (cache.facilityLevels == null)
        {
            Debug.LogWarning("[DataManager] PullFacilityLevelListToMap: cache.facilityLevels가 null입니다.");
            return;
        }

        foreach (var kv in cache.facilityLevels)
        {
            if (kv == null)
            {
                Debug.LogWarning("[DataManager] 리스트에 null인 아이템이 있습니다. 파싱 실패.");
                continue;
            }
            if (string.IsNullOrEmpty(kv.id))
            {
                Debug.LogWarning($"[DataManager] 리스트 아이템의 ID가 비어있습니다. (Level: {kv.level})");
                continue;
            }

            Debug.Log($"[DataManager] 맵에 데이터 추가: ID={kv.id}, Level={kv.level}");
            _facilityLevelMap[kv.id] = kv.level;
        }
    }

    public int GetFacilityLevel(string facilityId)
    {
        // 1레벨 시작 시설 방어 코드
        if (facilityId == FacilityMaxLvIncreaseId || facilityId == BuffSlotFacilityId ||
            facilityId == EquipmentMaxLvIncreaseId || facilityId == RarityUnlockFacilityId ||
            facilityId == BuffUnlockFacilityId || facilityId == MineCartFacilityId)
        {
            if (!_facilityLevelMap.ContainsKey(facilityId)) return 1;
        }
        return _facilityLevelMap.TryGetValue(facilityId, out var lv) ? lv : 0;
    }

    public void SetFacilityLevel(string facilityId, int level)
    {
        if (string.IsNullOrEmpty(facilityId)) return;
        _facilityLevelMap[facilityId] = level;

        // 시설 레벨이 변한 경우 해금 장비 다시 계산
        if (facilityId == RarityUnlockFacilityId)
        {
            RefreshEquipmentUnlockByFacilities();
        }

        OnFacilityLevelChanged?.Invoke();
        JsonSave();
    }

    // 개발/테스트용 시설 레벨 초기화 함수
    public void ResetAllFacilityLevels()
    {
        _facilityLevelMap.Clear();
        if (cache.facilityLevels == null) cache.facilityLevels = new List<FacilityLevelKV>();
        cache.facilityLevels.Clear();

        // 1레벨 시설 다시 초기화
        InitializeLevelOneFacilities();

        JsonSave();
        Debug.Log("[DataManager] 모든 시설 강화 레벨이 초기화되었습니다.");
    }

    #endregion

    public void DeleteAllData()
    {
        // 1. 물리적인 저장 파일 삭제
        if (File.Exists(_path))
        {
            File.Delete(_path);
            Debug.Log($"[DataManager] 세이브 파일 삭제 완료: {_path}");
        }

        // 2. 메모리 상의 데이터(캐시) 초기화
        cache = new SaveData(); // 빈 데이터로 교체

        // 3. 런타임 딕셔너리들 초기화
        _levelMap.Clear();
        _facilityLevelMap.Clear();
        _equipmentDurabilityMap.Clear();

        // 4. 초기 필수 세팅 복구 (시설 1레벨 등)
        InitializeLevelOneFacilities(); // 시설 레벨 1로 세팅
        PushFacilityLevelMapToList();   // 리스트 동기화

        // 5. 장비 해금 상태 초기화 (시설 레벨 1 기준)
        RefreshEquipmentUnlockByFacilities(autoSave: false);

        // 6. 돈(EconomyService) 초기화
        var eco = EconomyServiceInstance();
        if (eco != null)
        {
            eco.SetMoney(0);
        }

        // 7. 퀘스트/튜토리얼 상태 초기화
        if (QuestManager.Instance != null)
        {
            // QuestManager 쪽에 초기화 로직이 있다면 호출, 없다면 DataManager 캐시만이라도 비움
            cache.activeQuest.Clear();
            cache.tutorialStep = 0;
            cache.tutorialCompleted = false;
        }

        // 8. 초기화된 상태를 즉시 파일로 저장 (선택 사항: 안 하면 다음 로드 시 새로 생성됨)
        JsonSave();

        Debug.Log("[DataManager] 모든 데이터가 초기화되었습니다.");
    }
}
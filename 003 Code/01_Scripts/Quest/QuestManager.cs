using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("카탈로그")]
    [SerializeField] private QuestData[] tutorialQuests;
    [SerializeField] private QuestData[] repeatableQuests;

    public event Action OnQuestLogChanged;

    private DataManager D => DataManager.Instance;

    // 런타임 캐시
    private readonly Dictionary<string, QuestData> _index = new();
    private readonly Dictionary<string, QuestSave> _active = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildIndex();
        LoadFromSave();
    }

    private void OnEnable()
    {
        QuestEvents.OnMonsterKilled += HandleMonsterKilled;
        QuestEvents.OnOreAcquired += HandleOreAcquired;
        QuestEvents.OnFlagRaised += HandleFlagRaised;
    }
    private void OnDisable()
    {
        QuestEvents.OnMonsterKilled -= HandleMonsterKilled;
        QuestEvents.OnOreAcquired -= HandleOreAcquired;
        QuestEvents.OnFlagRaised -= HandleFlagRaised;
    }

    private void BuildIndex()
    {
        _index.Clear();
        foreach (var q in tutorialQuests) if (q && !string.IsNullOrEmpty(q.questId)) _index[q.questId] = q;
        foreach (var q in repeatableQuests) if (q && !string.IsNullOrEmpty(q.questId)) _index[q.questId] = q;
    }

    private void LoadFromSave()
    {
        //_active.Clear();
        //if (D == null) return;
        //foreach (var s in D.GetActiveQuests())
        //    if (!string.IsNullOrEmpty(s.questId) && _index.TryGetValue(s.questId, out _))
        //        _active[s.questId] = s;

        _active.Clear();
        if (D == null)
        {
            Debug.LogWarning("[QuestManager] DataManager가 아직 없습니다.");
            return;
        }

        var list = D.GetActiveQuests();
        Debug.Log($"[QuestManager] LoadFromSave: 가져온 activeQuest 수 = {list.Count}");

        foreach (var s in list)
            if (!string.IsNullOrEmpty(s.questId) && _index.TryGetValue(s.questId, out _))
                _active[s.questId] = s;

        Debug.Log($"[QuestManager] 로드 후 _active.Count = {_active.Count}");
    }

    private void SaveAll()
    {
        if (D == null) return;
        D.SetActiveQuests(_active.Values.ToList());

        Debug.Log("DataManager에 현재 활성 퀘스트 저장");

        OnQuestLogChanged?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 튜토리얼 제어

    private void HandleFlagRaised(string flagId)
    {
        foreach (var kv in _active.ToArray())
        {
            var def = _index[kv.Key];
            if (!string.Equals(def.targetId, flagId)) continue;

            var s = kv.Value;
            s.progress = def.targetCount; // (1회 목표)
            s.completed = true;
            _active[kv.Key] = s;
        }
        SaveAll();
    }

    public bool HasPendingTutorial()
    {
        if (D == null) return false;
        if (D.IsTutorialCompleted()) return false;
        int step = D.GetTutorialStep();
        var next = GetTutorialByStep(step);
        return next != null;
    }

    public QuestData GetTutorialByStep(int step)
    {
        if (tutorialQuests == null || tutorialQuests.Length == 0) return null;
        var ordered = tutorialQuests.Where(t => t != null).OrderBy(t => t.tutorialStepOrder).ToArray();
        return (step >= 0 && step < ordered.Length) ? ordered[step] : null;
    }
    
    // 튜토리얼 퀘스트 수락 함수
    public QuestSave EnsureTutorialAccepted()
    {
        if (D == null) return null;
        int step = D.GetTutorialStep();
        var def = GetTutorialByStep(step);
        if (def == null) return null;

        if (!_active.TryGetValue(def.questId, out var save))
        {
            save = new QuestSave { questId = def.questId, progress = 0, completed = false };
            _active[def.questId] = save;
            Debug.Log($"튜토리얼 퀘스트 수락: {def.title} ({def.questId})");
            SaveAll();
        }
        return save;
    }

    // 튜토리얼 퀘스트 완료 함수
    public bool TryCompleteTutorial(string questId)
    {
        if (!_index.TryGetValue(questId, out var def) || def.kind != QuestKind.Tutorial) return false;
        if (!_active.TryGetValue(questId, out var save)) return false;

        //  - goalType == None 이고
        //  - targetId (플래그/목표 ID)가 비어 있을 때만
        if (def.goalType == QuestGoalType.None && string.IsNullOrEmpty(def.targetId))
        {
            save.completed = true;
            _active[questId] = save;
        }

        // 그 외(플래그/킬/수집 등)는 외부에서 progress/completed가 이미 true가 되어 있어야만 완료 가능
        if (!save.completed)
            return false;

        Debug.Log($"튜토리얼 퀘스트 완료!: {def.title} ({def.questId})");

        GrantReward(def);
        _active.Remove(questId);

        if (D == null)
        {
            Debug.LogError("[QuestManager] DataManager 가 null입니다. 튜토리얼 스텝을 진행할 수 없습니다.");
            return false;
        }

        // 다음 단계로
        int nextStep = D.GetTutorialStep() + 1;
        var nextDef = GetTutorialByStep(nextStep);
        bool hasNext = nextDef != null;

        D.SetTutorialStep(nextStep, completed: !hasNext);

        //// 다음 튜토리얼 퀘스트가 있다면 자동 수주
        //if (hasNext && !_active.ContainsKey(nextDef.questId))
        //{
        //    _active[nextDef.questId] = new QuestSave
        //    {
        //        questId = nextDef.questId,
        //        progress = 0,
        //        completed = false
        //    };

        //    Debug.Log($"[QuestManager] 다음 튜토리얼 자동 수주: {nextDef.title} ({nextDef.questId})");
        //}

        // 다음 튜토리얼 퀘스트 자동 수주 X
        if (hasNext)
        {
            Debug.Log($"[QuestManager] 다음 튜토리얼 준비됨: {nextDef.title} ({nextDef.questId}). " +
                      $"해당 NPC와 대화 시 수주 팝업이 열립니다.");
        }

        SaveAll();
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 반복 퀘스트

    public List<QuestData> DrawRandomRepeatables(int count)
    {
        var pool = repeatableQuests?.Where(q => q && !string.IsNullOrEmpty(q.questId)).ToList() ?? new List<QuestData>();
        pool.RemoveAll(q => _active.ContainsKey(q.questId));

        for (int i = 0; i < pool.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.Take(Mathf.Clamp(count, 0, 3)).ToList();
    }

    public bool AcceptQuest(QuestData def)
    {
        if (def == null || string.IsNullOrEmpty(def.questId)) return false;
        if (_active.ContainsKey(def.questId)) return false;

        // 반복퀘는 최대 3개 제한 (튜토리얼은 예외)
        if (def.kind == QuestKind.Repeatable)
        {
            int repeatActive = _active.Values.Count(v => _index[v.questId].kind == QuestKind.Repeatable);
            if (repeatActive >= 3) return false;
        }

        _active[def.questId] = new QuestSave { questId = def.questId, progress = 0, completed = false };
        SaveAll();

        Debug.Log($"[QuestManager] 퀘스트 수락: {def.questId}");

        return true;
    }

    public bool TryTurnIn(string questId)
    {
        if (!_index.TryGetValue(questId, out var def)) return false;
        if (!_active.TryGetValue(questId, out var save)) return false;

        if (def.kind == QuestKind.Repeatable && !save.completed) return false;
        if (def.kind == QuestKind.Tutorial && def.goalType != QuestGoalType.None && !save.completed) return false;

        GrantReward(def);
        _active.Remove(questId);
        SaveAll();
        return true;
    }

    private void GrantReward(QuestData def)
    {
        if (def.rewardMoney != 0)
            (EconomyService.Instance ?? FindObjectOfType<EconomyService>())?.AddMoney(def.rewardMoney);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 진행 갱신

    private void HandleMonsterKilled(string monsterId, int count)
    {
        foreach (var kv in _active.ToArray())
        {
            var def = _index[kv.Key];
            if (def.goalType != QuestGoalType.Kill) continue;
            if (!string.Equals(def.targetId, monsterId)) continue;

            var s = kv.Value;
            s.progress = Mathf.Min(def.targetCount, s.progress + count);
            s.completed = s.progress >= def.targetCount;
            _active[kv.Key] = s;
        }
        SaveAll();
    }

    private void HandleOreAcquired(string oreId, int count)
    {
        foreach (var kv in _active.ToArray())
        {
            var def = _index[kv.Key];
            if (def.goalType != QuestGoalType.Collect) continue;
            if (!string.Equals(def.targetId, oreId)) continue;

            var s = kv.Value;
            s.progress = Mathf.Min(def.targetCount, s.progress + count);
            s.completed = s.progress >= def.targetCount;
            _active[kv.Key] = s;
        }
        SaveAll();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // NPC 기반 수주/완수 API

    /// <summary>이 NPC가 지금 줄(수주) 수 있는 퀘스트가 있는지</summary>
    public bool HasOfferAtNpc(string npcId, out QuestData offer)
    {
        offer = null;

        // 튜토리얼: 현재 스텝 미수주 && assignNpcId 일치 → 수주 가능
        if (HasPendingTutorial())
        {
            var def = GetTutorialByStep(D.GetTutorialStep());
            bool alreadyActive = def != null && _active.ContainsKey(def.questId);
            if (def != null && !alreadyActive && def.assignNpcId == npcId)
            {
                offer = def;
                return true;
            }
        }

        // 반복퀘: assignNpcId 일치 && 미수주
        foreach (var q in repeatableQuests)
        {
            if (!q || string.IsNullOrEmpty(q.questId)) continue;
            if (q.assignNpcId != npcId) continue;
            if (_active.ContainsKey(q.questId)) continue;

            offer = q; // 여러 개인 경우 UI에서 리스트 표기
            return true;
        }
        return false;
    }

    /// <summary>이 NPC에게 지금 턴인(완수) 가능한 퀘스트가 있는지</summary>
    public bool HasTurnInAtNpc(string npcId, out QuestData completable)
    {
        completable = null;

        foreach (var kv in _active)
        {
            var s = kv.Value;
            if (s == null) continue;

            if (!_index.TryGetValue(kv.Key, out var def) || def == null) continue;
            if (string.IsNullOrEmpty(def.completeNpcId)) continue;
            if (def.completeNpcId != npcId) continue;

            if (def.kind == QuestKind.Tutorial)
            {
                // [핵심] '대화만으로 완료되는' 튜토리얼인지 체크
                bool isTalkOnlyTutorial =
                    def.goalType == QuestGoalType.None &&
                    string.IsNullOrEmpty(def.targetId);

                // - 대화 전용 튜토리얼: 바로 턴인 가능
                // - 그 외(플래그/킬/수집 등): save.completed 가 true 여야만 턴인 가능
                bool ready = isTalkOnlyTutorial || s.completed;

                if (ready)
                {
                    completable = def;
                    return true;
                }
            }
            else
            {
                // 반복퀘 등: completed 되어야만 턴인 가능
                if (s.completed)
                {
                    completable = def;
                    return true;
                }
            }
        }
        return false;
    }


    /// <summary>NPC 대화에서 바로 호출: 수주 시도</summary>
    public bool TryAcceptAtNpc(string  npcId)
    {
        if (!HasOfferAtNpc(npcId, out var def) || def == null) return false;
        return AcceptQuest(def);
    }

    /// <summary>NPC 대화에서 바로 호출: 턴인 시도</summary>
    public bool TryCompleteAtNpc(string npcId)
    {
        if (!HasTurnInAtNpc(npcId, out var def) || def == null)
        {
            return false;
        }

        return (def.kind == QuestKind.Tutorial)
            ? TryCompleteTutorial(def.questId)
            : TryTurnIn(def.questId);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 조회
    public IReadOnlyDictionary<string, QuestSave> Active => _active;
    public bool TryGet(string questId, out QuestData def) => _index.TryGetValue(questId, out def);

    public bool HasOfferAtNpc(int npcId, out QuestData offer)
    => HasOfferAtNpc(npcId.ToString(), out offer);

    public bool HasTurnInAtNpc(int npcId, out QuestData completable)
        => HasTurnInAtNpc(npcId.ToString(), out completable);

    public bool TryAcceptAtNpc(int npcId)
        => TryAcceptAtNpc(npcId.ToString());

    public bool TryCompleteAtNpc(int npcId)
        => TryCompleteAtNpc(npcId.ToString());
}

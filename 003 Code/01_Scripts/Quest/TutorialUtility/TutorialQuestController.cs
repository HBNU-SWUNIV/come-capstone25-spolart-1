using System;
using System.Linq;
using UnityEngine;

public class TutorialQuestController : MonoBehaviour
{
    public static TutorialQuestController Instance { get; private set; }

    private bool _subscribedToTownUI = false;

    // 현재 진행중인 튜토리얼 퀘스트 가져오기
    private QuestData CurQuest
    {
        get
        {
            var qm = QuestManager.Instance;
            var dm = DataManager.Instance;
            if (qm == null || dm == null) return null;
            return qm.GetTutorialByStep(dm.GetTutorialStep());
        }
    }

    // 퀘스트 ID 획득
    private string CurQuestId => CurQuest != null ? CurQuest.questId : null;

    // 특정 스텝인지 확인
    private bool Step(int step) => DataManager.Instance.GetTutorialStep() == step;

    [Header("퀘스트 팝업")]
    [SerializeField] private QuestPopupUI questPopupUI;

    [Header("NPC ID")]
    [SerializeField] private int blacksmithNpcId = 0;   // 대장장이
    [SerializeField] private int priestNpcId = 1;       // 사제장
    [SerializeField] private int guildMasterNpcId = 2;  // 길드장
    [SerializeField] private int knightNpcId = 3;       // 기사단원

    [Header("튜토리얼 퀘스트 ID")]
    [SerializeField] private string questId; // QuestManager.TryGet으로 불러오기

    [Header("튜토리얼용 플래그 ID")]
    [SerializeField] private string flagWeaponRepaired = "WEAPON_REPAIRED";
    [SerializeField] private string flagQuestAccepted = "QUEST_ACCEPTED";
    [SerializeField] private string flagEnterDungeon = "ENTER_DUNGEON";
    [SerializeField] private string flagUseBucket = "USE_BUCKET";
    [SerializeField] private string flagBuyBuff = "BUY_BUFF";
    [SerializeField] private string flagUpgradeForge = "UPGRADE_FORGE";
    [SerializeField] private string flagRepeatableAccepted = "REPEATABLE_ACCEPTED";
    //[SerializeField] private string flag

    [Header("NPC")]
    [SerializeField] private NPCInteractable blacksmithNpc;
    [SerializeField] private NPCInteractable knightNpc;
    [SerializeField] private NPCInteractable priestNpc;
    [SerializeField] private NPCInteractable guildmasterNpc;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Debug.Log("[TutorialQuestController] OnEnable 호출");

        TrySubscribeTownUI();  // 이 시점에 이미 있으면 바로 구독
        QuestEvents.OnFlagRaised += HandleFlagRaised;
    }

    private void OnDisable()
    {
        var town = TownUIManager.Instance;
        if (town != null)
        {
            town.OnNpcTalkStarted -= HandleTalkStarted;
            town.OnNpcTalkEnded -= HandleTalkEnded;
        }
        _subscribedToTownUI = false;

        QuestEvents.OnFlagRaised -= HandleFlagRaised;

        Debug.Log("[TutorialQuestController] 이벤트 구독 해지");
    }

    private void Update()
    {
        // 아직 구독 못했고, TownUIManager가 생겼으면 다시 시도
        if (!_subscribedToTownUI && TownUIManager.Instance != null)
        {
            TrySubscribeTownUI();
        }
    }

    private void TrySubscribeTownUI()
    {
        var town = TownUIManager.Instance;
        if (town == null)
        {
            // 아직 TownUIManager가 안 떠 있으면 패스
            return;
        }

        if (_subscribedToTownUI)
            return;

        // 혹시 중복 구독 방지를 위해 한 번 제거 후 추가
        town.OnNpcTalkStarted -= HandleTalkStarted;
        town.OnNpcTalkEnded -= HandleTalkEnded;
        town.OnNpcTalkStarted += HandleTalkStarted;
        town.OnNpcTalkEnded += HandleTalkEnded;

        _subscribedToTownUI = true;
        Debug.Log("[TutorialQuestController] TownUIManager 이벤트 구독 완료");
    }

    #region === 튜토리얼 상태 헬퍼 ===
    private QuestData GetCurrentTutorialDef()
    {
        var qm = QuestManager.Instance;
        var dm = DataManager.Instance;
        if (qm == null || dm == null) return null;

        int step = dm.GetTutorialStep();
        return qm.GetTutorialByStep(step);
    }

    private string GetCurrentTutorialQuestId()
    {
        var def = GetCurrentTutorialDef();
        return def != null ? def.questId : null;
    }

    // 특정 퀘스트 ID가 지금 진행 중인 튜토리얼인지 체크
    public bool IsCurrentTutorial(string questId)
    {
        var qm = QuestManager.Instance;
        var dm = DataManager.Instance;
        if (qm == null || dm == null) return false;

        var def = qm.GetTutorialByStep(dm.GetTutorialStep());
        return def != null && def.questId == questId;
    }

    // “이 튜토리얼 퀘스트일 때만” 플래그를 올리는 헬퍼
    public bool RaiseFlagForTutorial(string questId, string flagId)
    {
        if (!IsCurrentTutorial(questId)) return false;
        QuestEvents.RaiseFlag(flagId);
        return true;
    }
    #endregion

    #region === NPC 대화 시작/종료 이벤트 처리 ===
    private void HandleTalkStarted(NPC_Data npc)
    {
        if (CurQuest == null) return;

        //// QT-002: 대장장이와 대화 시작 시 자동 수주
        //if (Step(1) && npc.npcId == blacksmithNpcId)
        //{
        //    QuestManager.Instance.EnsureTutorialAccepted();
        //    return;
        //}
    }

    private void HandleTalkEnded(int npcId)
    {
        var qm = QuestManager.Instance;
        if (qm == null)
        {
            Debug.Log("[TutorialQuestController] 퀘스트 매니저가 없습니다.");
            return;
        }

        Debug.Log($"[TutorialQuestController] HandleTalkEnded npcId={npcId}");

        // 1) 먼저 '완료 가능' 의뢰가 있는지 확인 (턴인을 우선)
        if (qm.HasTurnInAtNpc(npcId, out var completable) && completable != null)
        {
            Debug.Log($"[TutorialQuestController] 완료 가능 의뢰 존재: {completable.questId} ({completable.kind})");

            questPopupUI.ShowTurnIn(completable, () =>
            {
                bool ok;
                if (completable.kind == QuestKind.Tutorial)
                {
                    ok = qm.TryCompleteTutorial(completable.questId);
                }
                else
                {
                    ok = qm.TryTurnIn(completable.questId);
                }

                Debug.Log($"[TutorialQuestController] 퀘스트 턴인 결과: {ok}");
            });
            return;
        }

        // 2) 수주 가능한 의뢰가 있는지 확인
        if (qm.HasOfferAtNpc(npcId, out var offer) && offer != null)
        {
            Debug.Log($"[TutorialQuestController] 수주 가능 의뢰 존재: {offer.questId} ({offer.kind})");

            // 튜토리얼 / 반복 퀘스트 공통으로 팝업을 띄우되,
            // 튜토리얼일 때는 EnsureTutorialAccepted를 사용해 스텝과 동기화
            questPopupUI.ShowOffer(offer, () =>
            {
                bool ok = false;

                if (offer.kind == QuestKind.Tutorial)
                {
                    var save = qm.EnsureTutorialAccepted();
                    ok = (save != null);

                    if (ok)
                    {
                        Debug.Log($"[TutorialQuestController] 튜토리얼 수락 완료: {offer.title} ({offer.questId})");
                    }
                    else
                    {
                        Debug.LogWarning("[TutorialQuestController] 튜토리얼 수락 실패: EnsureTutorialAccepted()가 null을 반환");
                    }
                }
                else
                {
                    ok = qm.AcceptQuest(offer);
                    Debug.Log($"[TutorialQuestController] 반복 의뢰 수락 결과: {ok}");
                }
            });

            return;
        }

        Debug.Log("[TutorialQuestController] 완료/수주 가능한 의뢰 없음");
    }

    #endregion

    #region === 플래그 이벤트 처리 ===
    public void HandleFlagRaised(string flagId)
    {
        if (CurQuest == null) return;

        Debug.Log("플래그 확인");

        //if (Step(2) && flagId == flagWeaponRepaired)
        //{
        //    TutorialAPI.CompleteCurrent(CurQuestId);
        //    Debug.Log($"{flagId} 플래그로 퀘스트 완료 처리 성공!");
        //    return;
        //}
    }
    #endregion
}

using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [Header("UI 표시 오브젝트")]
    [Tooltip("퀘스트 수주 가능할 때 표시 (!)")]
    [SerializeField] private GameObject hintOfferQuest;

    [Tooltip("퀘스트 완수 가능할 때 표시 (?)")]
    [SerializeField] private GameObject hintTurnInQuest;

    [Tooltip("단순 상호작용 가능 표시")]
    [SerializeField] private GameObject hintTalk;

    // 내부 상태
    private bool isPlayerInRange = false;
    private string npcId;

    private void Awake()
    {
        var data = GetComponent<NPC_Data>();
        npcId = data ? data.npcId.ToString() : "0";

        // 초기화 시 모두 끄기
        SetIndicators(false, false, false);
    }

    private void Update()
    {
        UpdateIndicator();
    }

    public void SetPlayerInRange(bool inRange)
    {
        isPlayerInRange = inRange;
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        // QuestManager가 없으면 아무것도 하지 않음 (안전장치)
        var qm = QuestManager.Instance;
        if (qm == null) return;

        // 1) 퀘스트 상태 조회 (거리와 상관없이 항상 체크)
        bool hasTurnIn = qm.HasTurnInAtNpc(npcId, out var turnInQuest);
        bool hasOffer = qm.HasOfferAtNpc(npcId, out var offerQuest);

        // 2) 우선순위에 따른 표시 로직

        // [Priority 1] 완수 가능 (물음표) - 거리가 멀어도 보여야 함
        if (hasTurnIn)
        {
            SetIndicators(true, false, false);
            return;
        }

        // [Priority 2] 수주 가능 (느낌표) - 거리가 멀어도 보여야 함
        if (hasOffer)
        {
            SetIndicators(false, true, false);
            return;
        }

        // [Priority 3] 일반 대화 - 퀘스트가 없을 때 표시
        // 단, '단순 대화' 표시는 보통 가까이 갔을 때만(상호작용 가능할 때만) 띄우는 것이 일반적입니다.
        // 만약 멀리서도 '대화 가능함'을 띄우고 싶다면 if 문을 제거하면 됩니다.
        if (isPlayerInRange)
        {
            SetIndicators(false, false, true);
        }
        else
        {
            // 퀘스트도 없고, 범위 밖이라면 모든 인디케이터 끄기
            SetIndicators(false, false, false);
        }
    }

    // 코드 중복을 줄이기 위한 헬퍼 함수
    private void SetIndicators(bool turnIn, bool offer, bool talk)
    {
        if (hintTurnInQuest && hintTurnInQuest.activeSelf != turnIn)
            hintTurnInQuest.SetActive(turnIn);

        if (hintOfferQuest && hintOfferQuest.activeSelf != offer)
            hintOfferQuest.SetActive(offer);

        if (hintTalk && hintTalk.activeSelf != talk)
            hintTalk.SetActive(talk);
    }
}
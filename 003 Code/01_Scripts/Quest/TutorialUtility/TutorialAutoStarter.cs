using UnityEngine;

/// <summary>
/// 플레이어가 트리거에 처음 들어갔을 때
/// 현재 튜토리얼 스텝과 일치한다면
/// - 지정한 NPC와 자동으로 대화 시작
/// (이제는 여기서 퀘스트를 수주/완료하지 않음)
/// </summary>
public class TutorialAutoStarter : MonoBehaviour
{
    [Header("이 트리거가 담당할 튜토리얼 스텝 인덱스")]
    [SerializeField] private int tutorialStepIndex = 0;

    [Header("자동 대화 시작에 사용할 NPC 데이터")]
    [SerializeField] private NPC_Data knightNpcData;

    [Header("한 번만 발동할지 여부")]
    [SerializeField] private bool oneShot = true;

    private bool _triggered;

    private void Reset()
    {
        // 에디터에서 컴포넌트를 붙였을 때 자동으로 트리거로 설정
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        var dm = DataManager.Instance;
        var qm = QuestManager.Instance;

        if (dm == null || qm == null)
            return;

        // 튜토리얼이 전부 끝났으면 자기 자신 비활성화
        if (!qm.HasPendingTutorial())
        {
            DisableSelf();
            return;
        }

        // 이미 이 스텝을 지나친 경우에도 동작할 필요 없음
        if (dm.GetTutorialStep() > tutorialStepIndex)
        {
            DisableSelf();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered && oneShot)
            return;

        if (!other.CompareTag("Player"))
            return;

        var dm = DataManager.Instance;
        var qm = QuestManager.Instance;
        if (dm == null || qm == null)
            return;

        // 튜토리얼 자체가 더 이상 남아있지 않으면 종료
        if (!qm.HasPendingTutorial())
        {
            DisableSelf();
            return;
        }

        int currentStep = dm.GetTutorialStep();

        // 현재 튜토리얼 스텝과 이 트리거가 담당하는 스텝이 다르면 패스
        if (currentStep != tutorialStepIndex)
            return;

        // *** 중요: 여기서는 퀘스트를 자동 수주/완료하지 않습니다. ***
        // 수락/완료는 TutorialQuestController.HandleTalkEnded + 퀘스트 팝업에서 처리.

        // 타운 UI를 통해 기사단원과 자동 대화 시작
        var ui = FindObjectOfType<TownUIManager>();
        if (ui == null)
        {
            Debug.LogWarning("[TutorialAutoStarter] TownUIManager를 찾지 못했습니다.");
            return;
        }

        if (ui.IsBusy) // 이미 다른 대화/메뉴 중이면 스킵
            return;

        if (knightNpcData != null)
        {
            ui.StartConversation(knightNpcData);
            ui.Option_Talk();  // 실제 대화 선택
        }

        _triggered = true;
        if (oneShot)
            DisableSelf();
    }

    private void DisableSelf()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }
}

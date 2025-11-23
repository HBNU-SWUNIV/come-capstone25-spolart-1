using UnityEngine;

public class TutorialBootstrap : MonoBehaviour
{
    [SerializeField] private Transform kinghtNpc;
    [SerializeField] private GameObject hint; // 기사단원 머리 위 느낌표

    private void Start()
    {
        var qm = QuestManager.Instance;
        var dm = DataManager.Instance;
        if (qm == null || dm == null) return;

        // 튜토리얼이 모두 끝났다면 느낌표 숨김
        if (!qm.HasPendingTutorial())
        {
            if (hint) hint.SetActive(false);
            return;
        }

        // 1단계 스텝(0번)일 때만 기사단원 머리 위에 느낌표 표시
        if (dm.GetTutorialStep() == 0 && hint != null)
        {
            hint.SetActive(true);
        }
    }
}
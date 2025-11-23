using UnityEngine;

public static class TutorialAPI
{
    public static void CompleteCurrent(string questId)
    {
        var qm = QuestManager.Instance;
        var dm = DataManager.Instance;
        if (qm == null || dm == null) return;

        // 현재 단계 보증 (저장된 데이터가 없다면 자동 수주)
        var save = qm.EnsureTutorialAccepted();
        if (save == null) return;

        if (save.questId == questId)
        {
            if (qm.TryGet(questId, out var def))
            {
                dm.UpsertQuestProgress(questId, def.targetCount > 0 ? def.targetCount : 1, completed: true);
                // 퀘스트 매니저에게 실제 완료 처리
                qm.TryCompleteTutorial(questId);
            }
        }
    }
}

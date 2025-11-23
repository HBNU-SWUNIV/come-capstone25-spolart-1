using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class CurrentQuestUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private TMP_Text currentQuestLog;

    private QuestManager qm;
    private DataManager dm;
    
    private void OnEnable()
    {
        Debug.Log("[CurrentQuestUI] 퀘스트 로그 UI 활성화");

        if (qm == null) qm = QuestManager.Instance;
        if (dm == null) dm = DataManager.Instance;

        if (qm == null || dm == null)
        {
            Debug.LogWarning("[CurrentQuestUI] QuestManager 또는 DataManager 없음");
            return;
        }

        dm.JsonLoad();

        // 중복 구독 방지
        qm.OnQuestLogChanged -= RefreshQuestLog;
        qm.OnQuestLogChanged += RefreshQuestLog;

        RefreshQuestLog();
    }

    private void OnDisable()
    {
        if (qm != null)
        {
            qm.OnQuestLogChanged -= RefreshQuestLog;
        }
    }

    public void RefreshQuestLog()
    {
        if (currentQuestLog == null)
            return;

        StringBuilder sb = new StringBuilder();

        // QuestManager.Active에 데이터가 있으면 그걸 최우선으로 표시
        if (qm != null && qm.Active.Count > 0)
        {
            AppendActiveQuests(qm.Active, sb);
        }
        else
        {
            // QuestManager Active가 비어있을 경우:
            // DataManager 저장된 activeQuest를 로드하여 표시
            var savedList = dm?.GetActiveQuests();

            if (savedList != null && savedList.Count > 0)
            {
                foreach (var qs in savedList)
                {
                    if (qm != null && qm.TryGet(qs.questId, out var def))
                    {
                        string title = def.title;
                        string progressInfo = qs.completed
                            ? "(완료!)"
                            : (def.targetCount > 0 ? $"{qs.progress}/{def.targetCount}" : "(진행중)");

                        sb.AppendLine($"- {title} {progressInfo}");
                    }
                }
            }
            else
            {
                sb.AppendLine("진행 중인 의뢰가 없습니다.");
            }
        }

        currentQuestLog.text = sb.ToString();
    }

    // 퀘스트 매니저에서 불러온 내용을 텍스트에 추가
    private void AppendActiveQuests(IReadOnlyDictionary<string, QuestSave> active, StringBuilder sb)
    {
        if (active.Count == 0)
        {
            sb.AppendLine("진행 중인 의뢰가 없습니다.");
            return;
        }

        foreach (var kv in active)
        {
            var questSave = kv.Value;

            if (qm.TryGet(questSave.questId, out var questDef))
            {
                string title = questDef.title;
                string progressInfo = "";

                if (questSave.completed)
                {
                    progressInfo = "(완료!)";
                }
                else if (questDef.targetCount > 0)
                {
                    progressInfo = $"{questSave.progress}/{questDef.targetCount}";
                }
                else
                {
                    progressInfo = "(진행중)";
                }

                sb.AppendLine($"- {title} {progressInfo}");
            }
        }
    }
}

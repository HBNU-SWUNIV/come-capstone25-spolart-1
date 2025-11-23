using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuildMasterQuestController : MonoBehaviour
{
    [Header("UI (좌측)")]
    [SerializeField] private Transform questListParent;
    [SerializeField] private QuestListItemUI listItemPrefab;

    [Header("UI (우측)")]
    [SerializeField] private TMP_Text detailDescText;     // 퀘스트 상세설명
    [SerializeField] private Image coinImage;             // 코인 이미지 (활성/비활성용)
    [SerializeField] private TMP_Text rewardText;         // 보상
    [SerializeField] private TMP_Text completeMarkText;   // 의뢰 목표 달성 표시
    [SerializeField] private Button acceptButton;         // 수락 버튼
    [SerializeField] private Button completeButton;       // 완료 버튼
    [SerializeField] private Button rerollButton;         // 새로고침 버튼 (해당 의뢰 포기 및 재수주)

    [Header("하단 버튼")]
    [SerializeField] private Button checkButton;  // 의뢰 확인 버튼 프리팹
    [SerializeField] private Button acceptAllButton; // 일괄 수락 버튼
    [SerializeField] private Button completeAllButton; // 일괄 완료 버튼

    [Header("퀘스트 설정")]
    [SerializeField] private int offerCount = 3;

    // 생성할 퀘스트 아이템 리스트
    private readonly List<QuestListItemUI> _items = new();

    // 수주받은 퀘스트
    private readonly List<QuestData> _offers = new();

    private QuestData _selected;
    private int _selectedIndex = -1;

    private QuestManager QM => QuestManager.Instance;

    private void OnEnable()
    {
        WireButtons(true);

        if (QM != null)
            QM.OnQuestLogChanged += HandleQuestLogChanged;

        LoadActiveRepeatableQuestFromDatabase();
    }

    private void OnDisable()
    {
        WireButtons(false);

        if (QM != null)
            QM.OnQuestLogChanged -= HandleQuestLogChanged;

        ClearAllItems();
        _offers.Clear();
        _selected = null;
        _selectedIndex = -1;
    }

    private void WireButtons(bool on)
    {
        if (on)
        {
            if (checkButton) checkButton.onClick.AddListener(OnClickCheckQuest);
            if (acceptButton) acceptButton.onClick.AddListener(OnClickAccept);
            if (rerollButton) rerollButton.onClick.AddListener(OnClickRerollSelected);
            if (completeAllButton) completeAllButton.onClick.AddListener(OnClickCompleteAll);
            if (acceptAllButton) acceptAllButton.onClick.AddListener(OnClickAcceptAll);
            if (completeButton) completeButton.onClick.AddListener(OnClickCompleteSelected);
        }
        else
        {
            if (checkButton) checkButton.onClick.RemoveListener(OnClickCheckQuest);
            if (acceptButton) acceptButton.onClick.RemoveListener(OnClickAccept);
            if (rerollButton) rerollButton.onClick.RemoveListener(OnClickRerollSelected);
            if (completeAllButton) completeAllButton.onClick.RemoveListener(OnClickCompleteAll);
            if (acceptAllButton) acceptAllButton.onClick.RemoveListener(OnClickAcceptAll);
            if (completeButton) completeButton.onClick.RemoveListener(OnClickCompleteSelected);
        }
    }

    // 인덱스를 통해 퀘스트 선택
    private void SelectIndex(int index)
    {
        _selectedIndex = index;
        _selected = (index >= 0 && index < _offers.Count) ? _offers[index] : null;

        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].SetSelected(i == index);
        }

        if (_selected == null)
        {
            ClearDetail();
            return;
        }

        // 상세 정보 갱신
        if (detailDescText)
            detailDescText.SetText(_selected.description);

        if (coinImage)
            coinImage.gameObject.SetActive(true);

        if (rewardText)
            rewardText.SetText($"{_selected.rewardMoney:N0}");

        // 버튼 상태는 헬퍼로 일괄 처리
        RefreshButtonsForSelected();
    }


    private void LoadActiveRepeatableQuestFromDatabase()
    {
        ClearAllItems();
        _offers.Clear();
        ClearDetail();

        if (QM == null) return;

        var active = new List<QuestData>();

        foreach (var kv in QM.Active)
        {
            // kv.Key: questId, kv.Value: QuestSave
            if (!QM.TryGet(kv.Key, out var def)) continue;
            if (def == null) continue;
            if (def.kind != QuestKind.Repeatable) continue;

            active.Add(def);
        }

        // 활성 반복 퀘스트를 현재 보드에 올리기
        foreach (var def in active)
        {
            _offers.Add(def);

            Debug.Log($"아이템 생성: {def.questId}");
            var item = Instantiate(listItemPrefab, questListParent);
            _items.Add(item);

            int progress = 0;
            if (QM.Active.TryGetValue(def.questId, out var save))
            {
                progress = save.progress;
            }

            int captureIndex = _items.Count - 1;
            item.Bind(def, progress, def.targetCount, _ =>
            {
                SelectIndex(captureIndex);
            });
        }

        if (_offers.Count > 0)
        {
            SelectIndex(0);
        }
        else
        {
            ClearDetail();
        }
    }

    private void ClearDetail()
    {
        if (detailDescText) detailDescText.SetText("");
        if (coinImage) coinImage.gameObject.SetActive(false);
        if (rewardText) rewardText.SetText("");
        if (completeMarkText) completeMarkText.gameObject.SetActive(false);

        if (acceptButton) acceptButton.gameObject.SetActive(false);
        if (completeButton) completeButton.gameObject.SetActive(false);

        _selected = null;
        _selectedIndex = -1;
    }

    #region 버튼 액션
    // 의뢰 확인
    public void OnClickCheckQuest()
    {
        if (QM == null) return;

        int currentCount = _offers.Count;
        int remaining = Mathf.Clamp(offerCount - currentCount, 0, offerCount);

        // 이미 3개 꽉 차 있으면 더 생성하지 않음
        if (remaining <= 0) return;

        // 중복 방지용 exclude 집합 (이미 보드에 떠있는 것 + 수주한 것)
        var exclude = new HashSet<string>(_offers.Select(q => q.questId));
        foreach (var a in QM.Active)
            exclude.Add(a.Key);

        int candidateCount = remaining * 4; // 여유 있는 풀
        var pool = QM.DrawRandomRepeatables(candidateCount);

        int added = 0;
        foreach (var def in pool)
        {
            if (def == null) continue;
            if (exclude.Contains(def.questId)) continue;

            exclude.Add(def.questId);
            _offers.Add(def);

            var item = Instantiate(listItemPrefab, questListParent);
            _items.Add(item);

            int progress = 0;
            if (QM.Active.TryGetValue(def.questId, out var save))
                progress = save.progress;

            int captureIndex = _items.Count - 1;
            item.Bind(def, progress, def.targetCount, _ =>
            {
                SelectIndex(captureIndex);
            });

            added++;
            if (added >= remaining) break;
        }

        // 기존에 아무것도 선택되어 있지 않고, 새로 생성된 게 있다면 0번 선택
        if (_selectedIndex < 0 && _offers.Count > 0)
        {
            SelectIndex(0);
        }

        // QT-004 퀘스트 수주하기 플래그
        var tut = TutorialQuestController.Instance;
        if (tut != null)
        {
            tut.RaiseFlagForTutorial("QT-004", "QUEST_ACCEPTED");
        }
    }

    private void OnClickAccept()
    {
        if (_selected == null) return;
        if (QM.AcceptQuest(_selected))
        {
            var item = _items[_selectedIndex];
            item.SetProgress(0, _selected.targetCount);

            SelectIndex(_selectedIndex);
        }

        var tut = TutorialQuestController.Instance;
        if (tut != null)
        {
            tut.RaiseFlagForTutorial("QT-013", "REPEATABLE_ACCEPTED");
        }
    }

    private void OnClickAcceptAll()
    {
        if (QM == null || _offers.Count == 0) return;

        // 확인한 의뢰 수만큼 반복
        for (int i = 0; i < _offers.Count; i++)
        {
            var def = _offers[i];
            if (def == null) continue;

            // 이미 수락된 의뢰는 패스
            if (QM.Active.ContainsKey(def.questId)) continue;

            if (QM.AcceptQuest(def))
            {
                _items[i].SetProgress(0, def.targetCount);
            }
        }

        // 현재 선택된 의뢰를 기준으로 버튼 상태 갱신
        if (_selectedIndex >= 0) SelectIndex(_selectedIndex);

        var tut = TutorialQuestController.Instance;
        if (tut != null)
        {
            tut.RaiseFlagForTutorial("QT-013", "REPEATABLE_ACCEPTED");
        }
    }

    private void OnClickCompleteSelected()
    {
        if (_selected == null || QM == null) return;

        // 아직 수락하지 않았거나 목표 미완수 시 무시
        if (!QM.Active.TryGetValue(_selected.questId, out var save)) return;
        if (!save.completed) return;

        // 선택한 반복 의뢰 완수
        if (QM.TryTurnIn(_selected.questId))
        {
            // UI에서 진행도 초기화
            _items[_selectedIndex].SetProgress(0, _selected.targetCount);
            SelectIndex(_selectedIndex);
        }
    }

    private void OnClickCompleteAll()
    {
        // 튜토 먼저
        foreach (var kv in QM.Active.ToArray())
        {
            if (!QM.TryGet(kv.Key, out var def)) continue;
            if (def.kind == QuestKind.Tutorial) QM.TryCompleteTutorial(kv.Key);
        }
        // 반복 의뢰
        foreach (var kv in QM.Active.ToArray())
        {
            if (!QM.TryGet(kv.Key, out var def)) continue;
            if (def.kind == QuestKind.Repeatable) QM.TryTurnIn(kv.Key);
        }

        if (_selectedIndex >= 0) SelectIndex(_selectedIndex);
    }

    private void OnClickRerollSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _offers.Count) return;

        // 현재 보드에 떠있는 것 + 이미 수주한 반복퀘를 제외하고 하나 뽑기
        var exclude = new HashSet<string>(_offers.Select(q => q.questId));
        foreach (var a in QM.Active) exclude.Add(a.Key);

        var replacement = DrawOneReplacement(exclude);
        if (replacement == null) return; // 대체 후보가 없으면 무시

        _offers[_selectedIndex] = replacement;

        int progress = 0;
        if (QM.Active.TryGetValue(replacement.questId, out var save)) progress = save.progress;

        _items[_selectedIndex].Rebind(replacement, progress, replacement.targetCount);
        SelectIndex(_selectedIndex); // 상세 갱신
    }
    #endregion

    private QuestData DrawOneReplacement(HashSet<string> exclude)
    {
        // 여유 있게 여러 개 뽑아서 제외 집합으로 필터링
        var pool = QM.DrawRandomRepeatables(offerCount * 4); // 12개 정도
        var cand = pool.FirstOrDefault(q => q != null && !exclude.Contains(q.questId));
        return cand;
    }

    private void RefreshButtonsForSelected()
    {
        if (QM == null || _selected == null)
        {
            // 선택된 퀘스트가 없으면 둘 다 끔
            if (acceptButton) acceptButton.gameObject.SetActive(false);
            if (completeButton) completeButton.gameObject.SetActive(false);
            return;
        }

        bool isAccepted = QM.Active.TryGetValue(_selected.questId, out var save);
        bool isCompleted = isAccepted && save.completed;

        // 완료 마크 텍스트
        if (completeMarkText)
            completeMarkText.gameObject.SetActive(isCompleted);

        // === 1) 완료 상태 ===
        if (isCompleted)
        {
            if (acceptButton)
            {
                acceptButton.gameObject.SetActive(false);      // 수락 버튼 숨김
            }

            if (completeButton)
            {
                completeButton.gameObject.SetActive(true);     // 완료 버튼 표시
                completeButton.interactable = true;            // 클릭 가능
            }
            return;
        }

        // === 2) 수락만 된 상태 (완료 X) ===
        if (isAccepted)
        {
            if (acceptButton)
            {
                acceptButton.gameObject.SetActive(true);       // 수락 버튼은 보이지만
                acceptButton.interactable = false;             // 클릭 불가
            }

            if (completeButton)
            {
                completeButton.gameObject.SetActive(false);    // 완료 버튼 숨김
            }
            return;
        }

        // === 3) 아직 수락하지 않은 상태 ===
        if (acceptButton)
        {
            acceptButton.gameObject.SetActive(true);
            acceptButton.interactable = true;                  // 수락 가능
        }

        if (completeButton)
        {
            completeButton.gameObject.SetActive(false);        // 완료 버튼 숨김
        }

        if (completeMarkText)
            completeMarkText.gameObject.SetActive(false);
    }

    private void HandleQuestLogChanged()
    {
        if (QM == null) return;

        // 리스트 아이템들의 진행도 갱신
        for (int i = 0; i < _offers.Count && i < _items.Count; i++)
        {
            var def = _offers[i];
            if (def == null) continue;

            int progress = 0;
            if (QM.Active.TryGetValue(def.questId, out var save))
            {
                progress = save.progress;
            }

            _items[i].SetProgress(progress, def.targetCount);
        }

        // 현재 선택 중인 항목에 대해 버튼/완료 마크 갱신
        if (_selectedIndex >= 0 && _selectedIndex < _offers.Count)
        {
            _selected = _offers[_selectedIndex];
            RefreshButtonsForSelected();
        }
    }

    private void ClearAllItems()
    {
        foreach (var it in _items) if (it) Destroy(it.gameObject);
        _items.Clear();
    }
}

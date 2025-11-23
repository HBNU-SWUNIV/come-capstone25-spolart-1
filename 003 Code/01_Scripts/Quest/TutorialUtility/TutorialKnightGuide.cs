using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialKnightGuide : MonoBehaviour
{
    [Header("기사단원 NPC ID (대화 이벤트용)")]
    [SerializeField] private int knightNpcId = 3;

    // ─────────────────────────────────────────────────────
    // 퀘스트별 이동 경로

    [System.Serializable]
    public class KnightQuestPath
    {
        [Tooltip("이 경로를 사용할 튜토리얼 퀘스트 ID (예: QT-001)")]
        public string questId;

        [Tooltip("이 퀘스트 진행 중 기사가 따라갈 경로 포인트들")]
        public Transform[] points;
    }

    [Header("퀘스트별 이동 경로 설정")]
    [Tooltip("QT-001, QT-003, QT-005, QT-006, QT-007, QT-008, QT-009 등 퀘스트 ID별로 경로를 지정")]
    [SerializeField] private KnightQuestPath[] questPaths;

    // ─────────────────────────────────────────────────────
    // 튜토리얼 단계별 시작 위치

    [System.Serializable]
    public class KnightSpawnPoint
    {
        [Tooltip("이 위치에 둘 튜토리얼 퀘스트 ID (예: QT-001)")]
        public string questId;
        [Tooltip("해당 튜토리얼 단계에서 기사단원이 처음 있을 위치")]
        public Transform point;
    }

    [Header("튜토리얼 단계별 시작 위치")]
    [Tooltip("어떤 튜토리얼에도 매칭되지 않을 때 사용할 기본 위치 (없으면 현재 위치 유지)")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Tooltip("튜토리얼 퀘스트 ID별 시작 위치")]
    [SerializeField] private KnightSpawnPoint[] spawnPointsByQuest;

    // ─────────────────────────────────────────────────────
    // 이 씬에서의 등장 조건 제어

    [Header("이 씬에서의 등장 조건")]
    [Tooltip("체크하면 allowedQuestIds에 포함된 퀘스트일 때만 이 기사가 등장합니다.")]
    [SerializeField] private bool restrictAppearanceByQuest = false;

    [Tooltip("이 씬에서 기사단원이 등장할 튜토리얼 퀘스트 ID 목록")]
    [SerializeField] private string[] allowedQuestIds;

    /*
     * 예시:
     * - 마을 씬 기사:
     *   restrictAppearanceByQuest = true
     *   allowedQuestIds = ["QT-001", "QT-003", "QT-005", "QT-009"]
     *   → QT-006~QT-008 동안에는 자동으로 숨김 처리됨 (마을에 기사가 없음)
     *
     * - 튜토리얼 던전 씬 기사:
     *   restrictAppearanceByQuest = true
     *   allowedQuestIds = ["QT-006", "QT-007", "QT-008"]
     *   → 던전 튜토리얼일 때만 등장
     */

    // ─────────────────────────────────────────────────────
    // 이동 연출 / 공용 참조

    [Header("이동 연출")]
    [SerializeField] private float moveSpeed = 3f;      // 이동 속도
    [SerializeField] private float waitPoints = 0.2f;   // 각 포인트에서 잠깐 멈추는 시간

    [Header("기타 참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private CircleCollider2D circleCollider;
    [SerializeField] private bool defaultFacesRight = true;

    private Coroutine _moveRoutine;
    private readonly HashSet<string> _movedQuestIds = new(); // 퀘스트당 1번만 이동
    private bool _isHiddenByQuest = false;                   // 튜토리얼 단계에 의해 숨김 상태인지

    private bool _subscribedTownUI = false;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        circleCollider = GetComponentInChildren<CircleCollider2D>();
    }

    private void Awake()
    {
        // 씬에 들어왔을 때, 현재 튜토리얼 단계 기준으로
        // 기사단원의 위치/등장 여부를 먼저 정리
        ApplySpawnForCurrentTutorialStep();
    }

    private void OnEnable()
    {
        TrySubscribeTownUI();
    }

    private void OnDisable()
    {
        if (TownUIManager.Instance != null)
        {
            TownUIManager.Instance.OnNpcTalkEnded -= HandleTalkEnded;
        }
        _subscribedTownUI = false;
    }

    private void Update()
    {
        // TownUIManager가 늦게 생성되는 경우를 대비해서 매 프레임 재시도
        if (!_subscribedTownUI && TownUIManager.Instance != null)
        {
            TrySubscribeTownUI();
        }
    }

    private void TrySubscribeTownUI()
    {
        var town = TownUIManager.Instance;
        if (town == null) return;

        town.OnNpcTalkEnded -= HandleTalkEnded; // 중복 구독 방지
        town.OnNpcTalkEnded += HandleTalkEnded;
        _subscribedTownUI = true;

        Debug.Log("[TutorialKnightGuide] TownUIManager.OnNpcTalkEnded 구독 완료");
    }

    // ─────────────────────────────────────────────────────
    // 게임 재시작/씬 진입 시 초기 위치 + 등장 여부 설정

    private void ApplySpawnForCurrentTutorialStep()
    {
        var dm = DataManager.Instance;
        var qm = QuestManager.Instance;
        if (dm == null || qm == null) return;

        if (dm.IsTutorialCompleted())
        {
            // 튜토리얼이 끝난 뒤에는 자유롭게 두고 싶다면,
            // 여기서 defaultSpawnPoint로 보내거나, 그냥 현 위치 유지.
            if (defaultSpawnPoint != null)
            {
                transform.position = defaultSpawnPoint.position;
            }
            SetHiddenByQuest(false);
            return;
        }

        int step = dm.GetTutorialStep();
        var curDef = qm.GetTutorialByStep(step);
        if (curDef == null || string.IsNullOrEmpty(curDef.questId))
        {
            if (defaultSpawnPoint != null)
                transform.position = defaultSpawnPoint.position;

            SetHiddenByQuest(false);
            return;
        }

        string curQuestId = curDef.questId;

        // 이 씬에서 등장 여부 제어 (마을/던전 구분 등)
        if (restrictAppearanceByQuest && allowedQuestIds != null && allowedQuestIds.Length > 0)
        {
            bool allowed = System.Array.Exists(allowedQuestIds, id => id == curQuestId);
            SetHiddenByQuest(!allowed);

            // 이 씬에서 보여줄 퀘스트가 아니면 위치는 따로 신경쓸 필요 없음
            if (!allowed) return;
        }
        else
        {
            SetHiddenByQuest(false);
        }

        // 현재 튜토리얼 퀘스트에 맞는 시작 위치 찾기
        var spawn = spawnPointsByQuest?.FirstOrDefault(s => s.questId == curQuestId);
        if (spawn != null && spawn.point != null)
        {
            transform.position = spawn.point.position;
        }
        else if (defaultSpawnPoint != null)
        {
            // 매칭이 없으면 기본 위치
            transform.position = defaultSpawnPoint.position;
        }
        // 아무 것도 없으면 현재 위치 유지
    }

    private void SetHiddenByQuest(bool hidden)
    {
        _isHiddenByQuest = hidden;

        if (sr != null) sr.enabled = !hidden;
        if (circleCollider != null) circleCollider.enabled = !hidden;

        // 애니메이터를 꺼두면 Update가 멈춰서
        // 숨김 상태일 때는 굳이 동작할 필요가 없음
        if (animator != null) animator.enabled = !hidden;
    }

    // ─────────────────────────────────────────────────────
    // 대화 종료 → 퀘스트별 경로 이동

    private void HandleTalkEnded(int npcId)
    {
        // 이 씬에서 숨김 상태면 아무 것도 하지 않음
        if (_isHiddenByQuest) return;

        // 기사가 아닌 다른 NPC라면 무시
        if (npcId != knightNpcId) return;

        var dm = DataManager.Instance;
        var qm = QuestManager.Instance;
        if (dm == null || qm == null) return;

        var curDef = qm.GetTutorialByStep(dm.GetTutorialStep());
        if (curDef == null || string.IsNullOrEmpty(curDef.questId)) return;

        string curQuestId = curDef.questId;

        // 이미 이 퀘스트에서 한 번 이동했다면 재이동 X
        if (_movedQuestIds.Contains(curQuestId)) return;

        // 이 퀘스트에 해당하는 이동 경로 찾기
        var pathDef = questPaths?.FirstOrDefault(p => p.questId == curQuestId);
        if (pathDef == null || pathDef.points == null || pathDef.points.Length == 0)
        {
            Debug.Log($"[TutorialKnightGuide] {curQuestId} 에 해당하는 경로가 없습니다. (이동 안 함)");
            return;
        }

        // 실제 이동 시작
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveToPath(pathDef.points));

        _movedQuestIds.Add(curQuestId);
    }

    /// <summary>
    /// 던전 전용 트리거나 플래그에서
    /// "현재 튜토리얼 단계에 맞는 경로로 강제 이동"시키고 싶을 때 호출할 수 있는 함수
    /// </summary>
    public void ForceMoveForCurrentTutorial()
    {
        if (_isHiddenByQuest) return;

        var dm = DataManager.Instance;
        var qm = QuestManager.Instance;
        if (dm == null || qm == null) return;

        var curDef = qm.GetTutorialByStep(dm.GetTutorialStep());
        if (curDef == null || string.IsNullOrEmpty(curDef.questId)) return;

        string curQuestId = curDef.questId;

        if (_movedQuestIds.Contains(curQuestId)) return;

        var pathDef = questPaths?.FirstOrDefault(p => p.questId == curQuestId);
        if (pathDef == null || pathDef.points == null || pathDef.points.Length == 0) return;

        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveToPath(pathDef.points));

        _movedQuestIds.Add(curQuestId);
    }

    // ─────────────────────────────────────────────────────
    // 실제 이동 코루틴

    private IEnumerator MoveToPath(Transform[] pathPoints)
    {
        if (pathPoints == null || pathPoints.Length == 0) yield break;
        if (_isHiddenByQuest) yield break;

        if (circleCollider != null)
            circleCollider.enabled = false;

        if (animator != null)
        {
            animator.enabled = true;            // 혹시 꺼져 있었다면 켠다
            animator.SetBool("IsMove", true);
        }

        Vector3 currentPos = transform.position;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            Transform waypoint = pathPoints[i];
            if (waypoint == null) continue;

            Vector3 targetPos = waypoint.position;

            UpdateSpriteFacing(currentPos, targetPos);

            float distance = Vector3.Distance(currentPos, targetPos);
            float duration = Mathf.Max(0.01f, distance / Mathf.Max(0.01f, moveSpeed));

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                transform.position = Vector3.Lerp(currentPos, targetPos, t);
                yield return null;
            }

            transform.position = targetPos;
            currentPos = targetPos;

            if (waitPoints > 0f)
                yield return new WaitForSeconds(waitPoints);
        }

        if (animator != null)
            animator.SetBool("IsMove", false);

        if (circleCollider != null && !_isHiddenByQuest)
            circleCollider.enabled = true;

        _moveRoutine = null;
    }

    private void UpdateSpriteFacing(Vector3 from, Vector3 to)
    {
        if (sr == null) return;

        Vector3 dir = (to - from).normalized;
        if (Mathf.Abs(dir.x) < 0.01f)
        {
            // 거의 수직 이동이면 방향 유지
            return;
        }

        bool movingRight = dir.x > 0f;

        if (defaultFacesRight)
        {
            // 기본이 오른쪽일 때
            sr.flipX = !movingRight;
        }
        else
        {
            // 기본이 왼쪽일 때
            sr.flipX = movingRight;
        }
    }
}

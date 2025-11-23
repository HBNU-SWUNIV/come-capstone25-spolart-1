using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 결과창
    public struct RunResultLine
    {
        public OreData ore;
        public int count;
        public int price;
        public int total;
    }

    // 상태
    public enum GameState { InLobby, InTown, EnteringDungeon, InDungeon, RunComplete, Result, ReturnToTown, EnterToBoss, InBoss, Dead }
    public GameState State { get; private set; } = GameState.InLobby;

    public enum RunEndReason { Success, Giveup, Death }
    private RunEndReason _lastReason;

    public int clearedFloorThisRun = 0;
    private int earnedMoney = 0;

    public bool IsGamePaused { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsBossDead { get; private set; }

    private float dungeonStartTime;

    [Header("씬 이름")]
    [SerializeField] private string lobbyScene = "MainMenuScene";
    [SerializeField] private string townScene = "TownScene";
    [SerializeField] private string dungeonGenerationScene = "DungeonGenerationScene";
    [SerializeField] private string tutorialDungeonScene = "TutorialDungeonScene";
    [SerializeField] private string bossScene = "Boss";

    [Header("플레이어 스폰")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform player;
    private Transform _spawnPoint;
    [SerializeField] private string spawnPointTag = "PlayerSpawnPoint"; // 태그 이름

    [Header("참조할 SO")]
    [SerializeField] private RunInventory runInventory;
    [SerializeField] private PricingService pricingService;
    // [SerializeField] private EconomyService economyService;

    [Header("Result/UI")]
    [SerializeField] private ResultUIManager resultUI;

    // QT-008 (광물바구니 사용하기) 퀘스트 ID 상수
    private const string BasketQuestId = "QT-008";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
        Time.timeScale = 1f;

        // 씬 로드 콜백 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀔 때마다 태그로 스폰지점을 다시 찾는다
        RebindSpawnPoint();

        if (scene.name == bossScene)
        {
            State = GameState.InBoss;
            OnDungeonEnter();
        }
        else if (scene.name == dungeonGenerationScene || scene.name == tutorialDungeonScene)
        {
            State = GameState.InDungeon;
            OnDungeonEnter();

            TryFullRestorePlayer(); // 던전 입장 시 체력/마나 풀 회복
        }
        else if (scene.name == townScene)
        {
            State = GameState.InTown;
            EnsurePlayerInTown(); // 1. 플레이어를 먼저 스폰/배치합니다.

            // 2. ReturnToTown에서 이동된 후속 처리 로직
            // EnsurePlayerInTown()이 호출된 후라 'player' 변수가 유효합니다.
            if (player != null)
            {
                // 버프 클리어
                DungeonRunBuffManager.Instance.ClearAfterRun(player.gameObject);

                // 인벤토리 클리어 (새 플레이어의 컴포넌트를 직접 찾음)
                var inv = player.GetComponent<Inventory>();
                if (inv != null)
                {
                    inv.ResetAtferRun();
                }

                var status = player.GetComponent<PlayerStatus>();
                if (status != null)
                {
                    status.FullRestore();
                }
            }
            else
            {
                Debug.LogError("[GameManager] Town 씬 로드 후 Player를 찾지 못해 후속 처리를 스킵합니다.");
            }
        }
        else if (scene.name == lobbyScene)
        {
            State = GameState.InLobby;
        }
    }

    private void Update()
    {
        // 게임 오버 상태에서 아무 입력 감지 → 로비로 리셋 복귀
        if (IsGameOver && AnyInputPressed())
        {
            ProcessGameOverReturnToTown();
        }

        // [추가] 게임 플레이 중(던전, 보스 등)일 때만 ESC로 일시정지 가능
        // 로비나 로딩 중에는 작동하지 않도록 예외 처리
        if (State == GameState.InDungeon || State == GameState.InBoss || State == GameState.InTown)
        {
            // New Input System의 키보드 ESC 체크
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    public void LobbyToTown()
    {
        if (State != GameState.InLobby) return;

        ResetRunData();
        IsGameOver = false;
        ResumeGame();

        SceneManager.LoadScene(townScene);
        State = GameState.InTown;
    }

    // 로비 UI의 "게임 시작" 버튼에서 이 함수 연결
    public void StartGameFromTown()
    {
        // 로비 상태에서만 시작
        if (State != GameState.InTown) return;

        // 런/플래그 초기화
        ResetRunData();
        IsGameOver = false;
        ResumeGame();

        EnterDungeon();
    }

    public void EnterDungeon()
    {
        if (State != GameState.InTown) return;

        State = GameState.EnteringDungeon;
        runInventory.Clear();

        bool useTutorialDungeon = ShouldUseTutorialDungeon();

        //// SceneManager.LoadScene(dungeonGenerationScene);
        //LoadSceneManager.Instance.LoadSceneWaitGenerator(dungeonGenerationScene);

        //DungeonRunBuffManager.Instance.ApplyAllTo(player.gameObject);

        if (useTutorialDungeon && !string.IsNullOrEmpty(tutorialDungeonScene))
        {
            Debug.Log("[GameManager] 튜토리얼 던전으로 진입합니다.");
            // 튜토리얼은 고정 맵이라고 가정 → 일반 LoadScene
            LoadSceneManager.Instance.LoadScene(tutorialDungeonScene);
        }
        else
        {
            Debug.Log("[GameManager] 랜덤 생성 던전으로 진입합니다.");
            // 기존처럼 절차적 생성 던전에 WaitGenerator 사용
            LoadSceneManager.Instance.LoadSceneWaitGenerator(dungeonGenerationScene);
        }

        State = GameState.InDungeon;
        OnDungeonEnter();
    }

    public void EndRun(RunEndReason reason)
    {
        if (State != GameState.InDungeon && State != GameState.InBoss) return;

        State = GameState.RunComplete;
        _lastReason = reason;
        float playTime = GetDungeonPlayTime();

        earnedMoney = (reason == RunEndReason.Death) ? 0 : pricingService.EvaluateTotal(runInventory.Stacks, clearedFloorThisRun);

        OpenResultUI();
    }

    public void EnterBoss()
    {
        Debug.Log("보스전 진입");

        // 상태가 InTown이 아니라면 리턴
        if (State != GameState.InDungeon)
        {
            return;
        }
        State = GameState.EnterToBoss;

        // LoadSceneManager의 전역 함수로 씬 전환 호출
        LoadSceneManager.Instance.LoadScene(bossScene);
        // SceneManager.LoadScene(bossScene);
    }

    private void OpenResultUI()
    {
        if (resultUI != null)
        {
            resultUI.gameObject.SetActive(true);
            resultUI.Show();
        }

        PauseGame();
        State = GameState.Result;
    }

    // 결과창에서 "확인" 버튼이 눌렸을 때 연결
    public void OnResultConfirmed()
    {
        var eco = EconomyService.Instance ?? FindObjectOfType<EconomyService>();
        if (eco == null) { Debug.LogError("[GM] EconomyService 없음"); return; }
        if (runInventory == null) { Debug.LogError("[GM] runInventory 미할당"); return; }

        if (_lastReason == RunEndReason.Death)
        {
            runInventory.Clear();
        }
        else
        {
            EconomyService.Instance.AddMoney(earnedMoney);
            runInventory.Clear();
        }
        // economyService.Save();

        ReturnToTown();
    }

    private void ReturnToTown()
    {
        State = GameState.ReturnToTown;

        // 씬이 로드되기 전에 게임 시간을 원래대로 돌려놓습니다.
        ResumeGame();

        // 씬 로드만 호출합니다.
        LoadSceneManager.Instance.LoadScene(townScene);
    }

    public void GameOver()
    {
        // 게임 오버 UI만 띄우고, 입력 대기 → Update()에서 리셋 처리
        State = GameManager.GameState.Dead;
        IsGameOver = true;
        if (resultUI != null) resultUI.ShowGameoverUI();
        PauseGame(); // 움직임 정지
    }

    private void ProcessGameOverReturnToTown()
    {
        ResetRunData(); // 런 데이터(획득 광물, 클리어 층 등) 초기화
        IsGameOver = false; // 게임오버 상태 플래그 해제

        // ReturnToTown() 함수가 씬 로드, 게임 재개(ResumeGame),
        // 버프 클리어, 인벤토리 클리어를 모두 처리합니다.
        ReturnToTown();
    }

    // === 리셋 & 초기화 ===
    private void ResetRunData()
    {
        _lastReason = RunEndReason.Success;
        clearedFloorThisRun = 0;
        earnedMoney = 0;
        runInventory.Clear();
        dungeonStartTime = 0f;

        if (InventoryStateHandle.Runtime != null)
        {
            InventoryStateHandle.Runtime.Clear();
        }
    }

    private void ResetAndReturnToLobby()
    {
        ResetRunData();
        IsGameOver = false;
        ResumeGame();

        State = GameState.Dead;
        SceneManager.LoadScene(lobbyScene);
        State = GameState.InLobby;
    }

    private void TryFullRestorePlayer()
    {
        PlayerStatus status = null;

        if (player != null)
        {
            status = player.GetComponent<PlayerStatus>();
        }

        if (status == null)
        {
            status = FindObjectOfType<PlayerStatus>();
        }

        if (status != null)
        {
            status.FullRestore();
        }
        else
        {
            Debug.LogWarning("[GameManager] 전체 회복 시도 실패, PlayerStatus 탐지 실패");
        }
    }

    // === 보조 ===
    public void AddOreToRun(OreData ore, int count)
    {
        runInventory.Add(ore, count);

        // 퀘스트 이벤트 알림
        if (ore != null && !string.IsNullOrEmpty(ore.OreId))
        {
            QuestEvents.ReportOreAcquired(ore.OreId, count);
        }
    }
        

    private int EvaluateSubtotal(OreData ore, int count)
    {
        if (ore == null || count <= 0) return 0;
        var tmp = new List<RunInventory.Entry> { new RunInventory.Entry { ore = ore, count = count } };
        return pricingService.EvaluateTotal(tmp, clearedFloorThisRun);
    }

    public List<RunResultLine> BuildRunResultLines(out int grandTotal)
    {
        grandTotal = 0;
        var lines = new List<RunResultLine>();

        foreach (var e in runInventory.Stacks)
        {
            if (e.ore == null || e.count <= 0) continue;
            int subtotal = EvaluateSubtotal(e.ore, e.count);
            int unit = (e.count > 0) ? Mathf.RoundToInt((float)subtotal / e.count) : 0;

            lines.Add(new RunResultLine
            {
                ore = e.ore,
                count = e.count,
                price = unit,
                total = subtotal
            });
            grandTotal += subtotal;
        }
        return lines;
    }

    // 타운 씬에 플레이어 없으면 스폰
    private void EnsurePlayerInTown()
    {
        // 먼저 씬에 Player가 이미 있는지 확인
        if (player == null) player = FindExistingPlayer();

        Vector3 spawnPos = GetSpawnPositionOrDefault();

        // 없으면 생성
        if (player == null && playerPrefab != null)
        {
            var go = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            player = go.transform;
        }
        else if (player != null)
        {
            // 있으면 위치만 이동
            player.transform.position = spawnPos;
        }
    }

    // 씬에서 존재하는 플레이어 Transform 찾기 (Tag 또는 컴포넌트 기반)
    private Transform FindExistingPlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;

        return null;
    }

    private void RebindSpawnPoint()
    {
        _spawnPoint = null;
        var go = GameObject.FindGameObjectWithTag(spawnPointTag);
        if (go != null) _spawnPoint = go.transform;
    }

    private Vector3 GetSpawnPositionOrDefault()
    {
        // 스폰 포인트가 있으면 그 위치, 없으면 (0,0,0)
        return _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
    }

    public void PauseGame()
    {
        IsGamePaused = true;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        IsGamePaused = false;
        Time.timeScale = 1f;
    }

    public void OnDungeonEnter()
    {
        dungeonStartTime = Time.time;
    }

    public void ConvertOresInRunToMoney()
    {
        // 광물 정산을 위해 PricingService를 사용하여 총 금액을 계산합니다.
        int moneyToEarn = pricingService.EvaluateTotal(runInventory.Stacks, clearedFloorThisRun);

        // 계산된 돈을 EconomyService에 추가합니다.
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.AddMoney(moneyToEarn);
        }
        else
        {
            Debug.LogError("EconomyService 인스턴스를 찾을 수 없습니다. 돈을 정산할 수 없습니다.");
            return;
        }

        // 광물을 돈으로 바꾼 후 현재 인벤토리를 정리
        var inv = player.GetComponent<Inventory>();
        if (inv != null)
        {
            inv.ResetAtferRun();
        }

        // 정산 데이터 정리
        if (runInventory != null)
        {
            runInventory.Clear();
        }

        Debug.Log($"중간 정산: {moneyToEarn}원을 획득하고 광물 인벤토리를 비웠습니다.");
    }

    public float GetDungeonPlayTime() => Time.time - dungeonStartTime;
    public RunEndReason GetDungeonEndReason() => _lastReason;
    public void RegisterResultUI(ResultUIManager ui) => resultUI = ui;

    // === 입력 감지 (키보드/패드) ===
    private bool AnyInputPressed()
    {
        // Keyboard
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;

        // Mouse (선택사항: 클릭만 감지)
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame ||
                                      Mouse.current.rightButton.wasPressedThisFrame ||
                                      Mouse.current.middleButton.wasPressedThisFrame)) return true;

        // 기타 디바이스 추가 가능
        return false;
    }

    #region 튜토리얼/랜덤 던전 결정
    private bool ShouldUseTutorialDungeon()
    {
        var qm = QuestManager.Instance;
        var dm = DataManager.Instance;

        // 퀘스트 시스템이 없다면 일단 튜토리얼로 보냄 (개발 편의)
        if (qm == null)
        {
            Debug.LogWarning("[GameManager] QuestManager가 없어 튜토리얼 던전으로 진입합니다.");
            return true;
        }

        // 1) Active 퀘스트에서 QT-008 상태 직접 확인
        if (qm.Active != null && qm.Active.TryGetValue(BasketQuestId, out QuestSave save))
        {
            // completed == true면 목표 달성 상태(COMPLETE) → 이후부터 랜덤 던전
            if (save.completed)
            {
                Debug.Log("[GameManager] QT-008이 COMPLETE 상태라 랜덤 던전으로 전환합니다.");
                return false;
            }

            // 수주되어 있지만 COMPLETE 아니면 아직 튜토리얼 던전 사용
            Debug.Log("[GameManager] QT-008 진행 중이라 튜토리얼 던전 유지.");
            return true;
        }

        // 2) Active에 없을 경우: 아직 도달 전이거나 이미 턴인 후 다음 스텝으로 넘어간 상태
        if (dm == null)
        {
            // DataManager 없으면 보수적으로 튜토리얼 유지
            Debug.LogWarning("[GameManager] DataManager가 없어 QT-008 스텝을 판정할 수 없습니다. 튜토리얼 던전 유지.");
            return true;
        }

        int basketIndex = GetTutorialQuestIndex(qm, BasketQuestId);
        if (basketIndex < 0)
        {
            // QT-008을 튜토리얼에서 찾지 못하면, 더 진행된 상태로 보고 랜덤으로 보내도 됨
            Debug.LogWarning("[GameManager] 튜토리얼 목록에서 QT-008을 찾지 못했습니다. 랜덤 던전으로 진입합니다.");
            return false;
        }

        int currentStep = dm.GetTutorialStep();

        // currentStep > basketIndex  : QT-008 턴인까지 끝나서 다음 스텝으로 넘어간 상태 → 랜덤
        // currentStep <= basketIndex : 아직 QT-008 전이거나 도중 → 튜토리얼
        bool useTutorial = currentStep <= basketIndex;

        Debug.Log($"[GameManager] 튜토리얼 스텝 판정: currentStep={currentStep}, basketIndex={basketIndex}, useTutorial={useTutorial}");

        return useTutorial;
    }

    private int GetTutorialQuestIndex(QuestManager qm, string questId)
    {
        if (qm == null || string.IsNullOrEmpty(questId)) return -1;

        int step = 0;
        while (true)
        {
            var def = qm.GetTutorialByStep(step);
            if (def == null) break;

            if (def.questId == questId)
                return step;

            step++;
        }

        return -1;
    }
    #endregion


    // 긴급 패치
    [Header("일시정지 UI")]
    [SerializeField] private PauseUIManager pauseUI;
    [SerializeField] private string bootstrapScene = "Bootstrap";

    // [추가] 일시정지 토글
    // [추가] 외부(PauseUIManager)에서 호출하여 등록하는 함수
    public void RegisterPauseUI(PauseUIManager ui)
    {
        pauseUI = ui;
        // 씬 전환 시 일시정지 상태였다면 해제하거나 UI 상태를 동기화할 수도 있음
    }

    public void GoToBootstrap()
    {
        // 1. 데이터 및 상태 기본 초기화
        ResetRunData();
        IsGameOver = false;
        ResumeGame(); // 시간 정지 해제
        State = GameState.InLobby;

        // 2. [추가] 좀비처럼 살아남는 Player(PlayerNeverDie) 파괴
        // PlayerNeverDie 컴포넌트를 찾거나, "Player" 태그로 찾아서 파괴
        var playerSurvivor = FindObjectOfType<PlayerNeverDie>();
        if (playerSurvivor != null)
        {
            Destroy(playerSurvivor.gameObject);
        }
        else
        {
            // 혹시 컴포넌트가 없더라도 Player 태그가 있다면 파괴 (안전장치)
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) Destroy(playerObj);
        }

        // 3. [추가] 기존에 살아있던 Bootstrap 오브젝트 파괴 및 정적 변수 리셋
        // (이걸 안 하면 부트스트랩 씬이 로드되어도 start 로직이 안 돔)
        var oldBootstrap = FindObjectOfType<Bootstrap>();
        if (oldBootstrap != null)
        {
            Destroy(oldBootstrap.gameObject);
        }
        Bootstrap.ResetStaticState(); // static 변수 false로 변경

        // 4. 기존 DDOL UI 파괴 (이전 단계에서 추가한 내용)
        DDOLUiRoot.DestroyRoot();

        Debug.Log($"[GameManager] 모든 DDOL 객체 정리 완료. {bootstrapScene} 로드");

        // 5. 부트스트랩 씬 로드
        UnityEngine.SceneManagement.SceneManager.LoadScene(bootstrapScene);
    }

    // [수정] TogglePause 함수 안전장치 추가
    public void TogglePause()
    {
        // UI가 연결되지 않았다면 로그를 띄우고 리턴 (멈춤 현상 방지)
        if (pauseUI == null)
        {
            Debug.LogWarning("[GameManager] PauseUI가 연결되지 않았습니다! (Hierarchy에 PauseUI가 없거나 등록 실패)");

            // 만약 UI가 없는데 시간이 멈췄다면 다시 풀어서 움직이게 해줌 (버그 방지)
            if (IsGamePaused) ResumeGame();
            return;
        }

        if (IsGamePaused)
        {
            ResumeGame();
            pauseUI.Hide();
        }
        else
        {
            PauseGame();
            pauseUI.Show();
        }
    }

    // [추가] 던전 재시작 (Restart)
    public void RetryDungeon()
    {
        Debug.Log("[GameManager] 던전 재시작");

        // 1. 현재 런 데이터 초기화
        ResetRunData();

        // 2. 상태 강제 조정 (EnterDungeon 조건을 맞추기 위함)
        State = GameState.InTown;

        // 3. 던전 진입 로직 재호출
        EnterDungeon();
    }

    // [추가] 로비로 이동 (To Lobby)
    public void GoToLobby()
    {
        // 기존 private 함수였던 ResetAndReturnToLobby 로직 활용
        ResetRunData();
        IsGameOver = false;
        ResumeGame();

        State = GameState.InLobby;
        SceneManager.LoadScene(lobbyScene);
    }
}

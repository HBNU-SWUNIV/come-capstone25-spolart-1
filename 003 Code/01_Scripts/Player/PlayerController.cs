using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    /*
     공격 및 채광 속도 설정해 애니메이션과 동기화하여 정상적으로 기능하도록 구현
     
     적의 근접공격 범위 안에 있을 때, 타이밍에 맞춰 좌클릭을 누르면 패링
     
     */

    [Header("마우스 에임")]
    [SerializeField] private MouseAim mouseAim;

    [Header("전투/채광 컴포넌트")]
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerMining mining;

    [Header("플레이어 스탯")]
    [SerializeField] private float baseMoveSpeed = 5f;

    [Header("레이캐스트")]
    [SerializeField] private float miningRange;
    [SerializeField] private LayerMask miningMask;
    [SerializeField] private Transform rayOrigin;

    private Vector2 moveInput; // Input System을 위한 변수
    private Vector2 lastDirection;

    private Rigidbody2D rb;
    private Animator animator;

    // 참조하는 스크립트
    private PlayerParry playerParry;
    private Inventory playerInventory;
    private BuffSystem buff;
    private ActionLock _lock;

    [SerializeField] private InventoryUIManager inventoryUIManager;

    private bool isMoving;

    // NPC 대화 변수
    private readonly HashSet<NPCInteractable> npcInRange = new HashSet<NPCInteractable>();
    private NPCInteractable currentTargetNPC; // 현재 상호작용 대상

    // 쿨타임 타임스탬프
    private float nextAttackTime;
    private float nextMiningTime;

    private Camera _cam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerParry = GetComponent<PlayerParry>();
        playerInventory = GetComponent<Inventory>();
        buff = GetComponent<BuffSystem>();
        _lock = GetComponent<ActionLock>();

        if (combat == null) combat = GetComponent<PlayerCombat>();
        if (mining == null) mining = GetComponent<PlayerMining>();

        RebindSceneReferences();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnMove(InputValue value) 
    {
        if (CutSceneManager.Instance?.isCutScenePlaying == true) return;

        moveInput = value.Get<Vector2>();
        if (moveInput != Vector2.zero)
        { 
            lastDirection = moveInput.normalized;
        }

        isMoving = moveInput.sqrMagnitude > 0.01f; // 이동 중인지 확인 후 bool 값 반환
        animator.SetBool("IsMoving", isMoving);
    }

    // Input System 이용해 B키 눌렀을 때 동작하는 함수
    public void OnOpenInventory()
    {
        if (TownUIManager.Instance.IsBusy)
        {
            Debug.Log("TownUI가 열려있어 인벤토리 오픈 실패");
            return;
        }
        inventoryUIManager?.TogglePanel();
    }

    // 물리 연산은 FixedUpdate에서 진행
    private void FixedUpdate()
    {
        // 공격/채광 등으로 액션 락이 걸려 있으면 즉시 정지
        if (_lock != null && _lock.IsLocked)
        {
            rb.linearVelocity = Vector2.zero; // 물리적으로도 완전히 정지
            return;
        }

        Vector2 finalMoveSpeed = moveInput.normalized * baseMoveSpeed * buff.MoveSpeedMul * buff.MoveSpeedSlowMul * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + finalMoveSpeed);

        if (isMoving)
        {
            AudioManager.instance.PlayFootstep();
        }
    }

    public void OnInteract()
    {
        // 인벤토리가 열려있으면 상호작용 불가능
        if (inventoryUIManager.IsOpen)
        {
            Debug.Log("인벤토리가 열려있어 TownUI와 상호작용 불가능");
            return;
        }

        // 대화 진행 중이라면 대화만 진행
        if (TownUIManager.Instance.intalk)
        {
            TownUIManager.Instance.Interact_InTalk();
            return;
        }

        // 이미 마을 UI가 열려 있으면 다른 대화창을 더 이상 열지 않음
        if (TownUIManager.Instance.IsBusy)
        {
            return;
        }

        if (currentTargetNPC != null)
        {
            // currentTargetNPC에서 NPC_Data 컴포넌트를 가져와 대화 시작
            TownUIManager.Instance.StartConversation(currentTargetNPC.GetComponent<NPC_Data>());
        }
    }

    /* NPC 대화를 위한 트리거 함수 */
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("InteractRange"))
        {
            var npcInteractable = collision.GetComponentInParent<NPCInteractable>();
            if (npcInteractable == null) return;

            npcInteractable.SetPlayerInRange(true);

            npcInRange.Add(npcInteractable);
            UpdateCurrentTargetNPC();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("InteractRange"))
        {
            var npcInteractable = collision.GetComponentInParent<NPCInteractable>();
            if (npcInteractable == null) return;

            // 1) 나가기 전, 현재 대화중인 NPC였는지 체크
            bool wasCurrent = (npcInteractable == currentTargetNPC);

            npcInteractable.SetPlayerInRange(false);

            // 2) 범위 목록에서 제거하고 타겟 재선정
            npcInRange.Remove(npcInteractable);

            // 3) 자동 닫기 시도
            TryAutoCloseTownUI(wasCurrent);

            // 4) 타겟 재선정
            UpdateCurrentTargetNPC();            
        }
    }

    private void TryAutoCloseTownUI(bool exitedWasCurrent)
    {
        var tui = TownUIManager.Instance;
        if (tui == null) return;

        // UI가 열려있지 않으면 할 일 없음
        if (!tui.IsBusy) return;

        // 현재 대화 중인 NPC에서 벗어났거나, 더 이상 범위 내 NPC가 없을 때만 닫기
        if (exitedWasCurrent || npcInRange.Count == 0)
        {
            tui.CloseAllTownUI();
        }
    }

    private void UpdateCurrentTargetNPC()
    {
        // 1. HashSet에서 파괴되었거나 null인 항목을 제거
        // List의 RemoveAll(predicate) 대신 RemoveWhere를 사용합니다.
        npcInRange.RemoveWhere(item => item == null);

        // 2. 범위 내에 남은 NPC가 없으면 타겟을 null로 설정
        if (npcInRange.Count == 0)
        {
            currentTargetNPC = null;
            return;
        }

        // 3. 가장 가까운 대상을 선택 (LINQ의 OrderBy는 HashSet에서도 동일하게 작동)
        currentTargetNPC = npcInRange
            .OrderBy(npc => Vector2.SqrMagnitude((Vector2)npc.transform.position - (Vector2)transform.position))
            .First();
    }

    private void RebindSceneReferences()
    {
        // 1) InventoryUIManager 다시 찾기 (씬 오브젝트)
        if (inventoryUIManager == null)
        {
            inventoryUIManager = FindObjectOfType<InventoryUIManager>();
            if (inventoryUIManager == null)
            {
                Debug.LogWarning("[PlayerController] InventoryUIManager를 씬에서 찾지 못했습니다.");
            }
        }

        // 2) MouseAim이 있으면 카메라 재바인딩도 시도
        if (mouseAim != null)
        {
            mouseAim.RebindCamera();
        }
    }

    // 외부에서 수동 주입도 가능하도록 유지
    public void SetInventoryUIManager(InventoryUIManager ui)
    {
        inventoryUIManager = ui;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindSceneReferences();
    }

    private void OnSceneUnloaded(Scene unloadedScene)
    {
        npcInRange.Clear();
    }

    /* 테스트용 */
    public void OnClose()
    {
        GameManager.Instance.EndRun(GameManager.RunEndReason.Giveup);
    }
}

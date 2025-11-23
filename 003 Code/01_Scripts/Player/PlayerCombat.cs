using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private MouseAim mouseAim;      // 방향/원점 제공
    [SerializeField] private Animator animator;      // 공격 애니메이션 트리거
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private PlayerStatus status;
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private BuffSystem buff;
    [SerializeField] private ActionLock actionLock;

    private InventoryUIManager inventoryUI;

    [Header("Overlap Box 설정")]
    [Tooltip("공격 박스 가로x세로 크기")]
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.6f, 1.5f);
    [Tooltip("원점에서 얼마나 전방으로 낼지(박스 중심)")]
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask attackMask;

    [Header("SFX")]
    [SerializeField] private AudioClip swingClip;        // 휘두르는 소리
    [SerializeField] private AudioClip hitClip;          // 타격 확정음(한 번만)
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private Vector2 swingPitch = new Vector2(0.95f, 1.05f);
    [SerializeField] private Vector2 hitPitch = new Vector2(0.95f, 1.05f);
    
    private Vector2 AimDir => mouseAim != null ? mouseAim.Direction : Vector2.right;

    private Transform Origin => mouseAim != null && mouseAim.Origin != null ? mouseAim.Origin : transform;

    [SerializeField] private float attackTimer;
    private bool hitWindowOpen = false;

    private void Awake()
    {
        if (!actionLock) actionLock = GetComponent<ActionLock>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
        if (!status) status = GetComponent<PlayerStatus>();
        if (!equipment) equipment = GetComponent<PlayerEquipment>();

        inventoryUI = FindFirstObjectByType<InventoryUIManager>();
    }

    private void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    private bool IsUIBlocking()
    {
        // 1. 마을 UI가 열려있는지 확인 (기존 TownUIManager 활용)
        if (TownUIManager.Instance != null && TownUIManager.Instance.IsBusy)
            return true;

        // 2. 인벤토리 UI가 열려있는지 확인 (InventoryUIManager.IsOpen 활용)
        // (참조가 유실되었을 경우 대비해 다시 찾기)
        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryUIManager>();

        if (inventoryUI != null && inventoryUI.IsOpen)
            return true;

        return false;
    }

    /// Input System에서 호출
    public void OnAttack()
    {
        if (IsUIBlocking()) return;

        if (actionLock != null && actionLock.IsLocked) return;

        if (attackTimer > 0f) return;

        float cd = status ? status.FinalAttackCooldown : 0.5f;
        attackTimer = cd;
        hitWindowOpen = true;

        if (animator)
        {
            // 공격 시도 → 이동 잠금
            actionLock?.Lock("attack");

            animator.SetTrigger("IsAttack");
        }
    }

    /// 애니메이션의 히트 프레임에서 이벤트로 호출
    public void DoAttackHit()
    {
        if (!hitWindowOpen) return;
        hitWindowOpen = false;

        Vector2 origin = Origin.position;
        Vector2 aim = AimDir.sqrMagnitude > 0.0001f ? AimDir : Vector2.right;

        // 버프 적용
        int dmg = status ? status.FinalAttackPower : 1;
        dmg = GetComponent<BuffSystem>()?.ModifyDealtDamage(dmg) ?? dmg;

        // 크리티컬 확률 로드
        float critChance = status ? status.FinalAttackCritChance : 0f;
        if (buff != null) critChance = Mathf.Clamp01(critChance + Mathf.Max(0f, buff.CritChanceAdd));

        // 크리티컬 판정 및 데미지 증가
        bool isCrit = Random.value < critChance;
        if (isCrit) dmg = Mathf.RoundToInt(dmg * 1.5f);

        Vector2 center = origin + aim * attackRange;
        float angleZ = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        var cols = Physics2D.OverlapBoxAll(center, attackBoxSize, angleZ, attackMask);

        int hitCount = 0;
        var dedup = new HashSet<GameObject>();

        foreach (var c in cols)
        {
            if (!c) continue;
            var go = c.attachedRigidbody ? c.attachedRigidbody.gameObject : c.gameObject;
            if (!dedup.Add(go)) continue;
            if (go == gameObject) continue;

            bool damagedSomething = false;

            var monster = c.GetComponent<Monster>();
            if (monster != null)
            {
                monster.GotHit(dmg, isCrit);
                damagedSomething = true;
            }

            var boss = c.GetComponent<Boss>();
            if (boss != null)
            {
                boss.GotHit(dmg, isCrit);
                damagedSomething = true;
            }

            // 실제로 몬스터/보스에게 데미지를 줬을 때만 카운트 & 내구도 소모
            if (damagedSomething)
            {
                hitCount++;
                equipment.UseWeapon();
            }
        }

        // 몬스터/보스를 실제로 맞췄을 때만 소리 재생
        if (hitCount > 0)
        {
            PlayHitSfxOnce();
        }
    }


    // 애니메이션의 마지막 키프레임에 이벤트 등록
    public void OnAttackEnd()
    {
        actionLock?.Unlock("attack");
    }

    #region 사운드
    public void PlaySwingSfx()
    {
        Debug.Log("[PlayerCombat] PlaySwingSfx 호출");

        if (!sfxSource || !swingClip) return;

        float p = Random.Range(swingPitch.x, swingPitch.y);
        float prev = sfxSource.pitch;
        sfxSource.pitch = p;
        sfxSource.PlayOneShot(swingClip, sfxVolume);
        sfxSource.pitch = prev;
    }

    private void PlayHitSfxOnce()
    {
        if (!hitClip) return;

        if (sfxSource)
        {
            float p = Random.Range(hitPitch.x, hitPitch.y);
            float prev = sfxSource.pitch;
            sfxSource.pitch = p;
            sfxSource.PlayOneShot(hitClip, sfxVolume);
            sfxSource.pitch = prev;
        }
        else
        {
            AudioSource.PlayClipAtPoint(hitClip, transform.position, sfxVolume);
        }
    }
    #endregion

#if UNITY_EDITOR
    private void DrawAttackGizmosInternal(bool selected)
    {
        if (!showAttackGizmos) return;
        if (Origin == null) return;

        // 조준 방향 및 박스 중심, 회전
        Vector2 aim = Application.isPlaying ? AimDir : Vector2.right;
        Vector2 center = (Vector2)Origin.position + aim * attackRange;
        float angleZ = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        // 진행 방향 라인/화살표
        Gizmos.color = dirLineColor;
        Gizmos.DrawLine(Origin.position, (Vector2)Origin.position + aim.normalized * (attackRange + dirLineLength));
        // 간단한 화살표 머리
        Vector2 tip = (Vector2)Origin.position + aim.normalized * (attackRange + dirLineLength);
        Vector2 left = Quaternion.Euler(0, 0, 150f) * (Vector3)aim.normalized * 0.2f;
        Vector2 right = Quaternion.Euler(0, 0, -150f) * (Vector3)aim.normalized * 0.2f;
        Gizmos.DrawLine(tip, tip + left);
        Gizmos.DrawLine(tip, tip + right);

        // 회전행렬로 박스 정렬
        var prevMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angleZ), Vector3.one);

        // 채워진 박스
        Gizmos.color = gizmoFillColor;
        Gizmos.DrawCube(Vector3.zero, attackBoxSize);

        // 테두리
        Gizmos.color = selected ? Color.yellow : gizmoLineColor;
        Gizmos.DrawWireCube(Vector3.zero, attackBoxSize);

        // 원점 복구
        Gizmos.matrix = prevMatrix;

        // 라벨(선택 시만)
#if UNITY_EDITOR
        if (selected)
        {
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(center + Vector2.up * 0.15f, $"AttackBox\n{attackBoxSize.x:F2} x {attackBoxSize.y:F2}");
        }
#endif
    }

    // 선택 안 해도 보이게(옵션)
    private void OnDrawGizmos()
    {
        if (showEvenWhenUnselected)
            DrawAttackGizmosInternal(selected: false);
    }

    // 선택 시 강조 테두리/라벨
    private void OnDrawGizmosSelected()
    {
        DrawAttackGizmosInternal(selected: true);
    }
#endif

    [Header("디버그/기즈모")]
    [SerializeField] private bool showAttackGizmos = true;         // 기즈모 표시 ON/OFF
    [SerializeField] private bool showEvenWhenUnselected = true;   // 선택 안 해도 표시
    [SerializeField] private Color gizmoFillColor = new Color(1f, 0.2f, 0.2f, 0.15f);
    [SerializeField] private Color gizmoLineColor = new Color(1f, 0.2f, 0.2f, 0.85f);
    [SerializeField] private Color dirLineColor = new Color(1f, 1f, 1f, 0.7f);
    [SerializeField] private float dirLineLength = 0.75f;


}

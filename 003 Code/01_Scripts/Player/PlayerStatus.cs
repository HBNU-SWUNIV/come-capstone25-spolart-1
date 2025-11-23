using System;
using UnityEngine;
using System.Collections;

/*
 플레이어의 상태를 관리하는 스크립트
 체력 및 마나, 스탯 등
 */
public class PlayerStatus : MonoBehaviour
{
    [Header("플레이어 스탯")]
    [SerializeField] private float baseMaxHP;     // 기본 최대 체력
    [SerializeField] private float baseMaxMP;       // 기본 최대 마나
    [SerializeField] private float strength;    // 근력 : 공격력 및 채광력에 영향
    [SerializeField] private float handy;       // 손재주 : 채광속도에 영향

    [Header("자동 회복 설정")]
    [Tooltip("초당 회복되는 체력 양")]
    [SerializeField] private float hpRegenPerSecond = 1f;
    [Tooltip("초당 회복되는 마나 양")]
    [SerializeField] private float mpRegenPerSecond = 0.5f;

    [Header("의존성")]
    [SerializeField] private PlayerEquipment equipment;
    [SerializeField] private BuffSystem buffs;
    [SerializeField] private HitText hitText;

    [Header("참조")]
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource sfxSource;   // 피격 사운드 재생용

    [Header("피격 설정")]
    [SerializeField] private float invincibleDuration = 1f; // 무적 시간
    [SerializeField] private AudioClip damagedClip;
    [SerializeField] private float damagedVolume = 1f;

    [Header("피격 연출")]
    [Tooltip("첫 번째 강한 번쩍 색상")]
    [SerializeField] private Color hitFlashColor1 = Color.white;
    [Tooltip("두 번째 약한 번쩍 색상 (예: 살짝 붉게)")]
    [SerializeField] private Color hitFlashColor2 = new Color(1f, 0.7f, 0.7f, 1f);
    [SerializeField] private float hitFlashInterval = 0.1f;  // 각 단계 유지 시간(초

    private bool isInvincible = false;
    private float invincibleTimer = 0f;

    private SpriteRenderer sr;
    private Color originColor = Color.white;

    private Coroutine hitFlashCo; // 피격 코루틴

    public float MaxHP { get; private set; }
    public float MaxMP { get; private set; }
    public float CurrentHP { get; private set; }
    public float CurrentMP { get; private set; }

    // 체력 및 마나 변동 이벤트
    public event Action<float, float> OnHPChanged; // current, max
    public event Action<float, float> OnMPChanged;
    public event Action OnStatsChanged; // 상태창 UI에서 사용할 이벤트

    private void Awake()
    {
        if (!equipment) equipment = GetComponent<PlayerEquipment>();
        if (!buffs) buffs = GetComponent<BuffSystem>();
        if (!animator) animator = GetComponent<Animator>();

        MaxHP = baseMaxHP;
        MaxMP = baseMaxMP;

        // 플레이어의 현재 체력 및 마나 기본 값으로 설정
        CurrentHP = Mathf.Clamp(CurrentHP <= 0f ? MaxHP : CurrentHP, 0f, MaxHP);
        CurrentMP = Mathf.Clamp(CurrentMP <= 0f ? MaxMP : CurrentMP, 0f, MaxMP);

        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            originColor = sr.color;
        }
    }

    private void Start()
    {
        // HUD 초기 동기화
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnMPChanged?.Invoke(CurrentMP, MaxMP);
    }

    private void Update()
    {
        HandleAutoRegen();

        // 무적 타이머 처리
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                SetSpriteInvincible(false);
            }
        }
    }

    #region === 최종 스탯 계산 ===

    private int StrengthAttackBonus => Mathf.RoundToInt(strength);
    private int StrengthMiningBonus => Mathf.RoundToInt(strength * 0.5f);

    private float AttackSpeedMul => (buffs ? buffs.AttackSpeedMul : 1f);
    private float MiningSpeedMul => (buffs ? buffs.MiningSpeedMul : 1f);
    private float MoveSpeedMul => (buffs ? buffs.MoveSpeedMul * buffs.MoveSpeedSlowMul : 1f);
    private float DoubleDropChange => (buffs ? buffs.DoubleDropChance : 0f);

    // --- 공격 ---
    public int FinalAttackPower => Mathf.Max(1,
        (equipment ? equipment.GetAttackPower() : 1) + StrengthAttackBonus);

    public float FinalAttackCooldown =>
        ((equipment ? equipment.GetAttackCooldown() : 0.5f) / Mathf.Max(0.01f, AttackSpeedMul));

    public float FinalAttackCritChance =>
        Mathf.Clamp01(equipment ? equipment.GetAttackCriticalChance() : 0f);

    // --- 채광 ---
    public int FinalMiningPower => Mathf.Max(1,
       (equipment ? equipment.GetMiningPower() : 1) + StrengthMiningBonus);

    public float FinalMiningCooldown =>
        ((equipment ? equipment.GetMiningCooldown() : 0.5f) / Mathf.Max(0.01f, MiningSpeedMul));

    public float FinalMiningCritChance =>
        Mathf.Clamp01(equipment ? equipment.GetMiningCriticalChance() : 0f);

    // 필요 시 외부에서 호출해 강제 갱신 트리거
    public void ForceStatRecalcNotify() => OnStatsChanged?.Invoke();

    #endregion

    private void HandleAutoRegen()
    {
        bool hpChanged = false;
        bool mpChanged = false;

        // HP 자동 회복
        if (hpRegenPerSecond > 0f && CurrentHP > 0f && CurrentHP < MaxHP)
        {
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + hpRegenPerSecond * Time.deltaTime);
            hpChanged = true;
        }

        // MP 자동 회복
        if (mpRegenPerSecond > 0f && CurrentMP < MaxMP)
        {
            CurrentMP = Mathf.Min(MaxMP, CurrentMP + mpRegenPerSecond * Time.deltaTime);
            mpChanged = true;
        }

        if (hpChanged) OnHPChanged?.Invoke(CurrentHP, MaxHP);
        if (mpChanged) OnMPChanged?.Invoke(CurrentMP, MaxMP);
    }

    // 플레이어 캐릭터 피격 시 호출해 현재 체력 감소 함수
    public void TakeDamage(float damage, bool isCritical = false)
    {
        // 데미지가 0 이하일 경우 즉시 리턴
        if (damage <= 0) return;

        if (isInvincible) return;

        // 회피 시 무효
        if (UnityEngine.Random.value < Mathf.Clamp01(buffs.EvasionChance))
        { 
            return;
        }

        // 피해감소 적용
        if (buffs) damage *= Mathf.Max(0.01f, buffs.DamageTakenMul);

        // Clamp 함수로 플레이어가 받을 수 있는 최대 데미지 고정
        CurrentHP = Mathf.Clamp(CurrentHP - damage, 0f, MaxHP);

        // 피격 데미지 텍스트 스폰
        if (hitText != null)
        {
            Instantiate(hitText, transform.position, Quaternion.identity).Initialize(damage, isCritical, true);
        }

        // HUD 업데이트
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        if (damagedClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(damagedClip, damagedVolume);
        }

        StartInvincible();

        // 현재 체력이 0 이하일 경우 사망 함수 호출
        if (CurrentHP <= 0) 
        {
            OnDead();
        }

        Debug.Log("[PlayerStatue] 피격! " + damage);
    }

    // 힐 스킬 대상일 경우 호출
    public void Heal(float amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Clamp 함수로 플레이어가 받을 수 있는 최대 힐량 고정
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, MaxHP);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);
    }

    // 스킬 사용 시 호출해 현재 마나 감소 함수
    // 추후 스킬 사용 시 해당 함수를 호출해 사용하려는 마나가 최대 마나보다 적은지 확인하는 로직 추가
    public bool TrySpendMP(float usedMana)
    {
        if (usedMana > CurrentMP)
        {
            Debug.Log($"마나 부족! 현재 마나: {CurrentMP}");
            return false;
        }

        CurrentMP = Mathf.Clamp(CurrentMP - usedMana, 0, MaxMP);
        OnMPChanged?.Invoke(CurrentMP, MaxMP);
        Debug.Log($"[PlayerStatus] 마나 소모! 남은 마나: {CurrentMP}");

        return true;
    }

    // 마나 회복 시 호출
    public void RestoreMP(float amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentMP = Mathf.Clamp(CurrentMP + amount, 0, MaxMP);
        OnMPChanged?.Invoke(CurrentMP, MaxMP);
    }

    public void FullRestore()
    {
        CurrentHP = MaxHP;
        CurrentMP = MaxMP;

        OnHPChanged?.Invoke(CurrentHP, MaxHP);
        OnMPChanged?.Invoke(CurrentMP, MaxMP);
    }

    public void OnDead()
    {
        // 플레이어 이동 불가능
        // 사망 애니메이션 출력
        Debug.Log("플레이어 사망");
        animator.SetTrigger("Dead");
        GameManager.Instance.GameOver();
    }

    private void StartInvincible()
    {
        isInvincible = true;
        invincibleTimer = invincibleDuration;

        // 기존에 돌던 피격 번쩍 코루틴이 있으면 정지
        if (hitFlashCo != null)
        {
            StopCoroutine(hitFlashCo);
        }

        // 두 단계 번쩍 연출 시작
        hitFlashCo = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        if (sr == null)
        {
            yield break;
        }

        // 1단계: 강하게 하얗게 번쩍
        sr.color = hitFlashColor1;
        yield return new WaitForSeconds(hitFlashInterval);

        // 2단계: 살짝 붉은 톤으로 번쩍
        sr.color = hitFlashColor2;
        yield return new WaitForSeconds(hitFlashInterval);

        // 이후 남은 무적 시간 동안은 반투명 상태 유지
        SetSpriteInvincible(true);

        hitFlashCo = null;
    }

    private void SetSpriteInvincible(bool on)
    {
        if (sr == null) return;

        if (on)
        {
            // 무적 동안 약간 반투명
            sr.color = new Color(originColor.r, originColor.g, originColor.b, 0.5f);
        }
        else
        {
            sr.color = originColor;
        }
    }
}

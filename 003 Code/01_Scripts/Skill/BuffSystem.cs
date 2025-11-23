using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnemyGlobalModifiers
{
    // 몬스터 AI/스테이트머신에서 속도/공격주기 계산 시 곱해주기
    public static float EnemyMoveSpeedMul { get; private set; } = 1f;
    public static float EnemyAttackSpeedMul { get; private set; } = 1f;

    public static void SetGlobalSlow(float slowMul) // 0.8f = 20% 감속
    {
        EnemyMoveSpeedMul = Mathf.Clamp(slowMul, 0.2f, 2f);
        EnemyAttackSpeedMul = Mathf.Clamp(slowMul, 0.2f, 2f);
    }
}

/// <summary>
/// 전부 "배수/가산"을 합성해 최종치 제공.
/// - 일시적(키 기반) 버프: ApplyTimedMultiplierEx
/// - 1회성(던전 런) 버프: ApplyRunBuff / RemoveRunBuff
/// - 처치/저체력/글로벌 디버프 등은 BuffData 설정으로 자동 처리
/// </summary>
public class BuffSystem : MonoBehaviour
{
    #region 합성 결과(외부에서 읽기만)
    public float MoveSpeedMul { get; private set; } = 1f;
    public float AttackSpeedMul { get; private set; } = 1f;
    public float MiningSpeedMul { get; private set; } = 1f;
    public float DamageTakenMul { get; private set; } = 1f; // 0.8 = 20% 감소
    public float CritChanceAdd { get; private set; } = 0f; // +0.05 = +5%
    public float EvasionChance { get; private set; } = 0f; // +0.10 = +10%
    public float CooldownReduction { get; private set; } = 0f; // +0.10 = 10% 감소
    public float DoubleDropChance { get; private set; } = 0f; // +0.15 = 15%
    public float MoveSpeedSlowMul { get; private set; } = 1f; // 던전 전역 감속에 대비해 분리(플레이어용)
    #endregion

    [Header("의존")]
    [SerializeField] private PlayerStatus status;

    // 일시적 버프 풀(키 기반)
    private class TimedBuff
    {
        public float until;
        public float move, atkSpd, mining, addDouble, slow, dmgTaken;
        public float critAdd, evasionAdd, cdr;
        public GameObject vfxInstance;
    }
    private readonly Dictionary<string, TimedBuff> _timed = new();

    // 1회성 런 버프 적용 목록
    private readonly HashSet<string> _runBuffIds = new();
    private readonly List<BuffData> _runBuffs = new();

    // 지속 체크용
    private Coroutine _lowHpWatcher;
    private Coroutine _manaRegenLoop;

    private void Awake()
    {
        if (!status) status = GetComponent<PlayerStatus>();
        Recalculate();
    }

    private void Update()
    {
        if (_timed.Count == 0) return;

        bool changed = false;
        var toRemove = new List<string>();
        float now = Time.time;

        foreach (var kv in _timed)
        {
            if (now >= kv.Value.until)
            {
                // VFX 정리
                if (kv.Value.vfxInstance)
                {
                    Destroy(kv.Value.vfxInstance);
                }
                toRemove.Add(kv.Key);
                changed = true;
            }
        }

        foreach (var k in toRemove) _timed.Remove(k);
        if (changed) Recalculate();
    }

    #region === 퍼블릭 API ===
    // 일시적(키 기반) 버프: 스킬 등에서 호출 (PlayerSkillSystem에서 이미 사용중)
    public void ApplyTimedMultiplierEx(
        string key,
        float duration,
        float moveMul = 1f,
        float atkSpeedMul = 1f,
        float miningSpeedMul = 1f,
        float addDoubleChance = 0f,
        float slowMul = 1f,
        bool setStun = false, // (확장 여지)
        GameObject loopVFXPrefab = null,
        Vector2 vfxOffset = default,
        bool vfxFollow = true,
        float vfxFallbackLifetime = 2f,
        float damageTakenMul = 1f,
        float critChanceAdd = 0f,
        float evasionAdd = 0f,
        float cooldownReductionAdd = 0f
    )
    {
        var until = Time.time + Mathf.Max(0.01f, duration);
        TimedBuff t;
        if (!_timed.TryGetValue(key, out t))
        {
            t = new TimedBuff();
            _timed[key] = t;
        }
        t.until = until;
        t.move = moveMul;
        t.atkSpd = atkSpeedMul;
        t.mining = miningSpeedMul;
        t.addDouble = addDoubleChance;
        t.slow = slowMul;
        t.dmgTaken = damageTakenMul;
        t.critAdd = critChanceAdd;
        t.evasionAdd = evasionAdd;
        t.cdr = cooldownReductionAdd;

        // VFX(선택)
        if (loopVFXPrefab)
        {
            if (t.vfxInstance) Destroy(t.vfxInstance);
            var tr = transform;
            t.vfxInstance = Instantiate(loopVFXPrefab,
                vfxFollow ? tr : null);
            t.vfxInstance.transform.position = (Vector2)tr.position + vfxOffset;

            // 혹시 파괴 누락 대비
            Destroy(t.vfxInstance, Mathf.Max(duration, vfxFallbackLifetime));
        }

        Recalculate();
    }

    // 던전 1회성 버프 적용/제거
    public void ApplyRunBuff(BuffData data)
    {
        if (data == null || _runBuffIds.Contains(data.buffId)) return;
        _runBuffIds.Add(data.buffId);
        _runBuffs.Add(data);

        // 글로벌 감속
        if (data.triger == BuffTriger.GlobalAffect && data.enemySlowMul > 0f && data.enemySlowMul < 1f)
        {
            EnemyGlobalModifiers.SetGlobalSlow(data.enemySlowMul);
            MoveSpeedSlowMul = data.enemySlowMul; // 플레이어도 느리게 할 거면 활성화
        }

        // 지속 감시(저체력 보정/마나재생)
        EnsureLongRunningWatchers();

        Recalculate();
    }

    public void RemoveRunBuff(BuffData data)
    {
        if (data == null || !_runBuffIds.Contains(data.buffId)) return;
        _runBuffIds.Remove(data.buffId);
        _runBuffs.Remove(data);

        if (data.triger == BuffTriger.GlobalAffect)
        {
            EnemyGlobalModifiers.SetGlobalSlow(1f);
            MoveSpeedSlowMul = 1f;
        }

        Recalculate();
    }

    public void ClearRunBuffs()
    {
        _runBuffIds.Clear();
        _runBuffs.Clear();
        EnemyGlobalModifiers.SetGlobalSlow(1f);
        MoveSpeedSlowMul = 1f;

        StopLongRunningWatchers();
        Recalculate();
    }

    // 몬스터 사망 시 (플레이어가 처치) 호출해주면 onKill 계열 처리됨
    public void NotifyPlayerKill()
    {
        if (_runBuffs.Count == 0) return;

        foreach (var b in _runBuffs)
        {
            // BUF-002: 처치 시 체/마 회복
            if (b.onKillHealPercent > 0f && status)
                status.Heal(status.MaxHP * b.onKillHealPercent);

            if (b.onKillManaRegenPercent > 0f && status)
                status.RestoreMP(status.MaxMP * b.onKillManaRegenPercent);

            // BUF-001: 처치 시 일시 공증
            if (b.OnKillBuffAttackPowerMul > 1f && b.onKillBuffDuration > 0f)
            {
                ApplyTimedMultiplierEx(
                    key: $"onkill:{b.buffId}:{Time.frameCount}",
                    duration: b.onKillBuffDuration,
                    damageTakenMul: 1f,
                    critChanceAdd: 0f,
                    moveMul: 1f,
                    atkSpeedMul: 1f,
                    miningSpeedMul: 1f,
                    cooldownReductionAdd: 0f
                );
                // 공격력 배수만큼 "공격속도"가 아닌 "공격력"을 곱하고 싶다면
                // PlayerStatus에서 공격력 계산시 별도 훅이 필요하지만,
                // 본 프로젝트에선 최종 공격력은 장비+능력치 기반이므로
                // 일시 공증을 "피해계수"로 해석하여 DamageTakenMul에 곱하지 않고,
                // 실제 적용은 Monster.GotHit에서 추가계수로 곱하거나
                // 여기처럼 TimedMultiplierEx에 "critAdd"만 쓰지 않고 아래처럼
                // 내부 피해배수 전용으로 합성하고 싶으면 별도의 합성 변수로 확장 가능.
                // 간결하게 유지하기 위해 '일시 공증'은 크리티컬 대체가 아닌
                // 아래 임시 계수로 처리:
                _tempOnHitDamageMulUntil = Time.time + b.onKillBuffDuration;
                _tempOnHitDamageMul = b.OnKillBuffAttackPowerMul;
            }
        }
    }

    // PlayerCombat에서 최종 데미지를 산출한 다음 한 번 더 곱하고 싶을 때 사용
    public int ModifyDealtDamage(int baseDamage)
    {
        if (Time.time <= _tempOnHitDamageMulUntil)
        {
            return Mathf.RoundToInt(baseDamage * _tempOnHitDamageMul);
        }
        return baseDamage;
    }
    private float _tempOnHitDamageMul = 1f;
    private float _tempOnHitDamageMulUntil = -1f;
    #endregion

    #region 내부 합성
    private void Recalculate()
    {
        // 기본치
        float move = 1f, atk = 1f, mining = 1f, dmgTaken = 1f, slow = 1f;
        float crit = 0f, evasion = 0f, cdr = 0f, dbl = 0f;

        // 1) 런 버프 합성
        foreach (var b in _runBuffs)
        {
            move *= Mathf.Max(0.01f, b.moveSpeedMul);
            atk *= Mathf.Max(0.01f, b.attackSpeedMul);
            mining *= Mathf.Max(0.01f, b.miningSpeedMul);
            dmgTaken *= Mathf.Max(0.01f, b.damageTakenMul);
            crit += Mathf.Max(0f, b.critChanceAdd);
            evasion += Mathf.Max(0f, b.evasionAdd);
            cdr += Mathf.Max(0f, 1f - Mathf.Clamp01(b.cooldownReductionMul)); // 0.9 => 10%감소
            dbl += Mathf.Max(0f, b.doubleOreChanceAdd);
        }

        // 2) 일시 버프 합성
        foreach (var t in _timed.Values)
        {
            move *= Mathf.Max(0.01f, t.move);
            atk *= Mathf.Max(0.01f, t.atkSpd);
            mining *= Mathf.Max(0.01f, t.mining);
            slow *= Mathf.Max(0.01f, t.slow);
            dmgTaken *= Mathf.Max(0.01f, t.dmgTaken);
            crit += Mathf.Max(0f, t.critAdd);
            evasion += Mathf.Max(0f, t.evasionAdd);
            cdr += Mathf.Max(0f, t.cdr);
            dbl += Mathf.Max(0f, t.addDouble);
        }

        MoveSpeedMul = move;
        AttackSpeedMul = atk;
        MiningSpeedMul = mining;
        DamageTakenMul = dmgTaken;
        CritChanceAdd = Mathf.Clamp01(crit);
        EvasionChance = Mathf.Clamp01(evasion);
        CooldownReduction = Mathf.Clamp01(cdr);
        DoubleDropChance = Mathf.Clamp01(dbl);

        // 플레이어 전용 감속(던전 전역 슬로우와 별개로 필요하면 사용)
        MoveSpeedSlowMul = slow;
    }

    private void EnsureLongRunningWatchers()
    {
        if (_lowHpWatcher == null) _lowHpWatcher = StartCoroutine(CoLowHpWatcher());
        if (_manaRegenLoop == null) _manaRegenLoop = StartCoroutine(CoManaRegen());
    }

    private void StopLongRunningWatchers()
    {
        if (_lowHpWatcher != null) StopCoroutine(_lowHpWatcher);
        if (_manaRegenLoop != null) StopCoroutine(_manaRegenLoop);
        _lowHpWatcher = _manaRegenLoop = null;
    }

    private IEnumerator CoLowHpWatcher()
    {
        // 저체력 보정이 있는 런버프가 있을 때만 의미가 있음
        while (true)
        {
            bool any = false;
            float minThreshold = 1.1f;
            float dmgMulAtLow = 1f;
            float regenPerSec = 0f;

            foreach (var b in _runBuffs)
            {
                if (b.lowHpThreshold > 0f && (b.lowHp_DamageTakenMul < 1f || b.lowHp_HpRegenPerSec > 0f))
                {
                    any = true;
                    minThreshold = Mathf.Min(minThreshold, b.lowHpThreshold);
                    dmgMulAtLow = Mathf.Min(dmgMulAtLow, b.lowHp_DamageTakenMul <= 0f ? 1f : b.lowHp_DamageTakenMul);
                    regenPerSec = Mathf.Max(regenPerSec, b.lowHp_HpRegenPerSec);
                }
            }

            if (!any || status == null || status.MaxHP <= 0f)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float hpRate = status.CurrentHP / status.MaxHP;
            if (hpRate <= minThreshold)
            {
                // 저체력 구간
                DamageTakenMul = Mathf.Min(DamageTakenMul, dmgMulAtLow <= 0f ? 1f : dmgMulAtLow);
                if (regenPerSec > 0f) status.Heal(regenPerSec * Time.deltaTime);
            }
            else
            {
                // 저체력 해제 -> 전체 재계산으로 복원
                Recalculate();
            }

            yield return null;
        }
    }

    private IEnumerator CoManaRegen()
    {
        while (true)
        {
            float regenMul = 1f;
            foreach (var b in _runBuffs) regenMul *= Mathf.Max(0.01f, b.manaRegenMul);
            foreach (var t in _timed.Values) regenMul *= 1f; // 필요시 일시 마나재생도 확장

            // 기본 1MP/s * 배수 (원하면 DataManager로 기본 재생률을 노출 가능)
            if (status != null && regenMul > 0f)
            {
                status.RestoreMP(regenMul * Time.deltaTime);
            }
            yield return null;
        }
    }
    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/* 
 * 플레이어에 부착할 스크립트
 * 시전/쿨타임 관리/스킬 효과 실행
 */

public class PlayerSkillSystem : MonoBehaviour
{
    [Header("장착 슬롯")]
    public SkillData slot1;
    public SkillData slot2;

    [Header("의존")]
    [SerializeField] private PlayerEquipment equipment; // 공격력/속도 참조
    [SerializeField] private PlayerStatus status;       // HP, MP 등 참조
    [SerializeField] private MouseAim mouseAim;         // 방향성 스킬을 위한 참조
    [SerializeField] private BuffSystem buffs;

    private readonly Dictionary<string, float> _cooltime = new();
    public event Action<int, SkillData> OnSlotChanged; // 1 or 2

    public static PlayerSkillSystem Local;

    private void Awake()
    {
        if (!buffs) buffs = GetComponent<BuffSystem>();
        if (!equipment) equipment = GetComponent<PlayerEquipment>();
        if (!status) status = GetComponent<PlayerStatus>();
        if (!mouseAim) mouseAim = GetComponent<MouseAim>();
        Local = this;
    }
    public void OnSkill1()
    {
        TrySkillCast(slot1);
    }

    public void OnSkill2()
    {
        TrySkillCast(slot2);
    }

    public bool TrySkillCast(SkillData skillData)
    {
        if (!skillData) return false;

        // 레벨별 수치 해석
        var R = SkillLeveling.Resolve(skillData);

        // 쿨타임 체크
        float remain = GetRemainCooldown(skillData.skillId);
        if (remain > 0f)
        {
            Debug.Log($"{skillData.skillName}의 남은 쿨타임 {remain:F1}s");
            return false;
        }

        // 코스트 확인
        if (!status.TrySpendMP(R.mpCost))
        {
            // 마나 부족 시 이펙트 추가
            Debug.Log($"MP {R.mpCost - status.CurrentMP} 부족");
            return false;
        }

        // 스킬 효과 발동
        StartCoroutine(SkillExecutor.Executor(this, skillData, R));

        _cooltime[skillData.skillId] = Time.time + Mathf.Max(0.01f, R.cooldown);
        return true;
    }

    // 해당 스킬의 남은 쿨타임 반환
    public float GetRemainCooldown(string skillId)
    {
        if (!_cooltime.TryGetValue(skillId, out var until)) return 0f;
        return Mathf.Max(0f, until - Time.time);
    }

    public float AttackPower => status ? status.FinalAttackPower : (equipment ? equipment.GetAttackPower() : 1f);
    public float MoveSpeedMul => buffs ? buffs.MoveSpeedMul * buffs.MoveSpeedSlowMul : 1f;
    public float AttackSpeedMul => buffs ? buffs.AttackSpeedMul : 1f;
    public float MiningSpeedMul => buffs ? buffs.MiningSpeedMul : 1f;
    public float DoubleDropChance => buffs ? buffs.DoubleDropChance : 0f;

    public void SetSlot(int slotIndex, SkillData data)
    {
        if (slotIndex == 1) slot1 = data;
        else if (slotIndex == 2) slot2 = data;
        OnSlotChanged?.Invoke(slotIndex, data);
    }

    public SkillData GetSkillInSlot(int slotIndex)
    {
        if (slotIndex == 1) return slot1;
        if (slotIndex == 2) return slot2;
        return null;
    }

    public void UnequipIfMatched(int slotIndex, SkillData data)
    {
        if (slotIndex == 1 && slot1 == data) 
        {
            slot1 = null;
            OnSlotChanged?.Invoke(1, null);
        }
        else if (slotIndex == 2 && slot2 == data)
        {
            slot2 = null;
            OnSlotChanged?.Invoke(2, null);
        }
    }

    // 광물 2배 드랍 확률 적용 (Mineable에서 호출)
    public int ModifyMiningDropChance(int baseCount)
    {
        if (buffs == null || buffs.DoubleDropChance <= 0f) return baseCount;
        if (UnityEngine.Random.value < buffs.DoubleDropChance) return baseCount * 2;
        return baseCount;
    }

    public void HealOverTime(float totalAmount, float duration)
    {
        StartCoroutine(CoHealOverTime(totalAmount, duration));
    }
    private IEnumerator CoHealOverTime(float total, float dur)
    {
        if (status == null || total <= 0f || dur <= 0f) yield break;
        int ticks = Mathf.Max(1, Mathf.RoundToInt(dur));
        float per = total / ticks;
        for (int i = 0; i < ticks; i++)
        {
            status.Heal(per);
            yield return new WaitForSeconds(1.5f);
        }
    }

    public void ApplyTimedBuff(
        string key, // 버프 식별 키 추가
        float duration,
        float moveMul = 1f,
        float atkSpeedMul = 1f,
        float miningSpeedMul = 1f,
        float addDoubleChance = 0f,
        float slowMul = 1f,
        bool setStun = false,
        GameObject loopVFXPrefab = null,
        Vector2 vfxOffset = default,
        bool vfxFollow = true,
        float vfxFallbackLifetime = 2f
    )
    {
        buffs.ApplyTimedMultiplierEx(
            key,
            duration,
            moveMul,
            atkSpeedMul,
            miningSpeedMul,
            addDoubleChance,
            slowMul,
            setStun,
            loopVFXPrefab,
            vfxOffset,
            vfxFollow,
            vfxFallbackLifetime
        );
    }

    // 슬롯 변경을 알리는 함수 (최초 시작 시 아이콘 설정 위해 작성)
    public void NotifyAllSlotsChanged()
    {
        OnSlotChanged?.Invoke(1, slot1);
        OnSlotChanged?.Invoke(2, slot2);
    }
}

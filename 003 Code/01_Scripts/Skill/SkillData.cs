using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public enum SkillKind { Buff, Attack, CC, Move, Install }
public enum SkillCastType { Active, Passive }
public enum SkillTargeting { SelfCenter, Directinal, SelfOnly }

public enum SkillEffectAnchor { Caster, Mouse, InFront, CustomPoint}

[System.Serializable]
public class SkillUnlockCondition
{
    // 잠금 해제에 필요한 조건 (스킬 ID, 필요한 최소 레벨)
    public string requiredSkillId;
    public int requiredLevel = 1;
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillId;      // 스킬 아이디
    public string skillName;    // 스킬 이름
    public Sprite skillIcon;     // 스킬 아이콘 스프라이트
    [TextArea] public string description;

    [Header("분류/동작")]
    public SkillKind kind;      // 버프, 공격, CC, 이동, 설치
    public SkillCastType castType = SkillCastType.Active; // 캐스트 방식

    [Header("기본 수치(배열 비어있을 때 사용되는 기본값)")]
    public float duration = 0f;           // 기본 지속시간
    public float cooldown = 0f;           // 기본 쿨타임
    public int cost = 0;                  // 기본 MP
    public float value = 0f;              // 기본 계수(버프율/치유량/확률 등)
    public float attackMultiplier = 1f;   // 기본 공격 배율(공격형)
    public float damageFlat = 0f;         // 기본 고정 피해량
    public float range = 1f;
    public float radius = 1f;

    [Header("타겟팅/범위")]
    public SkillTargeting targeting;    // 자기중심/방향/자기자신

    [Header("디버프/CC")]
    public float moveSpeedPercent = 0f; // 0.5 => 50% 감속
    public float stunSeconds = 0f;

    [Header("설치기")]
    public GameObject installPrefab;    // 스킬로 설치될 게임오브젝트

    [Header("레벨 설정")]
    public int maxLevel;                // 최고 레벨
    public int[] upgradeCost;           // 업그레이드 및 해금 비용

    // ▼ 레벨별 덮어쓰기/스케일 값(있으
    // 면 해당 레벨 인덱스를 사용)
    public float[] levelCooldown;        // 쿨타임
    public float[] levelDuration;        // 지속시간
    public int[] levelMPCost;            // MP 소모 (선택)
    public float[] levelValue;           // 계수(버프율/치유량/확률 등)
    public float[] levelAtkMul;          // 공격 배율
    public float[] levelDmgFlat;         // 고정 데미지
    public float[] levelRange;           // 사거리
    public float[] levelRadius;          // 반경
    

    [Header("해금 조건")]
    public SkillUnlockCondition[] unlockConditions;

    [Header("연출 및 VFX")]
    public GameObject castVFX;  // 시전 시 보여줄 이펙트
    public GameObject hitVFX;   // 피격 및 적중 지점에 보여줄 이펙트
    public SkillEffectAnchor castAnchor = SkillEffectAnchor.Caster;
    public Vector2 castOffset;
    public bool castFollowCaster = false; // true라면 캐릭터 자식으로 붙여서 추적
    public float vfxLifetime = 1.5f; // 파티클 없을 때 파괴용 수명

    [Header("버프 효과 VFX")]
    public GameObject buffVFX;
    public Vector2 buffOffset = new Vector2(0f, 0.2f);
    public bool buffFollow = true;

    [Header("SFX")]
    public AudioClip castSFX;
    public AudioClip hitSFX;
    [Range(0f, 1f)] public float sfxVolume = 1f;
}

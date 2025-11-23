using UnityEngine;

public enum BuffTriger
{
    Passive,        // 지속형
    OnKill,         // 적 처치 시 잠깐 발동
    OnLowHP,        // 체력 낮을 때 발동
    GlobalAffect    // 던전 전역에 적용
}

[CreateAssetMenu(fileName = "BuffData", menuName = "Game/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("식별/표시")]
    public string buffId;
    public string buffName;
    public Sprite buffIcon;
    public int price;
    [TextArea] public string desription;
    [TextArea] public string effectDesc;

    [Header("여정 1회만 적용")]
    public bool runScoped = true;

    [Header("동작")]
    public BuffTriger triger = BuffTriger.Passive;

    [Header("공통 수치")]
    public float attackPowerMul = 1f;       
    public float moveSpeedMul = 1f;
    public float attackSpeedMul = 1f;
    public float miningSpeedMul = 1f;
    public float damageTakenMul = 1f;       // 피해감소량 버프
    public float critChanceAdd = 0f;        // 크리티컬 확률 버프
    public float evasionAdd = 0f;           // 회피 확률 버프
    public float cooldownReductionMul = 1f;
    public float manaRegenMul = 1f;

    [Header("상황별 추가 수치")]
    public float onKillBuffDuration = 5f; // 처치 버프 지속 시간
    public float OnKillBuffAttackPowerMul = 1f; // 처치 시 증가할 공격력 배수

    public float lowHpThreshold = 0.2f; // 체력이 해당 비율 이하일 때 보너스 적용
    public float lowHp_DamageTakenMul = 1f;
    public float lowHp_HpRegenPerSec = 0f;

    [Header("흡혈/MP 흡수")]
    public float onKillHealPercent = 0f;        // 처치 시 최대 체력 비율만큼 회복
    public float onKillManaRegenPercent = 0f;   // 처치 시 최대 마나 비율만큼 회복

    [Header("광물 2배 드랍")]
    public float doubleOreChanceAdd = 0f;

    [Header("던전 디버프")]
    public float enemySlowMul = 1f; // 0.8 = 공/이속 20% 감속

    [Header("해금 관련")]
    public float requiredTP002Level = 0f; // 해금되는 신전 레벨
}

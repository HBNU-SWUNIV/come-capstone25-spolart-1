using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static MonsterDataSO;

[CreateAssetMenu(fileName = "BossmonsterAttackSO_", menuName = "Monster/BossMonsterAttack")]

public class BossMonsterAttackSO : ScriptableObject
{
    

    [Header(" !! 보스 공격 !! ")]
    [Header("점프 공격 변수")]
    public GameObject jumpPreviewPrefab;
    public GameObject stalactitePrefab;
    public int stalactiteCount = 5;
    public float stalactiteRange = 10;
    public float stalactiteDamage = 10;
    public GameObject jumpFX;
    public GameObject jumpPreFX;
    public float jumpAttackRadius = 5f;
    public float jumpAttackRange = 8f;
    [Tooltip("공격이 실행되기 전에 잠깐 멈추는 시간")] public float BossJump_preCastingTime = 0.7f;
    [Tooltip("공격이 끝나고 잠깐 멈추는 시간")] public float BossJump_postCastingTime = 1.1f;
    public float JumpAttackCooldown = 4f;
    public float jumpAttackDamage = 50f;

    

    [Header("보스 투사체 공격 변수")]

    public float BossprojectileSpeed = 10f;
    [Tooltip("랜덤 투사체 프리팹")] public GameObject[] BossProjectilePrefabs;

    public float BossprojectileAngle = 25f;
    [Tooltip("공격이 실행되기 전에 잠깐 멈추는 시간")] public float BossProjectile_preCastingTime = 0.5f;
    [Tooltip("공격이 끝나고 잠깐 멈추는 시간")] public float BossProjectile_postCastingTime = 0.2f;
    public float BossProjectile_AttackCooldown = 4f;
    public float projectileDamage = 10f;



    [Header("돌진 공격 변수")]
    [Tooltip("돌진 거리")] public float BossDashSpeed = 15f;
    [Tooltip("돌진이 실행되는 시간")] public float BossDashDistance = 10f;
    [Tooltip("돌진의 범위")] public float BossDashRadius = 2.25f;
    [Tooltip("돌진의 데미지")] public float BossDashDamage = 20f;
    [Tooltip("돌진이 범위를 나타내는 프리팹")] public GameObject BossdashPreviewPrefab;
    public GameObject BossmeleePreviewPrefab;
    public GameObject BossdashAttackRangePrefab;
    public float BossmeleeArcRadius = 4f; // 반지름
    public float BossmeleeArcAngle = 165f;   // 부채꼴 각도
    public int Bosssegments = 30;           // 부채꼴의 점 갯수 10개면 부드러운듯
    [Tooltip("공격이 실행되기 전에 잠깐 멈추는 시간")] public float BossDash_preCastingTime = 1f;
    [Tooltip("공격이 끝나고 잠깐 멈추는 시간")] public float BossDash_postCastingTime = 0f;
    [Tooltip("공격이 끝나고 잠깐 멈추는 시간")] public float BossMeele_preCastingTime = 0.5f;
    [Tooltip("공격이 끝나고 잠깐 멈추는 시간")] public float BossMeele_postCastingTime = 0.5f;
    [Tooltip("근접공격 데미지")] public float BossMeleeDamage = 20f;
    public float BossDashMeele_AttackCooldown = 4f;


    [Header("장애물 인식 레이어")]
    public LayerMask obstacleMask;

}
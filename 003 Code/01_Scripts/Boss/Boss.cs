
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Boss : MonoBehaviour
{
    #region variable.
    [Header("스텟관련 SO")]
    [Tooltip("공격할 때 필요한 데이터")] public MonsterDataSO monsterData;
    [Header("공격용 SO")]
    [Tooltip("사실 그냥 여기에다 해도 되는데 이 스크립트가 지저분해 질까봐 스크립트를 분리하고 싶었음")] public BossMonsterAttackSO monsterAttackData;
    [Header("체력바 프리팹")] public Transform healthBarPrefab;

    private Collider2D targetPlayer;
    private float monsterHealth;        // 몬스터 체력
    private float monsterPoise;         // 몬스터 그로기(poise) 수치
    private SpriteRenderer spriteRenderer;  // 스프라이트 렌더러 참조

    [Header("감지할 레이어")]
    [Tooltip("감지 범위에 들어왔을때 공격, 추격을 시도할 레이어")] public LayerMask playerLayer;
    private NavMeshAgent agent;
    private float attackTimer = 0; private float reviveTimer = 0;

    [Header("!옵션!")]
    [Tooltip("이거 키면 타겟 추적중에도 주기적으로 더 가까운 플레이어를 타겟으로 삼음")] public bool IsChangeTarget = false;


    Vector2 Vector2ToTarget; float distanceToTarget; Vector2 directionToTarget; // 나중에 타겟과의 위치 계산할 때 쓸 변수들 코드 깔끔할라고 위로 뺌
    private Collider2D monsterCollider;     // 몬스터의 콜라이더
    private Rigidbody2D rigid; Transform HealthBar;
    Animator anim;
    public AnimationCurve animationCurve;
    public Transform bodyVisual;
    #endregion

    public LayerMask wallLayer;
    private List<GameObject> meleePreviewInstances = new List<GameObject>();
    private List<GameObject> dashPreviewInstances = new List<GameObject>();
    private List<GameObject> jumpPreviewInstances = new List<GameObject>();
    private Coroutine attackCoroutine, stunRecoverCoroutine, poiseRecoverCoroutine;

    [SerializeField] private HitText hitText;
 
    public enum MonterState
    {
        Idle,
        Chase,
        Attack_Melee, Attack_Dash, Attack_Projectile, Attack_Jump,
        Hit,    // 피격 상태 추가
        Stun,    // 피격 상태 추가
        Die
    }
    private MonterState _currentState; 
    
    // ★ 3. public 프로퍼티를 통해 상태 변경을 제어하고 이벤트를 호출합니다.
    public MonterState CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState != value)
            {
                _currentState = value;
                
                // 상태가 변경되었음을 모든 구독자에게 알립니다.
                OnStateChanged?.Invoke(_currentState); 
            }
        }
    }
    public event Action<MonterState> OnStateChanged;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = monsterData.moveSpeed;  // SO에서 속도 설정

        rigid = GetComponentInChildren<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        monsterCollider = GetComponentInChildren<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();  // 스프라이트 렌더러 가져오기

        monsterHealth = monsterData.maxHealth;  // 초기 체력 설정
        monsterPoise = monsterData.maxPoise;    // 초기 그로기 수치 설정
        InvokeRepeating(nameof(CheckForPlayer), 0.1f, monsterData.detectionInterval); // 죽었을때 껐다가 다시키기
        attackTimer = 0;
        reviveTimer = monsterData.reviveTime;

        HealthBar = Instantiate(healthBarPrefab);
        HealthBar.SetParent(transform);
        HealthBar.GetComponent<MonsterHealthBar>().Init(transform, new Vector3(-1.5f, -1.5f, 0), 8, 1f);
    }

    void CheckForPlayer()
    {
        if (CutSceneManager.Instance.isCutScenePlaying) return;

        if (targetPlayer == null)
        {
            Collider2D tempCollider = DetectClosestPlayer();
            if (tempCollider != null) { targetPlayer = tempCollider; }
        }
        else if (IsChangeTarget)
        {
            Collider2D tempCollider = DetectClosestPlayer();
            if (tempCollider != null) { targetPlayer = tempCollider; }
        }

        // 타겟 탐지를 하지 않는 경우 = IsChangeTarget이 false고 targetPlayer가 null이 아닌 경우
        if (targetPlayer != null)
        {
            agent.destination = targetPlayer.transform.position;

            if (CurrentState == MonterState.Idle)
            {
                attackTimer = 0;
                SetChaseState();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(CurrentState);
        //Debug.Log(reviveTimer);


        if (targetPlayer == null) return;


        switch (CurrentState)
        {
            case MonterState.Idle:
                Update_In_Idle();
                break;
            case MonterState.Chase:
                Update_In_Chase();
                break;
            case MonterState.Attack_Melee or MonterState.Attack_Dash or MonterState.Attack_Projectile or MonterState.Attack_Jump:
                Update_In_Attack();
                break;
            case MonterState.Hit:
                Update_In_Hit();
                break;
            case MonterState.Die:
                Update_In_Die();
                break;
        }

        UpdateState();

    }



    void UpdateState()
    {
        if (targetPlayer == null) return;
        Vector2ToTarget = targetPlayer.transform.position - transform.position;
        distanceToTarget = Vector2ToTarget.magnitude;
        directionToTarget = Vector2ToTarget.normalized;

    }

    void Update_In_Idle()
    {

    }

    void Update_In_Chase()
    {
        UpdateSpriteDirection();  // 스프라이트 방향 업데이트

        // Debug.Log(attackTimer);
        attackTimer -= Time.deltaTime;

        if (distanceToTarget >= monsterData.stopChaseDistance)
        {
            targetPlayer = null;
            SetIdleState();
        }


        // 공격사거리에 들어왔을 때
        if (distanceToTarget <= monsterData.attackRange)
        {

            if (attackTimer <= 0f)
            {
                ExecuteAttack();
            }
        }
        if (attackTimer > 0f)
        {
            agent.destination = targetPlayer.transform.position - (targetPlayer.transform.position - transform.position).normalized * monsterData.chaseMinDistance;
        }
    }

    void Update_In_Attack()
    {

    }

    void Update_In_Hit()
    {

    }

    void Update_In_Die()
    {
        reviveTimer -= Time.deltaTime;

        if (reviveTimer <= 0f)
        {
            //Revive();
            //Debug.Log("부활");
        }
    }

    private void Revive()
    {
        SetIdleState();
        anim.SetTrigger("Revive");
        reviveTimer = monsterData.reviveTime;

        monsterCollider.enabled = true;
        monsterHealth = monsterData.maxHealth;
        monsterPoise = monsterData.maxPoise; // 부활 시 그로기 수치 초기화
        HealthBar.GetComponent<MonsterHealthBar>().UpdateHealthBar(monsterHealth, monsterData.maxHealth);
    }

    public void SetIdleState()
    {
        agent.isStopped = true; agent.destination = transform.position;
        CurrentState = MonterState.Idle;
        anim.SetBool("IsWalking", false);
    }

    public void SetChaseState()
    {
        Vector2ToTarget = targetPlayer.transform.position - transform.position;
        distanceToTarget = Vector2ToTarget.magnitude;
        directionToTarget = Vector2ToTarget.normalized;

        agent.isStopped = false;
        CurrentState = MonterState.Chase;
        anim.SetBool("IsWalking", true);
    }

    public void SetAttackState()
    {
        // monsterCollider.isTrigger = true;
        agent.isStopped = true; agent.velocity = Vector3.zero;
    }

    void SetDieState()
    {
        StopAttack();

        // 몬스터 사망 시 보고
        if (monsterData != null && !string.IsNullOrEmpty(monsterData.id))
        {
            Debug.Log($"몬스터 처치 보고: {monsterData.id}");
            QuestEvents.ReportMonsterKill(monsterData.id, 1);
        }

        CurrentState = MonterState.Die;
        agent.isStopped = true;
        targetPlayer = null;
        agent.velocity = Vector3.zero;
        monsterCollider.enabled = false;
        anim.SetTrigger("Die");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndRun(GameManager.RunEndReason.Success);
        }
    }

    public void GotHit(float damage, bool isCritical = false)
    {
        // 체력감소 
        monsterHealth -= damage;

        if (hitText != null)
        {
            // 몬스터의 체력이 깎였을 때, 데미지 텍스트 프리팹 생성 및 데미지 전달
            Instantiate(hitText, transform.position, Quaternion.identity).Initialize(damage, isCritical, false);
        }

        if (!HealthBar.gameObject.activeSelf) HealthBar.gameObject.SetActive(true);
        HealthBar.GetComponent<MonsterHealthBar>().UpdateHealthBar(monsterHealth, monsterData.maxHealth);

        if (monsterHealth <= 0)
        {
            SetDieState();
        }
        else
        {
            // 그로기 수치 감소
            monsterPoise -= damage;

            if (monsterPoise <= 0)
            {
                // 그로기 상태가 되면 공격을 멈추고 피격 모션
                SetStunState(monsterData.stunTime);
            }
        }
    }

    void SetStunState(float StunTime)
    {
        StopAttack();

        agent.isStopped = true; agent.velocity = Vector3.zero;  // 속도를 0으로 설정

        CurrentState = MonterState.Stun;
        anim.SetTrigger("Stun");

        attackTimer = monsterAttackData.BossDashMeele_AttackCooldown / 2;
        monsterPoise = monsterData.maxPoise; // 그로기 수치 초기화

        // 이전에 실행 중인 피격 회복 코루틴이 있다면 중단
        if (stunRecoverCoroutine != null)
        {
            StopCoroutine(stunRecoverCoroutine);
        }

        // 0.42초 후 상태 복귀
        stunRecoverCoroutine = StartCoroutine(StunRecoverCoroutine(StunTime));

        // 일정 시간 피격되지 않으면 그로기 회복 시작
        if (poiseRecoverCoroutine != null)
        {
            StopCoroutine(poiseRecoverCoroutine);
        }
        poiseRecoverCoroutine = StartCoroutine(PoiseRecoverCoroutine());
    }

    private IEnumerator StunRecoverCoroutine(float StunTime)
    {
        yield return new WaitForSeconds(StunTime);

        if (CurrentState == MonterState.Stun) // 여전히 Stun 상태일 때만 변경
        {
            SetChaseState();
        }
    }

    private IEnumerator PoiseRecoverCoroutine()
    {
        // 일정 시간 피격 없으면 회복 시작
        yield return new WaitForSeconds(monsterData.poiseRecoveryTime);

        while (monsterPoise < monsterData.maxPoise)
        {
            monsterPoise = Mathf.Min(monsterPoise + monsterData.poiseRecoveryPerTick * Time.deltaTime, monsterData.maxPoise);
            yield return null;
        }
        Debug.Log("Poise recovered.");
    }

    void StopAttack()
    {
        switch (CurrentState)
        {
            case (MonterState.Attack_Dash):
                HidePreview();
                StopCoroutine(attackCoroutine);
                break;

            case (MonterState.Attack_Projectile):
                StopCoroutine(attackCoroutine);
                break;

            case (MonterState.Attack_Jump):
                StopCoroutine(attackCoroutine);
                HidePreview();
                break;

            case (MonterState.Attack_Melee):
                HidePreview();
                StopCoroutine(attackCoroutine);
                break;
        }
    }

    Collider2D DetectClosestPlayer()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, monsterData.detectionRange, playerLayer);

        float closestDistance = Mathf.Infinity;
        Collider2D closestPlayer = null;

        foreach (Collider2D col in colliders)
        {
            float distance = Vector2.Distance(transform.position, col.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = col;
            }
        }

        return closestPlayer;
    }

    void UpdateSpriteDirection()
    {
        if (targetPlayer != null && (CurrentState == MonterState.Chase || CurrentState == MonterState.Attack_Melee 
            || CurrentState == MonterState.Attack_Dash || CurrentState == MonterState.Attack_Jump || CurrentState == MonterState.Attack_Projectile))
        {

            // 방향에 따라 스프라이트 뒤집기
            if (directionToTarget.x > 0)
            {
                spriteRenderer.flipX = false;  // 오른쪽 보기
            }
            else if (directionToTarget.x < 0)
            {
                spriteRenderer.flipX = true;   // 왼쪽 보기
            }
        }
    }

    #region AttackLogic
    public void ExecuteAttack()
    {
        int randomAttack = UnityEngine.Random.Range(0, 10);
        // int randomAttack = 4;
        switch (randomAttack)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                attackCoroutine = StartCoroutine(BossDashMeleeAttackSequence());
                break;

            case 4:
            case 5:
            case 6:
            case 7:
                attackCoroutine = StartCoroutine(BossProjectileAttackSequence());
                break;

            case 8:
            case 9:
                attackCoroutine = StartCoroutine(JumpAttackSequence());
                break;
        }
    }
    #region DashCoroutine
    private IEnumerator BossDashMeleeAttackSequence()
    {
        // 1. 돌진 거리, 방향 정하기
        Vector2 start = transform.position;

        float adjustedDistance = monsterAttackData.BossDashDistance;

        // 🔴 2. 벽 체크: 몬스터 -> 플레이어 사이에 벽이 있는 경우 공격 중단
        RaycastHit2D wallCheck = Physics2D.CircleCast(start, monsterAttackData.BossDashRadius, directionToTarget, Vector2.Distance(start, targetPlayer.transform.position), monsterAttackData.obstacleMask);
        if (wallCheck.collider != null)
        {
            yield break;
        }

        // 3. 상태 업데이트
        SetAttackState();
        CurrentState = MonterState.Attack_Dash;
        anim.SetTrigger("Dash");

        // 4. 돌격 범위 계산
        RaycastHit2D hit = Physics2D.CircleCast(start, monsterAttackData.BossDashRadius, directionToTarget, monsterAttackData.BossDashDistance, monsterAttackData.obstacleMask); // 벽 충돌 체크
        if (hit.collider != null)
        {
            adjustedDistance = hit.distance - 0.1f; // 충돌 지점 앞까지
        }

        //adjustedDistance = Mathf.Min(adjustedDistance, distanceToTarget);

        Vector2 dashTarget = start + directionToTarget * adjustedDistance;

        // 5. 돌진 경로 시각화
        // 1. 밑그림 프리뷰를 생성 및 설정
        if (monsterAttackData.BossdashPreviewPrefab != null)
        {
            if (dashPreviewInstances.Count == 0)
            {
                // 최종 범위를 나타내는 반투명한 밑그림 프리뷰
                GameObject dashPreview1 = Instantiate(monsterAttackData.BossdashPreviewPrefab);
                dashPreviewInstances.Add(dashPreview1);

                // 밑그림의 위치, 방향, 크기 설정
                Vector2 dir = dashTarget - start;
                float length = dir.magnitude;

                dashPreviewInstances[0].transform.position = (start + dashTarget) / 2;
                dashPreviewInstances[0].transform.right = dir;
                dashPreviewInstances[0].transform.localScale = new Vector3(length, monsterAttackData.BossDashRadius * 2, 1f);

                // 반투명한 색상으로 설정
                SpriteRenderer renderer1 = dashPreviewInstances[0].GetComponent<SpriteRenderer>();
                if (renderer1 != null)
                {
                    renderer1.color = new Color(1, 0, 0, 0.3f);
                }

                // 2. 애니메이션 프리뷰를 생성
                GameObject dashPreview2 = Instantiate(monsterAttackData.BossdashPreviewPrefab);
                dashPreviewInstances.Add(dashPreview2);

                SpriteRenderer renderer2 = dashPreviewInstances[1].GetComponent<SpriteRenderer>();
                if (renderer2 != null)
                {
                    renderer2.color = Color.red;
                }

                // 애니메이션 코루틴 시작
                // 프리뷰의 초기 위치와 방향 설정
                dashPreviewInstances[1].transform.position = start;
                dashPreviewInstances[1].transform.right = dir;

                float timer = 0f;
                while (timer < monsterAttackData.BossDash_preCastingTime)
                {
                    timer += Time.deltaTime;
                    float fillProgress = Mathf.Clamp01(timer / monsterAttackData.BossDash_preCastingTime);

                    // 프리뷰의 위치와 크기를 업데이트
                    float currentLength = length * fillProgress;
                    dashPreviewInstances[1].transform.localScale = new Vector3(currentLength, monsterAttackData.BossDashRadius * 2, 1f);
                    dashPreviewInstances[1].transform.position = start + dir.normalized * (currentLength / 2f);

                    yield return null;
                }



                // 애니메이션 완료 후 최종 크기로 설정 (정확도를 위해)
                dashPreviewInstances[1].transform.localScale = new Vector3(length, monsterAttackData.BossDashRadius * 2, 1f);
                dashPreviewInstances[1].transform.position = (start + dashTarget) / 2;

                GameObject dashAttackRange = Instantiate(monsterAttackData.BossdashAttackRangePrefab, new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z), Quaternion.identity);
                dashPreviewInstances.Add(dashAttackRange);

                dashPreviewInstances[2].transform.localScale = new Vector3(monsterAttackData.BossDashRadius * 2, monsterAttackData.BossDashRadius * 2, 0);
                dashPreviewInstances[2].gameObject.transform.SetParent(transform);
                dashPreviewInstances[2].GetComponent<DashAttackRange>().damage = monsterAttackData.BossDashDamage;
            }
            else
            {
                dashPreviewInstances[0].SetActive(true);
                dashPreviewInstances[1].SetActive(true);
                // 밑그림의 위치, 방향, 크기 설정
                Vector2 dir = dashTarget - start;
                float length = dir.magnitude;

                dashPreviewInstances[0].transform.position = (start + dashTarget) / 2;
                dashPreviewInstances[0].transform.right = dir;
                dashPreviewInstances[0].transform.localScale = new Vector3(length, monsterAttackData.BossDashRadius * 2, 1f);

                // 애니메이션 코루틴 시작
                // 프리뷰의 초기 위치와 방향 설정
                dashPreviewInstances[1].transform.position = start;
                dashPreviewInstances[1].transform.right = dir;

                float timer = 0f;
                while (timer < monsterAttackData.BossDash_preCastingTime)
                {
                    timer += Time.deltaTime;
                    float fillProgress = Mathf.Clamp01(timer / monsterAttackData.BossDash_preCastingTime);

                    // 프리뷰의 위치와 크기를 업데이트
                    float currentLength = length * fillProgress;
                    dashPreviewInstances[1].transform.localScale = new Vector3(currentLength, monsterAttackData.BossDashRadius * 2, 1f);
                    dashPreviewInstances[1].transform.position = start + dir.normalized * (currentLength / 2f);

                    yield return null;
                }

                // 애니메이션 완료 후 최종 크기로 설정 (정확도를 위해)
                dashPreviewInstances[1].transform.localScale = new Vector3(length, monsterAttackData.BossDashRadius * 2, 1f);
                dashPreviewInstances[1].transform.position = (start + dashTarget) / 2;

                dashPreviewInstances[2].SetActive(true);
            }

        }

        // var attackRange = Instantiate(monsterAttackData.BossdashAttackRangePrefab,new Vector3(transform.position.x,transform.position.y + 0.7f,transform.position.z),Quaternion.identity);
        // attackRange.transform.localScale = new Vector3(monsterAttackData.BossDashRadius * 2,monsterAttackData.BossDashRadius * 2,0);
        // attackRange.gameObject.transform.SetParent(transform);
        // attackRange.GetComponent<DashAttackRange>().damage = 10f;

        // 6. 선딜레이
        yield return new WaitForSeconds(monsterAttackData.BossDash_preCastingTime);

        // 7. 돌진 실행
        float distance = Vector2.Distance(start, dashTarget);
        float defaultSpeed = monsterAttackData.BossDashSpeed;
        float duration = distance / defaultSpeed;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            rigid.MovePosition(Vector2.Lerp(start, dashTarget, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        rigid.MovePosition(dashTarget);

        // 9. 돌진범위 숨기기
        HidePreview();

        // 8. 후딜레이
        yield return new WaitForSeconds(monsterAttackData.BossDash_postCastingTime);

        anim.SetTrigger("Attack");
        CurrentState = MonterState.Attack_Melee;
        UpdateSpriteDirection();

        // 2. 공격 위치와 방향 계산
        Vector3 attackOrigin = transform.position;
        Vector2 attackDir = directionToTarget.normalized;

        if (meleePreviewInstances.Count == 0)
        {
            // 1. 첫 번째 프리팹(밑그림) 생성 및 설정
            GameObject meleePreview1 = Instantiate(monsterAttackData.BossmeleePreviewPrefab, attackOrigin, Quaternion.identity);
            meleePreviewInstances.Add(meleePreview1);


            MeshFilter meshFilter1 = meleePreview1.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer1 = meleePreview1.GetComponent<MeshRenderer>();
            PolygonCollider2D polygonCollider1 = meleePreview1.AddComponent<PolygonCollider2D>();
            polygonCollider1.isTrigger = true;

            // 밑그림의 최종 부채꼴 모양 메쉬와 콜라이더를 한 번에 설정
            Mesh finalMesh = new Mesh();
            finalMesh.name = "FinalArcMesh";

            Vector3[] finalVertices = new Vector3[monsterAttackData.Bosssegments + 2];
            Vector2[] finalPoints = new Vector2[monsterAttackData.Bosssegments + 2];

            finalVertices[0] = Vector3.zero;
            finalPoints[0] = Vector2.zero;

            float startAngle = -monsterAttackData.BossmeleeArcAngle / 2f;
            float step = monsterAttackData.BossmeleeArcAngle / monsterAttackData.Bosssegments;
            float baseAngle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;

            for (int i = 0; i <= monsterAttackData.Bosssegments; i++)
            {
                float currentAngle = startAngle + step * i + baseAngle;
                float rad = Mathf.Deg2Rad * currentAngle;
                float x = Mathf.Cos(rad) * monsterAttackData.BossmeleeArcRadius;
                float y = Mathf.Sin(rad) * monsterAttackData.BossmeleeArcRadius;
                finalVertices[i + 1] = new Vector3(x, y, 0);
                finalPoints[i + 1] = new Vector2(x, y);
            }

            int[] triangles = new int[monsterAttackData.Bosssegments * 3];
            for (int i = 0; i < monsterAttackData.Bosssegments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            finalMesh.Clear();
            finalMesh.vertices = finalVertices;
            finalMesh.triangles = triangles;
            finalMesh.RecalculateNormals();
            finalMesh.RecalculateBounds();

            meshFilter1.mesh = finalMesh;
            meshRenderer1.material.color = new Color(1, 0, 0, 0.3f);
            polygonCollider1.SetPath(0, finalPoints);

            // 2. 두 번째 프리팹(애니메이션) 생성 및 설정
            GameObject meleePreview2 = Instantiate(monsterAttackData.BossmeleePreviewPrefab, attackOrigin, Quaternion.identity);
            meleePreviewInstances.Add(meleePreview2);

            MeshFilter meshFilter2 = meleePreview2.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer2 = meleePreview2.GetComponent<MeshRenderer>();

            Mesh animatedMesh = new Mesh();
            animatedMesh.name = "AnimatedArcMesh";
            meshFilter2.mesh = animatedMesh;
            meshRenderer2.material.color = Color.red;

            Vector3[] vertices = new Vector3[monsterAttackData.Bosssegments + 2];

            // --- 부채꼴이 꼭짓점에서 차오르는 애니메이션 ---
            float timer = 0f;
            while (timer < monsterAttackData.BossMeele_preCastingTime)
            {
                timer += Time.deltaTime;
                float fillProgress = Mathf.Clamp01(timer / monsterAttackData.BossMeele_preCastingTime);
                float currentRadius = monsterAttackData.BossmeleeArcRadius * fillProgress;

                vertices[0] = Vector3.zero;

                startAngle = -monsterAttackData.BossmeleeArcAngle / 2f;
                step = monsterAttackData.BossmeleeArcAngle / monsterAttackData.Bosssegments;
                baseAngle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;

                for (int i = 0; i <= monsterAttackData.Bosssegments; i++)
                {
                    float currentAngle = startAngle + step * i + baseAngle;
                    float rad = Mathf.Deg2Rad * currentAngle;
                    float x = Mathf.Cos(rad) * currentRadius;
                    float y = Mathf.Sin(rad) * currentRadius;
                    vertices[i + 1] = new Vector3(x, y, 0);
                }

                animatedMesh.Clear();
                animatedMesh.vertices = vertices;
                animatedMesh.triangles = triangles;
                animatedMesh.RecalculateBounds();

                yield return null;
            }
        }
        else
        {
            foreach (var item in meleePreviewInstances)
            {
                item.SetActive(true);
                item.transform.position = attackOrigin;
            }

            MeshFilter meshFilter1 = meleePreviewInstances[0].GetComponent<MeshFilter>();
            //  MeshRenderer meshRenderer1 = PreviewInstances[0].GetComponent<MeshRenderer>();
            PolygonCollider2D polygonCollider1 = meleePreviewInstances[0].GetComponent<PolygonCollider2D>();

            // 밑그림의 최종 부채꼴 모양 메쉬와 콜라이더를 한 번에 설정
            Mesh finalMesh = new Mesh();
            finalMesh.name = "FinalArcMesh";

            Vector3[] finalVertices = new Vector3[monsterAttackData.Bosssegments + 2];
            Vector2[] finalPoints = new Vector2[monsterAttackData.Bosssegments + 2];

            finalVertices[0] = Vector3.zero;
            finalPoints[0] = Vector2.zero;

            float startAngle = -monsterAttackData.BossmeleeArcAngle / 2f;
            float step = monsterAttackData.BossmeleeArcAngle / monsterAttackData.Bosssegments;
            float baseAngle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;

            for (int i = 0; i <= monsterAttackData.Bosssegments; i++)
            {
                float currentAngle = startAngle + step * i + baseAngle;
                float rad = Mathf.Deg2Rad * currentAngle;
                float x = Mathf.Cos(rad) * monsterAttackData.BossmeleeArcRadius;
                float y = Mathf.Sin(rad) * monsterAttackData.BossmeleeArcRadius;
                finalVertices[i + 1] = new Vector3(x, y, 0);
                finalPoints[i + 1] = new Vector2(x, y);
            }

            int[] triangles = new int[monsterAttackData.Bosssegments * 3];
            for (int i = 0; i < monsterAttackData.Bosssegments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            finalMesh.Clear();
            finalMesh.vertices = finalVertices;
            finalMesh.triangles = triangles;
            finalMesh.RecalculateNormals();
            finalMesh.RecalculateBounds();

            meshFilter1.mesh = finalMesh;
            // meshRenderer1.material.color = new Color(1, 0, 0, 0.3f);
            polygonCollider1.SetPath(0, finalPoints);

            // 2. 두 번째 프리팹(애니메이션) 생성 및 설정

            MeshFilter meshFilter2 = meleePreviewInstances[1].GetComponent<MeshFilter>();
            //MeshRenderer meshRenderer2 = PreviewInstances[1].GetComponent<MeshRenderer>();

            Mesh animatedMesh = new Mesh();
            // animatedMesh.name = "AnimatedArcMesh";
            meshFilter2.mesh = animatedMesh;
            // meshRenderer2.material.color = Color.red;

            Vector3[] vertices = new Vector3[monsterAttackData.Bosssegments + 2];

            // --- 부채꼴이 꼭짓점에서 차오르는 애니메이션 ---
            float timer = 0f;
            while (timer < monsterAttackData.BossMeele_preCastingTime)
            {
                timer += Time.deltaTime;
                float fillProgress = Mathf.Clamp01(timer / monsterAttackData.BossMeele_preCastingTime);
                float currentRadius = monsterAttackData.BossmeleeArcRadius * fillProgress;

                vertices[0] = Vector3.zero;

                startAngle = -monsterAttackData.BossmeleeArcAngle / 2f;
                step = monsterAttackData.BossmeleeArcAngle / monsterAttackData.Bosssegments;
                baseAngle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;

                for (int i = 0; i <= monsterAttackData.Bosssegments; i++)
                {
                    float currentAngle = startAngle + step * i + baseAngle;
                    float rad = Mathf.Deg2Rad * currentAngle;
                    float x = Mathf.Cos(rad) * currentRadius;
                    float y = Mathf.Sin(rad) * currentRadius;
                    vertices[i + 1] = new Vector3(x, y, 0);
                }

                animatedMesh.Clear();
                animatedMesh.vertices = vertices;
                animatedMesh.triangles = triangles;
                animatedMesh.RecalculateBounds();

                yield return null;
            }
        }

        // 3. 공격 실행 및 두 프리팹 모두 제거
        if (meleePreviewInstances.Count != 0)
        {
            meleePreviewInstances[0].GetComponent<AttackRange>().DoDamage(monsterAttackData.BossMeleeDamage);
        }

        // 6. 예고 프리팹 제거
        HidePreview();

        // 7. 후딜레이
        yield return new WaitForSeconds(monsterAttackData.BossMeele_postCastingTime);


        // 10. 공격 끝 상태 업데이트
        // monsterCollider.isTrigger = false;
        SetChaseState();

        attackTimer = monsterAttackData.BossDashMeele_AttackCooldown;
    }
    private void HidePreview()
    {
        if (meleePreviewInstances.Count != 0)
        {
            foreach (var item in meleePreviewInstances)
            {
                // Destroy 대신 SetActive(false) 사용
                item.SetActive(false);
            }
        }
        if (dashPreviewInstances.Count != 0)
        {
            foreach (var item in dashPreviewInstances)
            {
                // Destroy 대신 SetActive(false) 사용
                item.SetActive(false);
            }
        }
        if (jumpPreviewInstances.Count != 0)
        {
            foreach (var item in jumpPreviewInstances)
            {
                // Destroy 대신 SetActive(false) 사용
                item.SetActive(false);
            }
        }
    }
    #endregion
    private IEnumerator BossProjectileAttackSequence()
    {
        // ▶ 공격 전에 벽이 있는지 체크
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToTarget, distanceToTarget, monsterAttackData.obstacleMask);
        if (hit.collider != null)
        {
            // attackTimer = monsterAttackData.attackCooldown / 2;   // 공격이 캔슬나면 공격쿨타임의 반
            // 벽에 막혀 있으면 공격 취소
            yield break;
        }

        // 1. 공격 상태로 전환
        SetAttackState();
        CurrentState = MonterState.Attack_Projectile;
        anim.SetTrigger("Range");

        // 2. 선딜레이
        yield return new WaitForSeconds(monsterAttackData.BossProjectile_preCastingTime);

        // 3. 투사체 생성 및 발사
        if (monsterAttackData.BossProjectilePrefabs != null)
        {

            float spreadAngle = monsterAttackData.BossprojectileAngle; // 부채꼴 퍼짐 각도 (도 단위)

            for (int i = -1; i <= 1; i++) // -1: 아래쪽, 0: 정중앙, 1: 위쪽
            {
                int r = UnityEngine.Random.Range(0, 2);
                // 기준 방향에서 각도를 추가로 회전시킴
                float angle = spreadAngle * i;
                Vector2 rotatedDirection = RotateVector(directionToTarget, angle);

                GameObject proj = Instantiate(monsterAttackData.BossProjectilePrefabs[r], transform.position, Quaternion.identity);
                proj.GetComponent<MonsterProjectile>().damage = monsterAttackData.projectileDamage;
                Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();

                if (projRb != null)
                {
                    projRb.linearVelocity = rotatedDirection.normalized * monsterAttackData.BossprojectileSpeed;
                    float zAngle = 0;
                    switch (r)
                    {
                        case 0:
                            zAngle = Mathf.Atan2(rotatedDirection.y, rotatedDirection.x) * Mathf.Rad2Deg - 90f - 21f;
                            proj.transform.rotation = Quaternion.Euler(0, 0, zAngle);
                            break;
                        case 1:
                            zAngle = Mathf.Atan2(rotatedDirection.y, rotatedDirection.x) * Mathf.Rad2Deg - 90f;
                            proj.transform.rotation = Quaternion.Euler(0, 0, zAngle);
                            break;
                        case 2:
                            zAngle = Mathf.Atan2(rotatedDirection.y, rotatedDirection.x) * Mathf.Rad2Deg - 90f + 37f;
                            proj.transform.rotation = Quaternion.Euler(0, 0, zAngle);
                            break;
                    }
                    proj.transform.rotation = Quaternion.Euler(0, 0, zAngle);
                }
            }
        }

        // 4. 후딜레이
        yield return new WaitForSeconds(monsterAttackData.BossProjectile_postCastingTime);

        // 5. 상태 복귀
        SetChaseState();

        attackTimer = monsterAttackData.BossProjectile_AttackCooldown;
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    private IEnumerator JumpAttackSequence()
    {
        // 1. 공격 상태로 전환
        SetAttackState();
        CurrentState = MonterState.Attack_Jump;
        anim.SetTrigger("Jump");

        // 2. 공격 위치와 방향 계산
        Vector2 attackDir = directionToTarget.normalized;

        Vector2 start = transform.position;
        Vector2 end = start + attackDir * Mathf.Min(monsterAttackData.jumpAttackRange, distanceToTarget);

        if (jumpPreviewInstances.Count == 0)
        {
            if (monsterAttackData.jumpPreFX != null)
            {
                GameObject jumpPreVFX = Instantiate(monsterAttackData.jumpPreFX, new Vector3(transform.position.x, transform.position.y, 0), Quaternion.identity);
                jumpPreviewInstances.Add(jumpPreVFX);
            }

            float jumpduration = 0.8f;
            // // 4. 선딜레이
            yield return new WaitForSeconds(monsterAttackData.BossJump_preCastingTime); // 0.7f

            HidePreview();
            GetComponent<Collider2D>().enabled = false;

            // 3. 시각적 예고 프리팹 생성
            if (monsterAttackData.jumpPreviewPrefab != null)
            {
                GameObject jumpPreview = Instantiate(monsterAttackData.jumpPreviewPrefab, new Vector3(end.x, end.y, 0), Quaternion.identity);
                jumpPreviewInstances.Add(jumpPreview);
                float visualScale = monsterAttackData.jumpAttackRadius * 2f;
                jumpPreview.transform.localScale = new Vector3(visualScale, visualScale, 1f);
            }

            // 3. 시각적 예고(프리뷰) 프리팹 생성 (선택)
            float elapsed = 0f;
            while (elapsed < jumpduration)
            {
                // Vector2 newPos = Vector2.Lerp(start, end, elapsed / jumpduration);
                float t = elapsed / jumpduration;
                Vector2 pos = Vector2.Lerp(start, end, t);

                float height = animationCurve.Evaluate(t) * 0.8f;
                rigid.MovePosition(pos);
                if (bodyVisual != null)
                    bodyVisual.localPosition = new Vector3(0, height * 4, 0); // sprite 위로 이동

                elapsed += Time.deltaTime;
                yield return null;
            }
            rigid.MovePosition(end);
            if (bodyVisual != null)
                bodyVisual.localPosition = Vector3.zero; // 복원


            // 공격 데미지 계산 
            jumpPreviewInstances[1].GetComponent<AttackRange>().DoDamage(monsterAttackData.jumpAttackDamage);
            GetComponent<Collider2D>().enabled = true;

            // 6. 예고 프리팹 제거
            HidePreview();

            if (monsterAttackData.jumpFX != null)
            {
                GameObject jumpPostVFX = Instantiate(monsterAttackData.jumpFX, new Vector3(end.x, end.y, 0), Quaternion.identity);
                jumpPreviewInstances.Add(jumpPostVFX);
                jumpPreviewInstances[2].transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            }
        }
        else
        {
            if (monsterAttackData.jumpPreFX != null)
            {
                jumpPreviewInstances[0].SetActive(true);
                jumpPreviewInstances[0].transform.position = new Vector3(transform.position.x, transform.position.y, 0);
            }

            float jumpduration = 0.8f;
            // // 4. 선딜레이
            yield return new WaitForSeconds(monsterAttackData.BossJump_preCastingTime); // 0.7f

            HidePreview();
            GetComponent<Collider2D>().enabled = false;

            // 3. 시각적 예고 프리팹 생성
            if (monsterAttackData.jumpPreviewPrefab != null)
            {
                jumpPreviewInstances[1].SetActive(true);
                jumpPreviewInstances[1].transform.position = new Vector3(end.x, end.y, 0);
                //float visualScale = monsterAttackData.jumpAttackRadius * 2f;
                //jumpPreviewInstances[1].transform.localScale = new Vector3(visualScale, visualScale, 1f);
            }

            // 3. 시각적 예고(프리뷰) 프리팹 생성 (선택)
            float elapsed = 0f;
            while (elapsed < jumpduration)
            {
                // Vector2 newPos = Vector2.Lerp(start, end, elapsed / jumpduration);
                float t = elapsed / jumpduration;
                Vector2 pos = Vector2.Lerp(start, end, t);

                float height = animationCurve.Evaluate(t) * 0.8f;
                rigid.MovePosition(pos);
                if (bodyVisual != null)
                    bodyVisual.localPosition = new Vector3(0, height * 4, 0); // sprite 위로 이동

                elapsed += Time.deltaTime;
                yield return null;
            }
            rigid.MovePosition(end);
            if (bodyVisual != null)
                bodyVisual.localPosition = Vector3.zero; // 복원


            // 공격 데미지 계산 
            jumpPreviewInstances[1].GetComponent<AttackRange>().DoDamage(monsterAttackData.jumpAttackDamage);
            GetComponent<Collider2D>().enabled = true;

            // 6. 예고 프리팹 제거
            HidePreview();

            if (monsterAttackData.jumpFX != null)
            {
                jumpPreviewInstances[2].SetActive(true);
                jumpPreviewInstances[2].transform.position = new Vector3(end.x, end.y, 0);
                //jumpPreviewInstances[2].transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            }
        }


        StartCoroutine(SpawnStalactites(new Vector2(end.x, end.y)));

        // 7. 후딜레이
        yield return new WaitForSeconds(monsterAttackData.BossJump_postCastingTime); //1.1f

        HidePreview();


        // 8. 상태 복귀
        SetChaseState();

        // if (monsterAttackData.aoePrefab != null)
        // {
        //     GameObject aoe = Instantiate(monsterAttackData.aoePrefab, aoePosition, Quaternion.identity);
        //     aoe.transform.localScale = new Vector3(monsterAttackData.aoeRange * 2f, monsterAttackData.aoeRange * 2f, 1f);
        //     Destroy(aoe, monsterAttackData.aoeDuration);
        // }

        attackTimer = monsterAttackData.JumpAttackCooldown;
    }
    private IEnumerator SpawnStalactites(Vector2 center)
    {
        int spawned = 0;
        int maxAttempts = 20;

        while (spawned < monsterAttackData.stalactiteCount && maxAttempts-- > 0)
        {
            Vector2 candidate = center + UnityEngine.Random.insideUnitCircle * monsterAttackData.stalactiteRange;
            Collider2D ground = Physics2D.OverlapPoint(candidate, 1 << LayerMask.NameToLayer("Ground"));

            if (ground != null)
            {
                // 살짝 위쪽에서 종유석 생성 (시각 연출)
                Vector2 spawnPos = candidate;

                GameObject stalactite = Instantiate(monsterAttackData.stalactitePrefab, spawnPos, Quaternion.identity);
                GameObject stalactitePreview = Instantiate(monsterAttackData.jumpPreviewPrefab, spawnPos, Quaternion.identity);
                stalactitePreview.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);


                StartCoroutine(StalactitesCoroutine(stalactitePreview));

                spawned++;
                yield return new WaitForSeconds(0.1f); // 순차적 생성
            }
            else { Debug.Log("ground Missing"); }
        }
    }

    private IEnumerator StalactitesCoroutine(GameObject stalactitePreview)
    {
        yield return new WaitForSeconds(1f);
        stalactitePreview.GetComponent<AttackRange>().DoDamage(monsterAttackData.stalactiteDamage);
        Destroy(stalactitePreview);
    }
    #endregion
}

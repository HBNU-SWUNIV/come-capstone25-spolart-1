using System.Collections;
using UnityEngine;

/* 도발 인형 전용 스크립트 */

public class Decoy : MonoBehaviour
{
    [Header("기본 수명/피격")]
    [SerializeField] private float hp = 100f;
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float lifeTime = 10f;

    [Header("도발 설정")]
    [SerializeField] private float tauntRadius = 6f;       // 도발 영향 반경
    [SerializeField] private float tauntPulseInterval = 0.5f; // 도발 전파 간격
    [SerializeField] private float tauntDuration = 2.5f;   // 몬스터가 나를 보도록 강제되는 시간
    [SerializeField] private int tauntPriority = 100;      // 우선순위(높을수록 강함)
    [SerializeField] private LayerMask monsterMask;        // 몬스터 레이어

    private int hits = 0;
    private Rigidbody2D rb;

    private Coroutine _pulseRoutine;
    private Coroutine _lifeRoutine;

    private void OnEnable()
    {
        // 수명 타이머 + 도발 펄스 시작
        _lifeRoutine = StartCoroutine(AutoDespawn());
        _pulseRoutine = StartCoroutine(TauntPulse());
    }

    private void OnDisable()
    {
        if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 움직임 완전 고정
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    public void Init(int maxHits, float hp, float lifeTime)
    {
        this.maxHits = maxHits;
        this.hp = hp;
        this.lifeTime = lifeTime;
        StartCoroutine(AutoDespawn());
    }

    private IEnumerator AutoDespawn()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    public void TakeDamage(float amount) 
    { 
        hp -= amount;
        hits++;
        if (hp <= 0f || hits >= maxHits)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator TauntPulse()
    {
        var req = new TauntRequest
        {
            target = this.transform,
            duration = tauntDuration,
            priority = tauntPriority
        };

        var results = new Collider2D[32];

        while (true)
        {
            // 반경 내의 몬스터 탐색
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, tauntRadius, results, monsterMask);
            for (int i = 0; i < count; i++)
            {
                var col = results[i];
                if (!col) continue;

                // 1) ITauntable 인터페이스를 구현했다면 우선 사용
                if (col.TryGetComponent<ITauntable>(out var tauntable))
                {
                    tauntable.ApplyTaunt(req.target, req.duration, req.priority);
                    continue;
                }

                // 2) 백업: SendMessage로 "ApplyTaunt" 호출(옵션, 수신측 미구현이면 무시)
                col.SendMessage("ApplyTaunt", req, SendMessageOptions.DontRequireReceiver);
            }

            yield return new WaitForSeconds(tauntPulseInterval);
        }
    }

    // 디코이가 몬스터에게 던지는 데이터
    public class TauntRequest
    {
        public Transform target;
        public float duration;
        public int priority;
    }
}

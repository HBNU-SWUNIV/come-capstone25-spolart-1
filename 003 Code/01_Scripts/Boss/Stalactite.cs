using UnityEngine;

public class stalactite : MonoBehaviour
{
    [SerializeField] private float delay = 1f;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false; // 처음엔 충돌 X
        }
    }

    private void Start()
    {
        Invoke(nameof(EnableCollider), delay);
    }

    private void EnableCollider()
    {
        if (col != null)
        {
            col.enabled = true; // 1초 후 벽처럼 충돌 O
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
{
    // if (collision.collider.CompareTag("Player"))
    // {
    //     Debug.Log("플레이어가 종유석에 충돌함!");
    //     // 원하는 행동 수행 (피해 입히기 등)
    // }
}
}

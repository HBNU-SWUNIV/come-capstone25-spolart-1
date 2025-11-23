using UnityEngine;

public class Enemy : MonoBehaviour
{
    // 적대적 몬스터일경우, 해당 스크립트 상속

    public void Initialize()
    {
        // 몬스터 초기화
    }

    public void TakeDamage(float damage)
    {
        // 몬스터가 공격받았을 시 실행
        Debug.Log("[Enemy] Take damage: " + damage);
    }
}

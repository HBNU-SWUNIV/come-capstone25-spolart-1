using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 캐릭터가 적의 공격범위 안에 들어갔을 때 동작 가능한 스크립트
public class PlayerParry : MonoBehaviour
{
    private bool canParry = false;
    private bool isParrying = false;

    [Header("패링 성공 시 효과")]
    [SerializeField] private GameObject parryEffectPrefab;

    // 플레이어가 공격범위 안에 있을 때 플레이어 캐릭터의 패링을 활성화시킬 함수
    public void SetParryAvailable(bool value)
    {
        Debug.Log("패리 가능 여부: " + value);
        canParry = value;
    }

    public bool GetParryAvailable()
    {
        return canParry;
    }

    public void TryParry()
    {
        isParrying = true;
        Debug.Log("패링 성공");

        if (parryEffectPrefab != null)
        {
            Instantiate(parryEffectPrefab, transform.position, Quaternion.identity);
        }

        Invoke(nameof(EndParry), 0.3f); // 패링 지속 시간
    }

    private void EndParry()
    {
        isParrying = false;
    }

    // 플레이어가 패링 중이라면 몬스터 공격을 막고 몬스터의 그로기 hp 깎는 기능을 위한 함수
    public bool IsParrying()
    {
        return isParrying;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격/채광 시 이동을 잠굴 때 쓸 스크립트
/// 같은 프레임에 여러 곳에서 잠그더라도 행동 별로 관리
/// </summary>

public class ActionLock : MonoBehaviour
{
    private readonly HashSet<string> _reason = new();
    public bool IsLocked => _reason.Count > 0;

    // 이동/행동 잠금, 동일 행동은 중복 추가 안됨
    public void Lock(string reason)
    {
        if (string.IsNullOrEmpty(reason)) reason = "generic";
        _reason.Add(reason);
    }

    // 이동/행동 잠금 해제, 등록된 reason만 해제
    public void Unlock(string reason)
    {
        if (string.IsNullOrEmpty(reason)) reason = "generic";
        _reason.Remove(reason);
    }

    // 클리어
    public void ClearAll() => _reason.Clear();  
}

using System;
using UnityEngine;

public class FacilityData : MonoBehaviour
{
    // Guild, Temple, BlackSmith
    // GD001 = 길드 1번째 업그레이드, TP002 = 신전 2번째 업그레이드 BS003
    [SerializeField] private string facilityId; 
    public string FacilityId => facilityId;

    private int lv = 0;
    public int Lv => lv;
    

    [SerializeField] private int maxLv;
    public int MaxLv => maxLv;

    public void SetLevel(int newLevel)
    {
        lv = Mathf.Clamp(newLevel, 0, maxLv); // 1레벨과 최대 레벨 사이로 강제
    }

    /// <summary>
    /// 이 시설의 레벨을 1 증가시킵니다.
    /// </summary>
    public void IncrementLevel()
    {
        lv++;
        if (lv > maxLv) lv = maxLv;
    }
}
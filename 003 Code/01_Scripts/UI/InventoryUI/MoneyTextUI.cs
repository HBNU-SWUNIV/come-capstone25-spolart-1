using System.Collections;
using TMPro;
using UnityEngine;

public class MoneyTextUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void OnEnable()
    {
        // EconomyService가 아직 안 떠 있을 수도 있으니 방어
        var eco = EconomyService.Instance;
        if (eco != null)
        {
            eco.OnMoneyChanged += HandleMoneyChanged;
            // 구독 직후 즉시 1회 갱신
            HandleMoneyChanged(eco.Money);
        }
        else
        {
            // 초기화 순서 이슈가 있으면 한 프레임 뒤 재시도
            StartCoroutine(RebindNextFrame());
        }
    }

    private IEnumerator RebindNextFrame()
    {
        yield return null;
        var eco = EconomyService.Instance;
        if (eco != null)
        {
            eco.OnMoneyChanged += HandleMoneyChanged;
            HandleMoneyChanged(eco.Money);
        }
    }


    private void OnDestroy()
    {
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
    }

    private void HandleMoneyChanged(long money)
    {
        if (text != null)
        {
            text.text = money.ToString("N0");
        }
    }
}

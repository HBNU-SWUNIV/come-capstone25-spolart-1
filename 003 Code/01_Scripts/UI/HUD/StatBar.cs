using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatBar : MonoBehaviour
{
    [Header("위젯 참조")]
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text label;

    [Header("표시 설정")]
    [SerializeField] private float smoothSpeed = 5f; // 감소 시 줄어드는 속도

    private Coroutine anim;

    public void SetStatBar(float current, float max)
    {
        float t = max <= 0f ? 0f : current / max;

        if (fill != null)
        {
            fill.fillAmount = t;
        }

        if (label != null)
        {
            int curInt = Mathf.RoundToInt(current);
            int maxInt = Mathf.RoundToInt(max);
            label.text = $"{curInt} / {maxInt}";
        }
    }


    public void SetSmooth(float current, float max)
    {
        if (anim != null)
        {
            StopCoroutine(anim);
        }
        anim = StartCoroutine(SmoothRoutine(current, max));
    }

    private IEnumerator SmoothRoutine(float current, float max)
    {
        Debug.Log("코루틴 시작");
        if (fill == null)
        {
            yield break;
        }

        float target = max <= 0f ? 0f : current / max;

        // 현재 게이지와 목표 게이지가 거의 같아질 때까지 반복
        while (!Mathf.Approximately(fill.fillAmount, target))
        {
            // fillAmount를 목표 값으로 점차 이동
            fill.fillAmount = Mathf.MoveTowards(
                fill.fillAmount,
                target,
                smoothSpeed * Time.unscaledDeltaTime
            );

            // 게이지에 맞춰 현재 값 계산해서 정수로 표시
            if (label != null && max > 0f)
            {
                float visualCurrent = fill.fillAmount * max;
                int curInt = Mathf.RoundToInt(visualCurrent);
                int maxInt = Mathf.RoundToInt(max);
                label.text = $"{curInt} / {maxInt}";
            }

            yield return null;
        }

        // 마지막 한 번 더 정확한 값으로 맞춰주기
        if (label != null)
        {
            int curInt = Mathf.RoundToInt(current);
            int maxInt = Mathf.RoundToInt(max);
            label.text = $"{curInt} / {maxInt}";
        }
    }

}
